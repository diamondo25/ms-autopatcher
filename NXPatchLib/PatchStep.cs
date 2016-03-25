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

        public string DescFormat { get { return Name + " " + Filename; } }

        public PatchStep(string name, string filename)
        {
            Name = name;
            Filename = filename;
        }

        public virtual string Parse(BinaryReader reader, string inputDir, string outputDir)
        {
            return null;
        }

        public virtual void QuickParse(BinaryReader reader)
        {
        }
    }

    public class PatchStepCreate : PatchStep
    {
        public PatchStepCreate(string filename) : base("Create", filename)
        {
        }
        
        public override void QuickParse(BinaryReader reader)
        {
            if (!IsFile) return;
            var Length = reader.ReadInt32();
            var Checksum = reader.ReadUInt32();

            reader.BaseStream.Position += Length;
        }

        public override string Parse(BinaryReader reader, string inputDir, string outputDir)
        {
            if (!IsFile)
            {
                var path = Path.Combine(outputDir, Filename).TrimEnd('\\');
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                return path;
            }

            var Length = reader.ReadInt32();
            var Checksum = reader.ReadUInt32();

            uint rollingSum = 0;
            var outputFile = Path.Combine(outputDir, Filename);

            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            using (var os = File.OpenWrite(outputFile))
            {
                var left = Length;
                do
                {
                    int blockSize = (int)Math.Min(SharedBuffer.BUFFER_SIZE, left);
                    reader.Read(SharedBuffer.Buffer.Value, 0, blockSize);

                    // Write to file
                    os.Write(SharedBuffer.Buffer.Value, 0, blockSize);
                    rollingSum = CRC32.CalculateChecksum(SharedBuffer.Buffer.Value, blockSize, rollingSum);
                    left -= blockSize;
                }
                while (left > 0);
            }

            if (rollingSum != Checksum)
            {
                File.Move(outputFile, outputFile + "-BAD");
                return outputFile + "-BAD";
            }

            return outputFile;
        }
    }

    public class PatchStepDelete : PatchStep
    {
        public PatchStepDelete(string filename) : base("Delete", filename)
        {
        }

        public override string Parse(BinaryReader reader, string inputDir, string outputDir)
        {
            return Path.Combine(outputDir, Filename) + "-DELETED";
        }
    }

    public class PatchStepChange : PatchStep
    {
        public PatchStepChange(string filename) : base("Change", filename)
        {
        }


        public override void QuickParse(BinaryReader reader)
        {
            if (!IsFile) return;
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
        }


        public override string Parse(BinaryReader reader, string inputDir, string outputDir)
        {
            var ChecksumBefore = reader.ReadUInt32();
            var outputFile = Path.Combine(outputDir, Filename);
            var inputFile = Path.Combine(inputDir, Filename);

            if (File.Exists(outputFile))
            {
                File.Move(outputFile, outputFile + "-" + DateTime.Now.ToFileTime());
            }

            bool error = false;

            if (!File.Exists(inputFile) || CRC32.CalculateChecksumFile(inputFile) != ChecksumBefore)
            {
                error = true;
            }


            var ChecksumAfter = reader.ReadUInt32();

            uint currentChecksum = 0;
            uint flushCounter = 0;
            FileStream input = null, output = null;
            if (!error)
            {
                input = File.OpenRead(inputFile);
                output = File.OpenWrite(outputFile);
            }

            {

                int bufferOffset = 0;

                Action tryFlush = () =>
                {
                    //if ((SharedBuffer.BUFFER_SIZE - bufferOffset) < 1000)
                    {
                        // Flush.
                        currentChecksum = CRC32.CalculateChecksum(SharedBuffer.Buffer.Value, bufferOffset, currentChecksum);
                        output.Write(SharedBuffer.Buffer.Value, 0, bufferOffset);
                        output.Flush();
                        bufferOffset = 0;
                    }
                };


                for (uint Command = reader.ReadUInt32(); Command != 0x00000000; Command = reader.ReadUInt32())
                {
                    if ((Command & 0xC0000000) == 0xC0000000)
                    {
                        //This is a repeat block. It's essentially run length encoding.
                        byte repeatedByte = (byte)(Command & 0x000000FF);
                        int lengthOfBlock = (int)((Command & 0x3FFFFF00) >> 8);
                        //use memset in C to write to a buffer containing the repeatedByte for lengthOfBlock number of bytes, then write it to the file.

                        if (!error)
                        {

                            for (var i = 0; i < lengthOfBlock; i++)
                            {
                                SharedBuffer.Buffer.Value[bufferOffset++] = repeatedByte;
                            }
                            tryFlush();
                        }

                    }
                    else if ((Command & 0x80000000) == 0x80000000)
                    {
                        // This is a direct write block. The bytes to be written are contained directly in the zlib stream. Simply write these bytes out to the file.
                        int lengthOfBlock = (int)(Command & 0x7FFFFFFF);

                        if (!error)
                        {
                            reader.Read(SharedBuffer.Buffer.Value, 0, lengthOfBlock);
                            bufferOffset += lengthOfBlock;

                            tryFlush();
                        }
                        else
                        {
                            reader.BaseStream.Position += lengthOfBlock;
                        }
                    }
                    else
                    {
                        // COPY
                        //This means we take from the old file and write to the new file.
                        int lengthOfBlock = (int)Command;
                        int oldFileOffset = reader.ReadInt32();

                        if (!error)
                        {
                            input.Seek(oldFileOffset, SeekOrigin.Begin);
                            while (lengthOfBlock > 0)
                            {
                                tryFlush();
                                var nextBlock = Math.Min(SharedBuffer.BUFFER_SIZE - bufferOffset, lengthOfBlock);

                                input.Read(SharedBuffer.Buffer.Value, 0, nextBlock);
                                bufferOffset += nextBlock;

                                lengthOfBlock -= nextBlock;
                            }
                            tryFlush();
                        }
                    }
                }

                if (!error && bufferOffset > 0)
                {
                    currentChecksum = CRC32.CalculateChecksum(SharedBuffer.Buffer.Value, bufferOffset, currentChecksum);
                    output.Write(SharedBuffer.Buffer.Value, 0, bufferOffset);
                }
            }

            if (!error)
            {
                input.Close();
                output.Flush(true);
                output.Close();
            }
            else
            {
                return null;
            }

            if (currentChecksum != ChecksumAfter)
            {
                File.Move(outputFile, outputFile + "-BAD");
                return outputFile + "-BAD";
            }

            return outputFile;
        }

    }
}