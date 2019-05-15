using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using BaseLib.Xwt;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xwt;

namespace MovieExample
{
    static class Program
    {
        public static IRendererFactory RenderFactory { get; private set; }
        public static ToolkitType ToolkitType { get; private set; }
        public static IXwt Xwt { get; private set; }
        public static IXwtRender XwtRender { get; private set; }


        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Console.Error.WriteLine($"assembly not found {args.Name}");
            return null;
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            var a = Assembly.Load("OpenTK, Version=3.0.1.0, Culture=neutral, PublicKeyToken=bad199fe84eb3df4");

            if (a.FullName.EndsWith("bad199fe84eb3df4"))
            {
              //  Console.WriteLine("windows");
            }
            else if (a.FullName.EndsWith("84e04ff9cfb79065"))
            {
                //    Console.WriteLine("osx");
                Console.Error.WriteLine($"Error loading opentk.dll");
            }
            try
            {
                // load engine
                try
                {
#if (__MACOS__)
                    if (args.Contains("gtk"))
                    {
                        try { TryLoad(ToolkitType.Gtk); }// i386 only 
                        catch { TryLoad(ToolkitType.XamMac); }
                    }
                    else
                    {
                        TryLoad(ToolkitType.XamMac);
                    }
#else
                    if (System.Environment.OSVersion.Platform == PlatformID.Unix || System.Environment.OSVersion.Platform == PlatformID.MacOSX)
                    {
                        TryLoad(args.Contains("gtk3") ? ToolkitType.Gtk3 : ToolkitType.Gtk);
                    }
                    else // assume windows
                    {
                        if (args.Contains("gtk"))
                        {
                            try { TryLoad(ToolkitType.Gtk); }// i386 only 
                            catch { TryLoad(ToolkitType.Wpf); }
                        }
                        else { TryLoad(ToolkitType.Wpf); }
                    }
#endif
                }
                catch (Exception e)
                { Console.Error.WriteLine($"Error initializing/loading engine '{e.Message}'"); return; }

                // initialize Xwt (with dll load for ubuntu with both gtk2 and gtk3 installed)
                try { BaseLib.Xwt.Platform.Initialize(Program.ToolkitType); }
                catch (Exception e)
                { Console.Error.WriteLine($"Error initializing/loading xwt '{e.Message}'"); return; }

                // intitialize xwt helpers
                try { Program.Xwt = BaseLib.Xwt.XwtImpl.Create(); }
                catch (Exception e) { Console.Error.WriteLine($"Error initializing/loading xwt-platform-specific '{e.Message}'"); return; }

                // createwdinow

                var window = new MainWindow(RenderFactory, XwtRender, Xwt) { Width = 250, Height = 250, Title = "Triangle" };
                window.Show();
                Application.Run();
            }
            catch (Exception e)
            {
            }
        }
        private static void TryLoad(ToolkitType type)
        {

            ToolkitType = type;
            XwtRender = BaseLib.Media.OpenTK.Platform.TryLoad(type, out IRendererFactory renderfactory);
            Program.RenderFactory = renderfactory;
        }
    }
}
