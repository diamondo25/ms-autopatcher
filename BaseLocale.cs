using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

using System.Net;
using System.IO;
using System.Diagnostics;

using System.Windows.Forms;
using System.ComponentModel;

namespace MS_AutoPatcher
{
    public abstract class BaseLocale
    {
        public class VersionInfo
        {
            public ushort Version { get; private set; }

            /// <summary>
            /// Value of the LastModified header the server sent when the Version.info file was requested
            /// </summary>
            public DateTime LastModified { get; private set; }

            /// <summary>
            /// A list of versions this version has patches for (to upgrade to this version)
            /// </summary>
            public List<ushort> VersionsPatchable { get; private set; }

            public VersionInfo(ushort version, DateTime lastModified, List<ushort> versionsPatchable)
            {
                Version = version;
                LastModified = lastModified;
                VersionsPatchable = versionsPatchable;
            }
        }
        
        public string URL { get; private set; }
        public string Name { get; private set; }
        public byte Locale { get; private set; }
        public ushort MinVersion { get; private set; }
        public Dictionary<string, string> Proxies { get { return new Dictionary<string, string>(_proxies); } }

        public Dictionary<ushort, ushort> VersionToNewVersion { get { return new Dictionary<ushort, ushort>(_versionToNewVersion); } }
        public Dictionary<ushort, VersionInfo> VersionInfos { get { return new Dictionary<ushort, VersionInfo>(_versionInfos); } }

        public ushort MaxVersion { get { return (_versionToNewVersion == null ? (ushort)999 : _versionToNewVersion.Max(x => x.Key)); } }

        private ConcurrentDictionary<ushort, ushort> _versionToNewVersion = null;
        private ConcurrentDictionary<ushort, VersionInfo> _versionInfos = null;
        private Dictionary<string, string> _proxies = new Dictionary<string, string>();

        public bool Loaded { get; private set; }
        public bool Loading { get; private set; }
        
        public bool NewFormat { get; protected set; }

        byte _threads = 0;

        public BaseLocale(string name, string url, byte locale, ushort minVersion = 0, byte threads = 5)
        {
            AddProxy("Official", url);
            UseProxy("Official");

            Name = name;
            Locale = locale;
            MinVersion = minVersion;
            Loaded = false;
            Loading = false;
            _threads = threads;
            NewFormat = false;
        }

        public override string ToString()
        {
            return Name;
        }

        public static string GetPatchFilename(ushort versionFrom, ushort versionTo)
        {
            return string.Format("{0:D5}to{1:D5}.patch", versionFrom, versionTo);
        }

        public static string GetVersionFilename()
        {
            return "Version.info";
        }

        public string GetPatchDir(ushort version)
        {
            return string.Format("{0}{1:D5}/", NewFormat ? "" : "patchdir/",  version);
        }

        public string GetNoticeFile(ushort version)
        {
            if (NewFormat) throw new Exception("Unsupported");
            return string.Format("notice/{0:D5}.txt", version);
        }

        protected void AddProxy(string name, string url)
        {
            _proxies.Add(name, url);
        }

        /// <summary>
        /// Set URL to one of the defined urls in <paramref name="Proxies"/>
        /// </summary>
        /// <param name="name">Name of the proxy to use</param>
        public void UseProxy(string name)
        {
            URL = _proxies[name];
        }

        public BackgroundWorker LoadAllPatches(ushort? minVersion = null, bool forced = false)
        {
            var bw = new BackgroundWorker();
            if (Loaded && !Loading && forced)
            {
                _versionToNewVersion = null;
                _versionInfos = null;
                Loading = false;
                Loaded = false;
            }
            if (!Loaded)
                bw.DoWork += (x, y) => _loadAllPatches(bw, minVersion);
            return bw;
        }

        public List<string> GetAllDownloadables()
        {
            var extraFiles = new List<string>();

            var versionSpecificDownloads = VersionInfos.SelectMany(kvp =>
            {
                var patchDir = GetPatchDir(kvp.Key);
                
                extraFiles.Add(patchDir + GetVersionFilename());
                extraFiles.Add(patchDir + "ExePatch.dat"); // Subversion
                extraFiles.Add(patchDir + "NewPatcher.dat"); // a new Patcher executable

                var patches = kvp.Value.VersionsPatchable.Select(prevVersion => patchDir + GetPatchFilename(prevVersion, kvp.Key));

                return patches;
            });

            if (!NewFormat)
            {
                // Add all patchnotes
                ushort latestVersion = VersionInfos.Keys.Max();
                for (ushort i = 0; i < latestVersion; i++)
                {
                    extraFiles.Add(GetNoticeFile(i));
                }
            }

            return versionSpecificDownloads.Concat(extraFiles).Select(x => URL + x).ToList();
        }

        private void _loadAllPatches(BackgroundWorker bw, ushort? minVersion = null)
        {
            if (Loading || Loaded)
                throw new InvalidOperationException();

            try
            {
                Loading = true;
                minVersion = minVersion.HasValue ? minVersion : MinVersion;

                if (_versionToNewVersion != null) return;
                _versionToNewVersion = new ConcurrentDictionary<ushort, ushort>();
                _versionInfos = new ConcurrentDictionary<ushort, VersionInfo>();

                int threads = 0;
                bool foundAny = false;

                Action<byte, ushort> loadVersionInPatch = null;
                loadVersionInPatch = (thread, version) =>
                {
                    _versionInfos.TryAdd(version, null); // Secure set this version

                    var fullUrl = URL + GetPatchDir(version) + GetVersionFilename();
                    WebRequest wr = WebHelper.BuildWebRequest(fullUrl);
                    VersionInfo tmp = null;
                    
                    var httpRequest = wr as HttpWebRequest;
                    var ftpRequest = wr as FtpWebRequest;

                    wr.Proxy = null;
                    wr.Timeout = 5000;

                    if (httpRequest != null)
                    {
                        // Prevent Apache from redirecting us to a different ver (154 -> 145)
                        //httpRequest.AllowAutoRedirect = false;
                        httpRequest.ReadWriteTimeout = 3000;
                        httpRequest.Timeout = 3000;
                    }

                    Console.WriteLine("Thread " + thread + ": Requesting Version.info for version {0}", version);

                    wr.BeginGetResponse((cb) =>
                    {
                        try
                        {
                            var baseResponse = wr.EndGetResponse(cb);
                            var httpResponse = baseResponse as HttpWebResponse;
                            var ftpResponse = baseResponse as FtpWebResponse;

                            if (httpResponse != null)
                            {
                                Console.WriteLine("Thread " + thread + ": Response code {0} for url {1}", httpResponse.StatusCode, httpResponse.ResponseUri.AbsoluteUri);
                                if (httpResponse.StatusCode == HttpStatusCode.MultipleChoices)
                                {
                                    throw new Exception("Got multiple choices response.");
                                }
                                else if (httpResponse.ResponseUri.AbsoluteUri != fullUrl)
                                {
                                    throw new Exception("Got forced redirect.");
                                }
                            }

                            using (var sr = new StreamReader(baseResponse.GetResponseStream()))
                            {
                                var fullFile = sr.ReadToEnd();

                                // First two lines are not useful
                                var versions = fullFile
                                        .Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                                        .Where(x => !x.StartsWith("0x")) // Remove checksum values
                                        .Select((input) => ushort.Parse(input))
                                        .ToList();

                                // Remove current version
                                if (!versions.Remove(version))
                                {
                                    Console.WriteLine("Thread " + thread + ": WARN! LIST OF VERSIONS DID NOT INCLUDE REQUESTED VERSION {0}.", version);
                                }

                                Console.WriteLine("Thread " + thread + ": Loaded version {0}", version);

                                if (_versionInfos.ContainsKey(version))
                                {
                                    DateTime lastModified = DateTime.Now;
                                    if (httpResponse != null)
                                        lastModified = httpResponse.LastModified;
                                    else if (ftpResponse != null)
                                        lastModified = ftpResponse.LastModified;

                                    var verInfo = new VersionInfo(
                                        version,
                                        lastModified,
                                        versions
                                    );

                                    foundAny = true;
                                    if (!_versionInfos.TryUpdate(version, verInfo, null))
                                    {
                                        Console.WriteLine("Thread " + thread + ": ALREADY LOADED???? {0}", version);
                                    }
                                    else
                                    {
                                        versions.ForEach((ver) =>
                                        {
                                            ushort knownVersion = 0;
                                            if (!_versionToNewVersion.TryGetValue(ver, out knownVersion))
                                            {
                                                _versionToNewVersion.TryAdd(ver, version);
                                            }
                                            else if (knownVersion < version)
                                            {
                                                _versionToNewVersion.TryUpdate(ver, version, knownVersion);
                                            }
                                        });

                                        _versionToNewVersion.TryAdd(version, version); // Add itself

                                        bw.ReportProgress(0, string.Format("Loaded version {0}", version));

                                    }
                                }
                                else
                                {
                                    throw new InvalidOperationException();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Exception while fetching patch: {0}", ex);
                            Console.WriteLine("URL: {0}", fullUrl);


                            _versionInfos.TryRemove(version, out tmp);

                            if (foundAny)
                            {
                                threads--;
                                Console.WriteLine("Thread " + thread + ": We've got something, so I stop working. Threads left: {0}", threads);
                                return;
                            }
                            else
                            {
                                Console.WriteLine("Thread " + thread + ": Continuing with next version....");
                            }
                        }

                        ushort nextVersion = (ushort)(version + 1);
                        while (_versionInfos.TryGetValue(nextVersion, out tmp)) nextVersion++;

                        loadVersionInPatch(thread, nextVersion);
                    }, null);
                };

                threads = _threads;
                for (byte i = 0; i < threads; i++)
                {
                    loadVersionInPatch(i, (ushort)(minVersion.Value + i));
                }


                while (threads > 0)
                    System.Threading.Thread.Sleep(2000);

                // Done!
                Loaded = true;
                Loading = false;
            }
            catch (Exception)
            {
                Loaded = false;
                Loading = false;
            }
        }

        private string LocalPath(string filename)
        {
            var outputFolder = Path.Combine(Environment.CurrentDirectory, Name);
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            var downloadFolder = Path.Combine(outputFolder, "Downloads");
            if (!Directory.Exists(downloadFolder))
                Directory.CreateDirectory(downloadFolder);

            return Path.Combine(downloadFolder, filename);
        }

        public string DownloadPatchfile(ushort versionFrom, ushort versionTo, Action<int> onProgress)
        {
            var filename = GetPatchFilename(versionFrom, versionTo);
            var tempfile = LocalPath(filename);

            if (File.Exists(tempfile))
            {
                if (MessageBox.Show("Patchfile already exists; use?\nPatchfile: " + filename, "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    return tempfile;
                }
                else
                {
                    File.Delete(tempfile);
                }
            }

            var wc = new WebClient();
            var uri = new Uri(URL + GetPatchDir(versionTo) + filename);

            int percentage = -1;
            wc.DownloadProgressChanged += (x, e) =>
            {
                if (percentage == e.ProgressPercentage) return;
                percentage = e.ProgressPercentage;
                onProgress(percentage);
            };

            Console.WriteLine("Downloading patchfile: {0}", uri.AbsoluteUri);
            wc.DownloadFileAsync(uri, tempfile);

            while (wc.IsBusy)
                System.Threading.Thread.Sleep(2000);

            return tempfile;
        }

        public ushort? GetNewVersion(ushort version)
        {
            if (_versionToNewVersion.ContainsKey(version) && _versionToNewVersion[version] != version)
            {
                return _versionToNewVersion[version];
            }
            else
            {
                return null;
            }
        }

        public ushort LatestVersion()
        {
            return _versionToNewVersion.Max(x => x.Key);
        }

        public static KeyValuePair<byte, ushort> GetVersionInfo(string location)
        {
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(location);
            ushort msver = (ushort)fvi.ProductMinorPart;
            byte mslocale = (byte)fvi.ProductMajorPart;

            Console.WriteLine("MapleStory v{0}.{1} locale {2}", fvi.ProductMinorPart, fvi.ProductBuildPart, fvi.ProductMajorPart);

            return new KeyValuePair<byte, ushort>(mslocale, msver);
        }
    }
}
