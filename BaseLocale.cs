using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.IO;
using System.Diagnostics;

namespace ManualPatcher
{
    abstract class BaseLocale
    {
        public string URL { get; private set; }
        public string Name { get; private set; }
        public byte Locale { get; private set; }
        public ushort MinVersion { get; private set; }
        private Dictionary<ushort, ushort> _versionToNewVersion = null;

        public ushort MaxVersion { get { return (_versionToNewVersion == null ? (ushort)999 : _versionToNewVersion.Max(x => x.Key)); } }

        public BaseLocale(string name, string url, byte locale, ushort minVersion = 0)
        {
            URL = url;
            Name = name;
            Locale = locale;
            MinVersion = minVersion;
        }

        public override string ToString()
        {
            return Name;
        }

        public void LoadAllPatches(ushort? minVersion = null)
        {
            minVersion = minVersion.HasValue ? minVersion : MinVersion;

            if (_versionToNewVersion != null) return;
            _versionToNewVersion = new Dictionary<ushort, ushort>();

            Func<ushort, IEnumerable<ushort>> loadVersionInPatch = (version) =>
            {
                WebRequest wr = null;

                var fullUrl = String.Format("{0}{1:D5}/Version.info", URL, version);
                switch (URL.Substring(0, URL.IndexOf(':')))
                {
                    case "http": wr = HttpWebRequest.Create(fullUrl); break;
                    case "https": wr = HttpWebRequest.Create(fullUrl); break;
                    case "ftp": wr = FtpWebRequest.Create(fullUrl); break;
                    default: throw new NotSupportedException("Protocol used in this url is not supported: " + URL);
                }
                
                
                wr.Proxy = null;
                try
                {
                    using (var response = wr.GetResponse() as WebResponse)
                    using (var sr = new StreamReader(response.GetResponseStream()))
                    {
                        var fullFile = sr
                            .ReadToEnd();

                        // First two lines are not useful
                        var lines = fullFile
                            .Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                            .Skip(2)
                            .ToList();

                        Console.WriteLine("Loaded version {0}", version);
                        return lines.Select((input) => ushort.Parse(input));
                    }
                }
                catch (Exception)
                {
                }

                return null;
            };

            var foundUpdate = false;

            for (ushort i = minVersion.Value; ; i++)
            {
                var result = loadVersionInPatch(i);
                if (result == null)
                {
                    if (foundUpdate) break;
                }
                else
                {
                    result.ToList().ForEach((ver) =>
                    {
                        if (!_versionToNewVersion.ContainsKey(ver))
                        {
                            _versionToNewVersion.Add(ver, i);
                        }
                        else if (_versionToNewVersion[ver] < i)
                        {
                            _versionToNewVersion[ver] = i;
                        }
                    });

                    foundUpdate = true;
                }
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
            var filename = String.Format("{0:D5}to{1:D5}.patch", versionFrom, versionTo);
            var tempfile = LocalPath(filename);

            if (!File.Exists(tempfile))
            {
                var wc = new WebClient();
                var uri = new Uri(String.Format("{0}{1:D5}/{2}", URL, versionTo, filename));

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
            }

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

            return new KeyValuePair<byte,ushort>(mslocale, msver);
        }
    }
}
