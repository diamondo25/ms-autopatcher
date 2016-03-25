using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace MS_AutoPatcher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var file = new NXPatchLib.PatchFile(@"C:\Users\Erwin\Documents\ms-autopatcher\MS-AutoPatcher\bin\Debug\Global\Downloads\00091to00092.patch");
            List<string> goodfiles, badfiles;
            file.Parse(@"C:\Nexon\MapleStory V.84 to V.95", @"C:\Nexon\MapleStory V.84 to V.95\Prepatch_91", out goodfiles, out badfiles);

            

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
