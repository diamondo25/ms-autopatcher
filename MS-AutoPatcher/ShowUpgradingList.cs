using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace MS_AutoPatcher
{
    public partial class ShowUpgradingList : Form
    {
        BaseLocale _locale = null;
        BackgroundWorker _worker = null;

        public ShowUpgradingList(BaseLocale locale)
        {
            InitializeComponent();

            _locale = locale;
        }

        private void ShowUpgradingList_Load(object sender, EventArgs e)
        {

            _worker = _locale.LoadAllPatches();

            _worker.ProgressChanged += (x, y) =>
            {
                RedrawList();
            };

            _worker.RunWorkerCompleted += (x, y) =>
            {
                RedrawList();
            };

            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = true;

            _worker.RunWorkerAsync();
        }

        private void RedrawList()
        {
            Invoke((MethodInvoker)delegate
            {
                listView1.Items.Clear();
                listView1.Items.AddRange(
                    _locale.VersionToNewVersion
                    .OrderBy(kvp => kvp.Key)
                    .Select(kvp => {
                        BaseLocale.VersionInfo tmp;
                        string patchers = "", lastModified = "";
                        if (_locale.VersionInfos.TryGetValue(kvp.Key, out tmp) && tmp != null)
                        {
                            patchers = string.Join(", ", tmp.VersionsPatchable.ToArray());
                            lastModified = tmp.LastModified.ToString();
                        }

                        return new ListViewItem(new string[] { kvp.Key.ToString(), kvp.Value.ToString(), patchers, lastModified });
                    })
                    .ToArray()
                );
            });
        }

        private void ShowUpgradingList_FormClosing(object sender, FormClosingEventArgs e)
        {
            _worker.CancelAsync();
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            
        }

        private void ShowUpgradingList_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'p')
                System.IO.File.WriteAllLines("localedownloads.txt", _locale.GetAllDownloadables());
            else if (e.KeyChar == 'd' && MessageBox.Show("Are you sure you want to download all the files (if they do not exist)?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                var downloads = _locale.GetAllDownloadables();

                Action<byte, Queue<string>> download = null;
                download = (thread, urls) =>
                {
                    var url = urls.Dequeue();
                    var wc = new WebClient();
                    var uri = new Uri(url);
                    var localPath = _locale.Name + uri.LocalPath;
                    var directory = localPath.Substring(0, localPath.LastIndexOf('/')); // Only the directory
                    Directory.CreateDirectory(directory);

                    wc.DownloadFileCompleted += (x, y) =>
                    {
                        if (urls.Count == 0) return;
                        download(thread, urls);
                    };

                    wc.DownloadFileAsync(uri, localPath);

                };

                int threads = 5;
                int filesPerThread = downloads.Count / threads;
                for (byte i = 0; i < threads; i++)
                {
                    var subQueue = new Queue<string>(downloads.Take(filesPerThread).ToList());
                    downloads = downloads.Skip(filesPerThread).ToList();
                    download(i, subQueue);
                }
            }
        }
    }
}
