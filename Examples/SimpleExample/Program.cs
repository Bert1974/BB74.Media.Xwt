using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using BaseLib.Xwt;
using System;
using System.Linq;
using Xwt;

namespace SimpleExample
{
    static class Program
    {
        public static IRendererFactory RenderFactory { get; private set; }
        public static ToolkitType ToolkitType { get; private set; }
        public static IXwt Xwt { get; private set; }
        public static IXwtRender XwtRender { get; private set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // load engine
            try
            {
#if (__MACOS__)
                TryLoad(ToolkitType.XamMac);
#else
                if (BaseLib.Xwt.Platform.OSPlatform == PlatformID.MacOSX)
                {
                    if (args.Contains("-gtk"))
                    {
                        try { TryLoad(ToolkitType.Gtk); }
                        catch { TryLoad(ToolkitType.XamMac); }
                    }
                    else { TryLoad(ToolkitType.XamMac); }
                }
                else if (BaseLib.Xwt.Platform.OSPlatform == PlatformID.Unix)
                {
                    TryLoad(args.Contains("-gtk3") ? ToolkitType.Gtk3 : ToolkitType.Gtk);
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
            catch (Exception e) { Console.Error.WriteLine($"Error initializing/loading engine '{e.Message}'"); return; }

            // initialize Xwt (with dll load for ubuntu with both gtk2 and gtk3 installed)
            try { BaseLib.Xwt.Platform.Initialize(Program.ToolkitType); }
            catch (Exception e) { Console.Error.WriteLine($"Error initializing/loading xwt '{e.Message}'"); return; }

            // intitialize xwt helpers
            try { Program.Xwt = BaseLib.Xwt.XwtImpl.Create(); }
            catch (Exception e) { Console.Error.WriteLine($"Error initializing/loading xwt-platform-specific '{e.Message}'"); return; }

            // createwdinow

            var window = new MainWindow(RenderFactory, XwtRender, Xwt) { Width = 250, Height = 250, Title = "Triangle" };
            window.Show();
            Application.Run();
        }

        private static void TryLoad(ToolkitType type)
        {
            ToolkitType = type;
            XwtRender = BaseLib.Media.OpenTK.Platform.TryLoad(type, out IRendererFactory renderfactory);
            Program.RenderFactory = renderfactory;
        }
    }
}
