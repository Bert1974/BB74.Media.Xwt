using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Media.Interop
{
    static class staticinit
    {
        public static void Initialize()
        {
            string dir = Path.GetDirectoryName(new Uri(typeof(staticinit).Assembly.CodeBase).AbsolutePath);
            if (IntPtr.Size == 8)
            {
                var a = Assembly.LoadFile(Path.Combine(dir, "x64\\", "Media.Interop.Impl.dll"));
            }
            else
            {
                var a = Assembly.LoadFile(Path.Combine(dir, "x86\\", "Media.Interop.Impl.dll"));
            }
        }
    }
}
