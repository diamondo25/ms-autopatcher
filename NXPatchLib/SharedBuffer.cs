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
        public const int BUFFER_SIZE = 0x02000000;

        public static ThreadLocal<byte[]> Buffer = new ThreadLocal<byte[]>(() => new byte[BUFFER_SIZE]);

    }
}
