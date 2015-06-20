using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace ManualPatcher
{
    public partial class Form1 : Form
    {
        private string _mapleDir = "";
        private string __nxpatcher = null;
        private string _nxPatcher { get { return __nxpatcher ?? (__nxpatcher = ExportNXPatcher()); } }

        BackgroundWorker bw = new BackgroundWorker();

        public Form1()
        {
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = false;
            bw.DoWork += (x, y) =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    txtPath.Enabled = btnPathSelector.Enabled = locales.Enabled = nudVersion.Enabled = nudFinalVersion.Enabled = button2.Enabled = false;

                    lblStatus.Text = "Starting!";
                });

                object[] lol = y.Argument as object[];
                try
                {
                    Run(lol[0] as BaseLocale, (ushort)lol[1]);
                }
                catch (Exception)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        txtPath.Enabled = btnPathSelector.Enabled = locales.Enabled = nudVersion.Enabled = nudFinalVersion.Enabled = button2.Enabled = true;

                        lblStatus.Text = "Idle...";
                    });
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
                new LocaleEurope(),
                new LocaleGlobal(),
                new LocaleJapan(),
                new LocaleSEA()
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

                        nudVersion.Value = CheckBaseWZVersion();
                        if (nudVersion.Value == 0)
                        {
                            MessageBox.Show("Couldn't figure out the variant. Please enter it yourself!");
                        }
                    }
                    else
                    {
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
                                bw.ReportProgress(0, CheckBaseWZVersion());
                                newVersion = (ushort)(newVersion.Value - 1);
                                continue;
                            }
                            else if (MessageBox.Show("NXPatcher exited with exit code " + exitCode + ". Try maple patcher instead?", "", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                            {
                                using (var binw = new BinaryWriter(File.Open(Path.Combine(_mapleDir, "Patcher.info"), FileMode.Create, FileAccess.Write, FileShare.Read)))
                                {
                                    binw.Write((ushort)currentVersion);
                                    binw.Write((ushort)newVersion.Value);
                                    binw.Write(new byte[0x300]);
                                    // Last executed path
                                    binw.Write(new byte[0x100]);
                                    // Last working dir
                                    binw.Write(new byte[0x100]);
                                    binw.Flush();
                                    binw.Close();
                                }
                                OpenPatcher();
                            }
                            else
                            {
                                MessageBox.Show("Sorry, but I'm out of ideas now....");
                            }
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

            processes.ForEach(p =>
            {
                p.WaitForExit();
            });

            return process.ExitCode;
        }

        private int OpenNXPatcher(string patchfile)
        {
            var nxdirPath = Path.GetDirectoryName(_nxPatcher);
            File.WriteAllText(Path.Combine(_mapleDir, "NXPatcher.ini"), Properties.Resources.NXPatcher_INI);

            var process = Process.Start(new ProcessStartInfo(_nxPatcher)
            {
                WorkingDirectory = _mapleDir,
                Arguments = "read "+ '"' + patchfile + '"'
            });

            process.WaitForExit();

            Console.WriteLine("NXPatcher exited with {0}", process.ExitCode);

            return process.ExitCode;
        }

        private int CheckBaseWZVersion()
        {
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
                var version = int.Parse(sr.Substring(sr.LastIndexOf(' ')));
                return version;
            }

            return 0;
        }

        private string ExportNXPatcher()
        {
            string tempFile = Path.GetTempFileName() + ".exe";
            File.WriteAllBytes(tempFile, Properties.Resources.NXPatcher_EXE);
            return tempFile;
        }

        private void locales_SelectedIndexChanged(object sender, EventArgs e)
        {
            var bl = locales.SelectedItem as BaseLocale;
            lblStatus.Text = "Loading all patches...";

            if (nudVersion.Value > 0)
                bl.LoadAllPatches((ushort)nudVersion.Value);
            else
                bl.LoadAllPatches();

            nudFinalVersion.Minimum = bl.MinVersion;
            nudFinalVersion.Maximum = bl.MaxVersion;
            nudFinalVersion.Value = nudFinalVersion.Maximum;

            lblStatus.Text = "Idle...";
        }
    }
}
