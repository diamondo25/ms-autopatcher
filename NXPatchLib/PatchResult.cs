using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NXPatchLib
{
    public interface IPatchResult
    {
        string Filename { get; }
        string Info { get; }
    }

    public class PatchResultFileNotFound : IPatchResult
    {
        public string Filename { get; }
        public string Info { get; private set; }
        public PatchResultFileNotFound(string filename) { Filename = filename; Info = "File not found"; }
    }

    public class PatchResultOriginalFileCorrupt : IPatchResult
    {
        public string Filename { get; }
        public string Info { get; private set; }
        public PatchResultOriginalFileCorrupt(string filename) { Filename = filename; Info = "Original file is corrupt (checksum mismatch)"; }
    }

    public class PatchResultPatchedFileCorrupt : IPatchResult
    {
        public string Filename { get; }
        public string Info { get; private set; }
        public PatchResultPatchedFileCorrupt(string filename) { Filename = filename; Info = "Patched file is corrupt (checksum mismatch)"; }
    }

    public class PatchResultSuccessful : IPatchResult
    {
        public string Filename { get; }
        public string Info { get; private set; }
        public PatchResultSuccessful(string filename) { Filename = filename; Info = "Successful"; }
    }


    public class PatchResultFileDeleted : IPatchResult
    {
        public string Filename { get; }
        public string Info { get; private set; }
        public PatchResultFileDeleted(string filename) { Filename = filename; Info = "Successfully deleted file"; }
    }


}
