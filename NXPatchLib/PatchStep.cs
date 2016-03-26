using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NXPatchLib
{

    abstract public class PatchStep
    {
        public string Name { get; protected set; }
        public string Filename { get; private set; }
        public bool IsFile { get { return !Filename.EndsWith("\\"); } }


        public MemoryStream PreparedStream { get; protected set; } = null;

        public string DescFormat { get { return Name + " " + Filename; } }

        public PatchStep(string name, string filename)
        {
            Name = name;
            Filename = filename;
        }

        public virtual IPatchResult Parse(string inputDir, string outputDir)
        {
            return null;
        }

        public virtual void Prepare(BinaryReader reader)
        {
        }

        public void Unprepare()
        {
            if (PreparedStream == null) return;

            PreparedStream.Close();
            PreparedStream.Dispose();
            PreparedStream = null;
        }
    }

    public class PatchStepCreate : PatchStep
    {

        public PatchStepCreate(string filename) : base("Create", filename)
        {
        }

        public override void Prepare(BinaryReader reader)
        {
            if (!IsFile) return;

            var start = reader.BaseStream.Position;
            var Length = reader.ReadInt32();
            var Checksum = reader.ReadUInt32();

            var blob = new byte[Length + 8];
            reader.BaseStream.Position = start;
            reader.Read(blob, 0, blob.Length);
            PreparedStream = new MemoryStream(blob);
        }

        public override IPatchResult Parse(string inputDir, string outputDir)
        {
            if (!IsFile)
            {
                var path = Path.Combine(outputDir, Filename).TrimEnd('\\');
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                return new PatchResultSuccessful(path);
            }

            using (var reader = new BinaryReader(PreparedStream))
            {
                var Length = reader.ReadInt32();
                var Checksum = reader.ReadUInt32();



                var calculatedChecksum = CRC32.CalculateChecksumStream(PreparedStream);
                PreparedStream.Position = 8;

                var outputFile = Path.Combine(outputDir, Filename);

                Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

                using (var os = File.OpenWrite(outputFile))
                {
                    PreparedStream.CopyTo(os);
                }

                if (calculatedChecksum != Checksum)
                    return new PatchResultPatchedFileCorrupt(outputFile);

                return new PatchResultSuccessful(outputFile);
            }
        }
    }

    public class PatchStepDelete : PatchStep
    {
        public PatchStepDelete(string filename) : base("Delete", filename)
        {
        }

        public override IPatchResult Parse(string inputDir, string outputDir)
        {
            var inputFile = Path.Combine(inputDir, Filename);

            if (!File.Exists(inputFile))
                return new PatchResultFileNotFound(inputFile);
            else
                return new PatchResultFileDeleted(inputFile);
        }
    }

    public class PatchStepChange : PatchStep
    {

        public PatchStepChange(string filename) : base("Change", filename)
        {
        }


        public override void Prepare(BinaryReader reader)
        {
            var start = reader.BaseStream.Position;

            var ChecksumBefore = reader.ReadUInt32();
            var ChecksumAfter = reader.ReadUInt32();

            for (uint Command = reader.ReadUInt32(); Command != 0x00000000; Command = reader.ReadUInt32())
            {
                if ((Command & 0xC0000000) == 0xC0000000)
                {
                    byte repeatedByte = (byte)(Command & 0x000000FF);
                    int lengthOfBlock = (int)((Command & 0x3FFFFF00) >> 8);

                }
                else if ((Command & 0x80000000) == 0x80000000)
                {
                    int lengthOfBlock = (int)(Command & 0x7FFFFFFF);

                    reader.BaseStream.Position += lengthOfBlock;
                }
                else
                {
                    int lengthOfBlock = (int)Command;
                    int oldFileOffset = reader.ReadInt32();
                }
            }

            var end = reader.BaseStream.Position;

            var blob = new byte[end - start];
            reader.BaseStream.Position = start;
            reader.BaseStream.Read(blob, 0, blob.Length);

            PreparedStream = new MemoryStream(blob);
        }


        public override IPatchResult Parse(string inputDir, string outputDir)
        {
            using (var reader = new BinaryReader(PreparedStream))
            {
                var ChecksumBefore = reader.ReadUInt32();
                var outputFile = Path.Combine(outputDir, Filename);
                var inputFile = Path.Combine(inputDir, Filename);

                if (File.Exists(outputFile))
                {
                    File.Move(outputFile, outputFile + "-" + DateTime.Now.ToFileTime());
                }


                if (!File.Exists(inputFile))
                    return new PatchResultFileNotFound(inputFile);

                if (CRC32.CalculateChecksumFile(inputFile) != ChecksumBefore)
                    return new PatchResultOriginalFileCorrupt(inputFile);


                var ChecksumAfter = reader.ReadUInt32();

                uint currentChecksum = 0;

                using (var input = File.OpenRead(inputFile))
                using (var output = File.OpenWrite(outputFile))
                {
                    var sharedBuffer = SharedBuffer.Buffer.Value;
                    int bufferOffset = 0;

                    Action tryFlush = () =>
                    {
                        if (bufferOffset == 0) return;

                        currentChecksum = CRC32.CalculateChecksum(sharedBuffer, bufferOffset, currentChecksum);
                        output.Write(sharedBuffer, 0, bufferOffset);
                        bufferOffset = 0;
                    };


                    for (uint Command = reader.ReadUInt32(); Command != 0x00000000; Command = reader.ReadUInt32())
                    {
                        if ((Command & 0xC0000000) == 0xC0000000)
                        {
                            //This is a repeat block. It's essentially run length encoding.
                            byte repeatedByte = (byte)(Command & 0x000000FF);
                            int lengthOfBlock = (int)((Command & 0x3FFFFF00) >> 8);
                            //use memset in C to write to a buffer containing the repeatedByte for lengthOfBlock number of bytes, then write it to the file.

                            for (var i = 0; i < lengthOfBlock; i++)
                            {
                                sharedBuffer[bufferOffset++] = repeatedByte;
                            }
                            tryFlush();

                        }
                        else if ((Command & 0x80000000) == 0x80000000)
                        {
                            // This is a direct write block. The bytes to be written are contained directly in the zlib stream. Simply write these bytes out to the file.
                            int lengthOfBlock = (int)(Command & 0x7FFFFFFF);

                            while (lengthOfBlock > 0)
                            {
                                tryFlush();
                                var nextBlock = Math.Min(SharedBuffer.BUFFER_SIZE - bufferOffset, lengthOfBlock);

                                reader.Read(sharedBuffer, 0, nextBlock);
                                bufferOffset += nextBlock;

                                lengthOfBlock -= nextBlock;
                            }
                            tryFlush();

                        }
                        else
                        {
                            // COPY
                            //This means we take from the old file and write to the new file.
                            int lengthOfBlock = (int)Command;
                            int oldFileOffset = reader.ReadInt32();

                            input.Seek(oldFileOffset, SeekOrigin.Begin);
                            while (lengthOfBlock > 0)
                            {
                                tryFlush();
                                var nextBlock = Math.Min(SharedBuffer.BUFFER_SIZE - bufferOffset, lengthOfBlock);

                                input.Read(sharedBuffer, 0, nextBlock);
                                bufferOffset += nextBlock;

                                lengthOfBlock -= nextBlock;
                            }
                            tryFlush();
                        }
                    }

                    tryFlush();
                }


                if (currentChecksum != ChecksumAfter)
                    return new PatchResultPatchedFileCorrupt(outputFile);

                return new PatchResultSuccessful(outputFile);
            }
        }

    }
}