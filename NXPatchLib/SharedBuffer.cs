using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace NXPatchLib
{
    static class SharedBuffer
    {
        public const int BUFFER_SIZE = 0x00500000; // 0x00050000 is default in MapleStory

        public static ThreadLocal<byte[]> Buffer = new ThreadLocal<byte[]>(() =>
        {
            Console.WriteLine($"Creating new buffer of {BUFFER_SIZE} bytes on thread {Thread.CurrentThread.ManagedThreadId}");
            return new byte[BUFFER_SIZE];
        });
    }
}
