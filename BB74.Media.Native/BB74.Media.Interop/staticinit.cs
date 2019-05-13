using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using BaseLib.Interop;

namespace BaseLib.Media.Interop
{
    static class staticinit
    {
        private static object messagefunc;
        private static bool doinit = true;

        public static void Initialize()
        {
            if (doinit)
            {
                doinit = false;

                string dir = Path.GetDirectoryName(new Uri(typeof(staticinit).Assembly.Location).AbsolutePath);
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
        }
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("BB74.Media.Interop"))
            {
                try
                {
                    string dir = Path.GetDirectoryName(new Uri(typeof(staticinit).Assembly.Location).AbsolutePath);

                    if (IntPtr.Size == 8)
                    {
                        return Assembly.LoadFile(Path.Combine(dir, "BB74.Media.Interop.x64.dll"));
                    }
                    else
                    {
                        return Assembly.LoadFile(Path.Combine(dir, "BB74.Media.Interop.x86.dll"));
                    }
                }
                catch { }
            }
            return null;
        }

        internal static void Initialize2()
        {
            try
            {
                messagefunc = new BaseLib.Interop.messagefunction(message);
                BaseLib.Interop.Imports.__setprintf(Marshal.GetFunctionPointerForDelegate((Delegate)messagefunc));
            }
            catch (Exception e)
            {

            }
        }
        private static void message(string message)
        {
            Console.WriteLine(message);
        }
    }
}
