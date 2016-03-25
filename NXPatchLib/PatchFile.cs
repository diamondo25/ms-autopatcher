using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.IO;
using System.IO.Compression;

namespace NXPatchLib
{
    public class PatchFile
    {
        FileStream _stream;
        BinaryReader _reader;

        public string Filename { get; private set; }
        private string rawPatchfilename;

        public PatchFile(string filename)
        {
            Filename = filename;
            _stream = File.OpenRead(Filename);
            _reader = new BinaryReader(_stream);
        }

        public void Parse(string inputDirectory, string outputDirectory, out List<string> filesOkay, out List<string> filesBad)
        {
            if (Encoding.ASCII.GetString(_reader.ReadBytes(8)) != "WzPatch\x1A")
                throw new Exception("Not a WzPatch file");

            _reader.ReadInt32(); // Skipped; version number?

            int checksum = _reader.ReadInt32();

            if (!validateFile(_stream, (uint)checksum))
                throw new Exception("Patch file is corrupt");

            _reader.BaseStream.Seek(16, SeekOrigin.Begin);

            byte[] patchfile = zlibInflate(_reader.ReadBytes((int)(_reader.BaseStream.Length - _reader.BaseStream.Position)));
            rawPatchfilename = Path.Combine(outputDirectory, "rawpatch.bin");

            File.WriteAllBytes(rawPatchfilename, patchfile);
            _reader = new BinaryReader(new MemoryStream(patchfile));
            patchfile = null;

            parseSteps(inputDirectory, outputDirectory, out filesOkay, out filesBad);
        }
        
        private void parseSteps(string inputDirectory, string outputDirectory, out List<string> filesOkay, out List<string> filesBad)
        {
            var _filesOkay = new List<string>();
            var _filesBad = new List<string>();
            string filename = "";

            Dictionary<PatchStep, long> steps = new Dictionary<PatchStep, long>();


            PatchStep step = null;
            do
            {
                byte x = _reader.ReadByte();
                switch (x)
                {
                    case 0: step = new PatchStepCreate(filename); break;
                    case 1: step = new PatchStepChange(filename); break;
                    case 2: step = new PatchStepDelete(filename); break;
                    default:
                        filename += (char)x;
                        continue;
                }

                if (step != null)
                {
                    var startPos = _reader.BaseStream.Position;
                    step.QuickParse(_reader);

                    steps.Add(step, startPos);

                    filename = "";
                    step = null;
                }
            } while (_reader.BaseStream.Position != _reader.BaseStream.Length);

            _reader.Close();
            _reader.Dispose();

            var readerThreads = new List<Thread>();
            foreach (var kvp in steps)
            {
                var thread = new Thread(x =>
                {
                    var _kvp = (KeyValuePair<PatchStep, long>)x;

                    using (var reader = new BinaryReader(File.OpenRead(rawPatchfilename)))
                    {
                        reader.BaseStream.Position = _kvp.Value;
                        var handled = _kvp.Key.Parse(reader, inputDirectory, outputDirectory);
                        if (handled != null)
                        {
                            _filesOkay.Add(handled);
                        }
                        else
                        {
                            _filesBad.Add(_kvp.Key.Filename);
                        }
                    }
                });

                thread.Start(kvp);
                readerThreads.Add(thread);
            }

            readerThreads.ForEach(x => x.Join());
            filesOkay = _filesOkay;
            filesBad = _filesBad;
            GC.Collect();
            
        }

        private static byte[] zlibInflate(byte[] input)
        {
            using (var output = new MemoryStream())
            using (var ms = new MemoryStream(input.Skip(2).ToArray()))
            using (var ds = new DeflateStream(ms, CompressionMode.Decompress))
            {
                ds.CopyTo(output);
                ds.Close();
                return output.ToArray();
            }
        }

        private static bool validateFile(Stream _stream, uint checksum)
        {
            uint rollingSum = 0;
            long left = _stream.Length - _stream.Position;
            do
            {
                int blockSize = (int)Math.Min(SharedBuffer.BUFFER_SIZE, left);
                _stream.Read(SharedBuffer.Buffer.Value, 0, blockSize);
                rollingSum = CRC32.CalculateChecksum(SharedBuffer.Buffer.Value, blockSize, rollingSum);
                left -= blockSize;
            }
            while (left > 0);

            return checksum == rollingSum;
        }

    }
}
