using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.IO;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;

namespace NXPatchLib
{
    public class PatchFile
    {
        public static byte MaxThreads = 8;

        Stream _stream;
        BinaryReader _reader;

        public string Filename { get; private set; }
        private string rawPatchfilename;

        public PatchFile(string filename)
        {
            Filename = filename;
            _stream = File.OpenRead(Filename);
            _reader = new BinaryReader(_stream);
        }

        public void Parse(string inputDirectory, string outputDirectory, out List<IPatchResult> patchResults)
        {
            var startTime = DateTime.Now;

            if (Encoding.ASCII.GetString(_reader.ReadBytes(8)) != "WzPatch\x1A")
                throw new Exception("Not a WzPatch file");

            _reader.ReadInt32(); // Skipped; version number?

            uint checksum = _reader.ReadUInt32();

            Console.WriteLine("Checking patchfile checksum....");
            if (CRC32.CalculateChecksumStream(_stream) != checksum)
                throw new Exception("Patch file is corrupt");

            _reader.BaseStream.Seek(16, SeekOrigin.Begin);

            Console.WriteLine("Inflating patchfile....");

            _stream.Seek(2, SeekOrigin.Current); // Skip zlib header
            var inflatedPatchfile = zlibInflate(_stream);

            // Cleanup the previous stream
            _stream.Close();
            _stream.Dispose();

            // Continue with new stream
            _stream = inflatedPatchfile;
            _stream.Position = 0;

            Directory.CreateDirectory(outputDirectory);

            rawPatchfilename = Path.Combine(outputDirectory, "rawpatch.bin");

            Console.WriteLine($"Saving raw patchdata to disk @ {rawPatchfilename}");

            using (var fs = File.OpenWrite(rawPatchfilename))
            {
                _stream.CopyTo(fs);
            }

            _stream.Position = 0;

            var patchSize = _stream.Length;

            Console.WriteLine("Saved patchfile");

            _reader = new BinaryReader(_stream);

            parseSteps(inputDirectory, outputDirectory, out patchResults);

            File.Delete(rawPatchfilename);

            File.WriteAllText(Path.Combine(outputDirectory, $"MS-AutoPatcher log {DateTime.Now.ToString("yyyyMMdd HHmmss")}.log"), $@"
Patchfile: {Filename}
Raw patch size {patchSize} bytes
Total time: {DateTime.Now.Subtract(startTime)}
Results:
{string.Join(Environment.NewLine, patchResults.Select(x => x.Filename + "\t\t" + x.Info))}
");

        }

        private void parseSteps(string inputDirectory, string outputDirectory, out List<IPatchResult> patchResults)
        {
            var _patchResults = new List<IPatchResult>();
            string filename = "";

            List<Tuple<PatchStep, long, long>> stepsToDo = new List<Tuple<PatchStep, long, long>>();

            var stepCount = 0;
            {
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
                        Console.WriteLine($"Got step {step} for file {filename} @ {startPos}");
                        stepCount++;

                        if (step is PatchStepDelete)
                        {
                            _patchResults.Add(step.Parse(inputDirectory, outputDirectory));
                        }
                        else {
                            step.Prepare(_reader);
                            var endPos = _reader.BaseStream.Position;
                            stepsToDo.Add(new Tuple<PatchStep, long, long>(step, startPos, endPos - startPos));
                        }

                        filename = "";
                        step = null;
                    }
                } while (_reader.BaseStream.Position != _reader.BaseStream.Length);
            }

            _reader.Close();
            _reader.Dispose();
            _reader = null;
            _stream.Close();
            _stream.Dispose();
            _stream = null;


            ConcurrentQueue<Tuple<PatchStep, long, long>> steps = new ConcurrentQueue<Tuple<PatchStep, long, long>>();
            stepsToDo.Sort((x, y) =>
            {
                if (x.Item3 < y.Item3) return 1;
                if (x.Item3 > y.Item3) return -1;
                return 0;
            });
            stepsToDo.ForEach(x =>
            {
                Console.WriteLine($"Enqueueing {x.Item1.Filename} (patch block size: {x.Item3} bytes)");
                steps.Enqueue(x);
            });

            stepsToDo.Clear();
            stepsToDo = null;



            var readerThreads = new List<Thread>();
            var startTime = DateTime.Now;
            for (var i = 0; i < MaxThreads; i++)
            {
                var thread = new Thread(x =>
                {

                    while (true)
                    {
                        Tuple<PatchStep, long, long> _kvp;
                        if (!steps.TryDequeue(out _kvp))
                        {
                            break;
                        }

                        var step = _kvp.Item1;

                        Console.WriteLine($"Patching {step.Filename} on thread {Thread.CurrentThread.ManagedThreadId}...");

                        _patchResults.Add(step.Parse(inputDirectory, outputDirectory));

                        step.Unprepare();

                        Console.WriteLine($"Patched {step.Filename} on thread {Thread.CurrentThread.ManagedThreadId}");
                    }
                    SharedBuffer.Buffer.Value = null;
                });

                readerThreads.Add(thread);
                thread.Start(i);
            }


            readerThreads.ForEach(x => x.Join());
            readerThreads.Clear();
            readerThreads = null;
            steps = null;

            Console.WriteLine($"Patched with {MaxThreads} threads {stepCount} patch steps in {DateTime.Now.Subtract(startTime).ToString()}");

            patchResults = _patchResults;

            GC.Collect();
        }

        private static MemoryStream zlibInflate(Stream input)
        {
            var output = new MemoryStream();
            using (var ds = new DeflateStream(input, CompressionMode.Decompress))
            {
                ds.CopyTo(output);
                ds.Close();
            }

            return output;
        }


    }
}
