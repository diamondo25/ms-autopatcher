using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Net.NetworkInformation;

namespace ManualPatcher
{
    class NetworkOptions
    {
        public static string installpath = "";
        public static string TempFolder { get; private set; }

        private static int _loopbackIndex = -1;

        private static void TryGetLoopbackIdx()
        {
            if (_loopbackIndex != -1) return;

            string output = RunAndLog("netsh", "int ip sh int");
            string[] lines = output.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Length; i++)
            {
                string[] blocks = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (var j = 0; j < blocks.Length; j++)
                {
                    if (blocks[j].Contains("Loopback"))
                    {
                        _loopbackIndex = int.Parse(blocks[0]);
                        LogText("Info", "Found loopback device: {0}", _loopbackIndex);
                        return;
                    }
                }
            }

            if (_loopbackIndex == -1)
            {
                LogText("Error", "No loopback adapter found");
                System.Windows.Forms.MessageBox.Show("Unable to get Loopback interface index!");
                Environment.Exit(100);
            }
        }

        public static void FixLoopback()
        {
            RunAndLog("netsh", "int ip set dns " + _loopbackIndex + " dhcp");
        }

        public static void CreateNewLoopback(string[] ips)
        {
            LogText("Info", "Creating loopback devices starting");

            TryGetLoopbackIdx();
            FixLoopback();

            LogText("Info", "Retrieving IP addies of loopback interface");
            RunAndLog("netsh", "int ip show ipaddresses int=" + _loopbackIndex);
            LogText("Info", "Retrieving routing table");
            RunAndLog("netsh", "int ip show route");

            foreach (var ip in ips)
                RunAndLog("netsh", "int ip add addr " + _loopbackIndex + " address=" + ip + " mask=255.255.255.255 st=ac");

            LogText("Info", "Retrieving IP addies of loopback interface");
            RunAndLog("netsh", "int ip show ipaddresses int=" + _loopbackIndex);
            LogText("Info", "Retrieving routing table");
            RunAndLog("netsh", "int ip show route");

            LogText("Info", "Creating loopback devices finished");
        }

        public static void RemoveLoopback(string[] ips)
        {
            LogText("Info", "Removing loopback devices starting");

            TryGetLoopbackIdx();
            foreach (var ip in ips)
                RunAndLog("netsh", "int ip delete addr " + _loopbackIndex + " address=" + ip);

            LogText("Info", "Removing loopback devices finished");
        }

        static string RunAndLog(string filename, string args)
        {
            LogText("Running CMD", "{0} {1}", filename, args);
            Process process = new Process();
            process.StartInfo.FileName = filename;
            process.StartInfo.Arguments = args;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            if (System.Environment.OSVersion.Version.Major >= 6)
            {
                process.StartInfo.Verb = "runas";
            }

            process.Start();

            while (!process.HasExited)
                Thread.Sleep(100);

            LogText("Exit code", "{0}", process.ExitCode);
            string output = process.StandardOutput.ReadToEnd();
            LogText("Result", "{0}", output);

            return output;
        }

        private static void LogText(string type, string text, params object[] format)
        {
            using (StreamWriter sw = new StreamWriter(File.Open("log.txt", FileMode.Append, FileAccess.Write, FileShare.Read)))
            {
                sw.WriteLine("[{0}]{1} = {2}", DateTime.Now.ToString("R"), type, string.Format(text, format).Trim());
            }
        }
    }
}