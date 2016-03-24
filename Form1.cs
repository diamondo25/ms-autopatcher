using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MS_AutoPatcher
{
    public partial class Form1 : Form
    {
        private string _mapleDir = "";
        private string __nxpatcher = null;
        private string _nxPatcher { get { return __nxpatcher ?? (__nxpatcher = ExportNXPatcher()); } }
        private bool RemoveBackup { get { return chkBackupMaple.Checked; } }
        private bool RemovePatchAfterInstall { get { return chkRemovePatchAfterInstall.Checked; } }

        BackgroundWorker bw = new BackgroundWorker();

        private void ToggleInputs(bool enable)
        {
            this.Invoke((MethodInvoker)delegate
            {
                txtPath.Enabled = btnPathSelector.Enabled = locales.Enabled = nudVersion.Enabled = nudFinalVersion.Enabled = button2.Enabled =
                chkBackupMaple.Enabled = chkRemovePatchAfterInstall.Enabled = cbProxies.Enabled = enable;
            });

        }

        private void SetStatus(string status)
        {
            this.Invoke((MethodInvoker)delegate
            {
                lblStatus.Text = status;
            });
        }

        public Form1()
        {
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = false;
            bw.DoWork += (x, y) =>
            {
                object[] lol = y.Argument as object[];
                try
                {
                    ToggleInputs(false);
                    SetStatus("Starting!");
                    Run(lol[0] as BaseLocale, (ushort)lol[1]);
                }
                catch (Exception)
                {
                    ToggleInputs(true);
                    SetStatus("Idle...");
                }
            };
            bw.ProgressChanged += (x, progress) =>
            {
                if (progress.UserState is string)
                {
                    lblStatus.Text = (string)progress.UserState;
                }
                else if (progress.UserState is int)
                {
                    nudVersion.Value = (int)progress.UserState;
                }
            };

            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            locales.Items.AddRange(new object[] {
                new LocaleGlobal(),
                new LocaleEurope(),
                new LocaleSEA(),
                new LocaleJapan(),
                new LocaleTaiwan(),
                new LocaleIndonesia(),
                new LocaleKorea()
            });
        }

        private void btnPathSelector_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "MapleStory.exe|MapleStory.exe";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!File.Exists(Path.Combine(Path.GetDirectoryName(ofd.FileName), "Patcher.exe")))
                {
                    MessageBox.Show("Couldn't find Patcher.exe. This should be in the same directory.");
                }
                else
                {
                    txtPath.Text = ofd.FileName;
                    _mapleDir = Path.GetDirectoryName(txtPath.Text);

                    var versionInfo = BaseLocale.GetVersionInfo(txtPath.Text);

                    if (versionInfo.Key == 1 && versionInfo.Value == 0)
                    {

                        var version = DetectBaseWZVersion();
                        if (version == 0)
                        {
                            MessageBox.Show("Couldn't figure out the version. Please enter it yourself!");
                        }
                        var locale = DetectLocale();
                        if (locale == 0)
                        {
                            MessageBox.Show("Couldn't figure out the locale. Please enter it yourself!");
                        }

                        versionInfo = new KeyValuePair<byte, ushort>(locale, version);


                    }

                    nudVersion.Value = versionInfo.Value;
                    var i = 0;
                    foreach (BaseLocale bl in locales.Items)
                    {
                        if (bl.Locale == versionInfo.Key)
                        {
                            locales.SelectedIndex = i;
                        }
                        i++;
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (bw.IsBusy)
            {
                MessageBox.Show("Don't bug me, I'm working!");
                return;
            }

            if (!File.Exists(txtPath.Text))
            {
                MessageBox.Show("Could not find Maplestory!!!");
                return;
            }

            if (locales.SelectedItem == null)
            {
                MessageBox.Show("No locale selected...");
                return;
            }

            bw.RunWorkerAsync(new object[] {
                locales.SelectedItem as BaseLocale,
                (ushort)nudVersion.Value
            });
        }

        private void Run(BaseLocale bl, ushort currentVersion)
        {

            while (true)
            {
                ushort? newVersion = bl.GetNewVersion(currentVersion);

                if (newVersion.HasValue)
                {
                    newVersion = (ushort)(currentVersion + 1);
                    while (true)
                    {
                        bw.ReportProgress(0, String.Format("Downloading {0} to {1} patch: init...", currentVersion, newVersion.Value));

                        string patchFilename = "";
                        try
                        {
                            patchFilename = bl.DownloadPatchfile(currentVersion, newVersion.Value, (progress) =>
                            {
                                bw.ReportProgress(0, String.Format("Downloading {0} to {1} patch: {2}%", currentVersion, newVersion.Value, progress));
                            });
                        }
                        catch (Exception)
                        {
                            bw.ReportProgress(0, "Failed downloading patch. Trying different version.");
                            newVersion = (ushort)(newVersion.Value - 1);
                            continue;
                        }

                        bw.ReportProgress(0, String.Format("Running patcher for {0} -> {1}", currentVersion, newVersion.Value));
                        var exitCode = OpenNXPatcher(patchFilename);

                        if (exitCode != 0)
                        {
                            if (currentVersion + 1 != newVersion.Value)
                            {
                                bw.ReportProgress(0, "Trying older version...");
                                bw.ReportProgress(0, DetectBaseWZVersion());
                                newVersion = (ushort)(newVersion.Value - 1);
                                continue;
                            }
                            else if (MessageBox.Show("NXPatcher exited with exit code " + exitCode + ". Try maple patcher instead?", "", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                            {
                                WritePatcherInfoFile(currentVersion, newVersion.Value);
                                OpenPatcher();
                            }
                            else
                            {
                                MessageBox.Show("Sorry, but I'm out of ideas now....");
                                ToggleInputs(true);
                                return;
                            }
                        }
                        else
                        {
                            if (!ApplyNXPatchedFiles(currentVersion, newVersion.Value))
                                return;
                        }

                        break;
                    }

                    currentVersion = newVersion.Value;
                    bw.ReportProgress(0, (int)nudVersion.Value);

                }
                else
                {
                    bw.ReportProgress(0, "Everything is up-to-date");
                    MessageBox.Show("Nothing to patch. Current version is " + currentVersion + " and latest version is " + bl.LatestVersion());
                    break;
                }

            }
        }

        private void DeleteFileIfExists(string file)
        {
            if (File.Exists(file))
                File.Delete(file);
        }

        private int OpenPatcher()
        {
            var patcher = Path.Combine(_mapleDir, "Patcher.exe");
            var process = Process.Start(new ProcessStartInfo(patcher)
            {
                WorkingDirectory = _mapleDir
            });
            process.WaitForExit();

            Console.WriteLine("MaplePatcher exited with {0}", process.ExitCode);
            List<Process> processes = Process.GetProcesses().Where(x => x.ProcessName.IndexOf("NewPatcher") != -1).ToList();

            processes.ForEach(p => p.WaitForExit());

            return process.ExitCode;
        }

        private int OpenNXPatcher(string patchfile)
        {
            var nxdirPath = Path.GetDirectoryName(_nxPatcher);
            File.WriteAllText(Path.Combine(_mapleDir, "NXPatcher.ini"), Properties.Resources.NXPatcher_INI);

            var process = Process.Start(new ProcessStartInfo(_nxPatcher)
            {
                WorkingDirectory = _mapleDir,
                Arguments = "read " + '"' + patchfile + '"'
            });

            process.WaitForExit();

            Console.WriteLine("NXPatcher exited with {0}", process.ExitCode);

            return process.ExitCode;
        }

        private bool ApplyNXPatchedFiles(int fromVersion, int toVersion)
        {
            string outputDir = Path.Combine(_mapleDir, String.Format("Patcher_{0}-{1}", fromVersion, toVersion)) + Path.DirectorySeparatorChar;
            string backupDir = Path.Combine(_mapleDir, String.Format("Prepatch_{0}", fromVersion)) + Path.DirectorySeparatorChar;

            if (!Directory.Exists(outputDir))
            {
                MessageBox.Show("Failed to patch? The patch output dir wasn't there.\nOutput dir:\n" + outputDir);
                return false;
            }

            if (Directory.Exists(backupDir))
            {
                if (MessageBox.Show("Backup directory is already there; overwrite?\nBackup dir:\n" + backupDir, "", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                {
                    return false;
                }
                else
                {
                    Directory.Delete(backupDir, true);
                }
            }

            Func<string, bool> moveFiles = null;
            moveFiles = (path) =>
            {
                Console.WriteLine("Running in path: {0}", path);
                Directory.CreateDirectory(Path.Combine(backupDir, path));

                var outputPath = Path.Combine(outputDir, path);

                foreach (var filepath in Directory.GetFiles(outputPath))
                {
                    var filename = Path.GetFileName(filepath);
                    var fileLocalPath = Path.Combine(path, filename);
                    var originalFilePath = Path.Combine(_mapleDir, fileLocalPath);
                    var backupFilePath = Path.Combine(backupDir, fileLocalPath);

                    if (File.Exists(originalFilePath))
                    {
                        try
                        {
                            Console.WriteLine("Moving {0} to {1}", originalFilePath, backupFilePath);
                            File.Move(originalFilePath, backupFilePath);
                            Console.WriteLine("Moving {0} to {1}", filepath, originalFilePath);
                            File.Move(filepath, originalFilePath);
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        Console.WriteLine("File not found: {0}", originalFilePath);
                    }
                }

                foreach (var dirpath in Directory.GetDirectories(outputPath))
                {
                    var dirname = dirpath.Substring(dirpath.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                    var dirLocalPath = Path.Combine(path, dirname + Path.DirectorySeparatorChar);
                    var originalDirPath = Path.Combine(_mapleDir, dirLocalPath);
                    var backupDirPath = Path.Combine(backupDir, dirLocalPath);

                    if (Directory.Exists(originalDirPath))
                    {
                        if (!moveFiles(dirLocalPath))
                            return false;
                    }
                    else
                    {
                        Console.WriteLine("Directory not found: {0}", originalDirPath);
                    }
                }

                return true;
            };

            if (!moveFiles("." + Path.DirectorySeparatorChar))
            {
                MessageBox.Show("An error occurred while applying the patched files. You can find a backup of the files already patched here:\n" + backupDir);
                return false;
            }
            else
            {
                Directory.Delete(outputDir, true);
                if (RemoveBackup)
                    Directory.Delete(backupDir, true);
            }

            return true;
        }

        private ushort DetectBaseWZVersion()
        {
            var nxdirPath = Path.GetDirectoryName(_nxPatcher);
            File.WriteAllText(Path.Combine(_mapleDir, "NXPatcher.ini"), Properties.Resources.NXPatcher_INI);

            var process = new Process();
            process.StartInfo = new ProcessStartInfo(_nxPatcher)
            {
                WorkingDirectory = _mapleDir,
                Arguments = "version Base.wz",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            process.Start();

            var sr = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (sr.Contains("version is"))
            {
                var version = ushort.Parse(sr.Substring(sr.LastIndexOf(' ')));
                return version;
            }

            return 0;
        }

        private byte DetectLocale()
        {
            // KMS has generic MapleStory.ini
            if (File.Exists(Path.Combine(_mapleDir, "MapleStory.ini")))
                return 1;

            // JMS has ingame advertising from Tricod
            if (File.Exists(Path.Combine(_mapleDir, "Tricod6_0_maple_md.dll")))
                return 3;

            // CMS uses Shanda encryption (SD*)
            if (File.Exists(Path.Combine(_mapleDir, "SDDyn.ini")))
                return 4;

            // TWMS has 'bean.ico' BeanFun! icon
            if (File.Exists(Path.Combine(_mapleDir, "bean.ico")))
                return 6;

            // SEA has 'NetWrap.dll' HackShield dll (unused?)
            if (File.Exists(Path.Combine(_mapleDir, "NetWrap.dll")))
                return 7;

            // Old GMS version has a 'MapleStoryUS.ini' file
            if (File.Exists(Path.Combine(_mapleDir, "MapleStoryUS.ini")))
                return 8;

            // EMS has WZ files with locales
            if (File.Exists(Path.Combine(_mapleDir, "StringDE.wz")))
                return 9;

            // IMS has a MSIDLauncer.exe
            if (File.Exists(Path.Combine(_mapleDir, "MSIDLauncher.exe")))
                return 100;

            return 0;
        }

        private string ExportNXPatcher()
        {
            string tempFile = Path.GetTempFileName() + ".exe";
            File.WriteAllBytes(tempFile, Properties.Resources.NXPatcher_EXE);
            return tempFile;
        }

        private void WritePatcherInfoFile(ushort currentVersion, ushort newVersion)
        {
            using (var binw = new BinaryWriter(File.Open(Path.Combine(_mapleDir, "Patcher.info"), FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                binw.Write(currentVersion);
                binw.Write(newVersion);
                binw.Write(new byte[0x300]);
                // Last executed path
                binw.Write(new byte[0x100]);
                // Last working dir
                binw.Write(new byte[0x100]);

                if (binw.BaseStream.Length != 1284)
                    throw new Exception("Patcher.info is an invalid length!");
                binw.Flush();
                binw.Close();
            }
        }

        BackgroundWorker patchFetcherWorker = null;
        private void locales_SelectedIndexChanged(object sender, EventArgs e)
        {
            var bl = (BaseLocale)locales.SelectedItem;

            cbProxies.Items.Clear();
            cbProxies.Items.AddRange(bl.Proxies.Select(kvp => kvp.Key).ToArray());
            cbProxies.SelectedItem = "Official";

        }

        private void Form1_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            var locale = (BaseLocale)locales.SelectedItem;
            if (locale != null && locale.Loaded)
            {
                new ShowUpgradingList(locale).ShowDialog();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (patchFetcherWorker != null && patchFetcherWorker.IsBusy)
            {
                patchFetcherWorker.CancelAsync();
            }
        }

        private void cbProxies_SelectedIndexChanged(object sender, EventArgs e)
        {
            var locale = (BaseLocale)locales.SelectedItem;
            if (locale != null)
            {
                locale.UseProxy((string)cbProxies.SelectedItem);

                if (patchFetcherWorker != null)
                {
                    patchFetcherWorker.CancelAsync();
                    patchFetcherWorker.Dispose();
                    patchFetcherWorker = null;
                }

                SetStatus("Loading all patches...");
                ToggleInputs(false);
                if (nudVersion.Value > 0)
                    patchFetcherWorker = locale.LoadAllPatches((ushort)nudVersion.Value, true);
                else
                    patchFetcherWorker = locale.LoadAllPatches(null, true);

                patchFetcherWorker.WorkerReportsProgress = true;
                patchFetcherWorker.WorkerSupportsCancellation = true;

                patchFetcherWorker.ProgressChanged += (x, data) =>
                {
                    SetStatus((string)data.UserState);
                };

                patchFetcherWorker.RunWorkerCompleted += (x, y) =>
                {
                    Invoke((MethodInvoker)delegate
                    {
                        nudFinalVersion.Minimum = locale.MinVersion;
                        nudFinalVersion.Maximum = locale.MaxVersion;
                        nudFinalVersion.Value = nudFinalVersion.Maximum;

                    });
                    SetStatus("Idle...");
                    ToggleInputs(true);
                };

                patchFetcherWorker.RunWorkerAsync();
            }
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
        }

        private void createPatcherinfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Title = "Patcher.info to create";
            sfd.FileName = "Patcher.info";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var tmp = _mapleDir;
                _mapleDir = Path.GetDirectoryName(sfd.FileName);
                WritePatcherInfoFile((ushort)nudVersion.Value, (ushort)nudFinalVersion.Value);
                _mapleDir = tmp;
            }
        }
    }
}
