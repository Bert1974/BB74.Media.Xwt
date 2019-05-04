using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BaseLib.Media.Interop
{
    static class staticinit
    {
        private static bool doinit = true;

        public static void Initialize()
        {
            if (doinit)
            {
                doinit = false;
           /*     string dir = Path.GetDirectoryName(new Uri(typeof(staticinit).Assembly.CodeBase).AbsolutePath);
                Assembly a;
                if (IntPtr.Size == 8)
                {
                    a = Assembly.LoadFile(Path.Combine(dir, "x64\\", "Media.Interop.Impl.dll"));
                }
                else
                {
                    a = Assembly.LoadFile(Path.Combine(dir, "x86\\", "Media.Interop.Impl.dll"));
                }*/
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
        }
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = args.Name.Split(',')[0];
            string fn = null;

            if (IntPtr.Size == 8)
            {
                fn = Path.Combine("x64", $"{name}.dll");
            }
            else
            {
                fn = Path.Combine("x86", $"{name}.dll");
            }
            fn = Path.Combine(Path.GetDirectoryName(new Uri(typeof(staticinit).Assembly.CodeBase).AbsolutePath),fn);

            if (File.Exists(fn))
            {
                try
                {
                    Assembly a = Assembly.LoadFile(fn);

                    if (a != null)
                    {
                        return a;
                    }
                }
                catch { }

           //     Log.Error($"failed to load '{fn}'.");
                return null;
            }
         //   Log.Error($"failed to resolve '{name}'.");
            return null;
        }
    }
}
