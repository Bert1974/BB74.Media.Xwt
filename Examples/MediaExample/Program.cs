using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xwt;

namespace DockExample
{
    static class UIHelpers
    {
        public static MenuItem NewMenuItem(string text, EventHandler click)
        {
            var r = new MenuItem(text);
            r.Clicked += click;
            return r;
        }
        public static void NewWindow()
        {
            var mainWindow = new mainwindow(Program.XwtRender)
            {
            };
            Program.AddWindow(mainWindow);
            mainWindow.Show();
        }
    }

    class Program
    {
        static readonly List<mainwindow> openwindows = new List<mainwindow>();
        
        public static BaseLib.Media.OpenTK.IXwtRender XwtRender { get; private set; }
        public static BaseLib.Xwt.IXwt Xwt { get; private set; }
        public static BaseLib.Media.Display.IRendererFactory Render { get; private set; }

        private static BaseLib.Media.OpenTK.IXwtRender TryLoad(ToolkitType toolkit)
        {
            try
            {
                string type;
                switch (toolkit)
                {
                    case ToolkitType.XamMac: type = "XamMac"; break;
                    case ToolkitType.Gtk: type = "GTK"; break;
                    case ToolkitType.Gtk3: type = "GTK"; break;
                    case ToolkitType.Wpf: type = "WPF"; break;
                    default: throw new NotImplementedException();
                }
                var a = Assembly.Load($"BB74.Xwt.OpenTK.{type}");
                var t = a.GetType($"BaseLib.Platforms.{type}");
                var o = new object[] { null };
                var r = (BaseLib.Media.OpenTK.IXwtRender)Activator.CreateInstance(t, o);
                Program.Render = o[0] as BaseLib.Media.Display.IRendererFactory;

                BaseLib.Xwt.Platform.Initialize(toolkit);

                return r;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        [STAThread()]
        static void Main(string[] args)
        {
            try
            {
#if (__MACOS__)
                XwtRender = TryLoad("XamMac", ToolkitType.XamMac);
#else
                if (BaseLib.Xwt.Platform.OSPlatform == PlatformID.MacOSX)
                {
                    if (args.Contains("-gtk"))
                    {
                        try { XwtRender = TryLoad(ToolkitType.Gtk); }
                        catch { XwtRender = TryLoad(ToolkitType.XamMac); }
                    }
                    else { XwtRender = TryLoad(ToolkitType.XamMac); }
                }
                else if (BaseLib.Xwt.Platform.OSPlatform == PlatformID.Unix)
                {
                    XwtRender = TryLoad(args.Contains("-gtk3") ? ToolkitType.Gtk3 : ToolkitType.Gtk);
                }
                else
                {
                    if (args.Contains("gtk"))
                    {
                        try
                        {
                            XwtRender = TryLoad(ToolkitType.Gtk); // i386 only
                        }
                        catch (Exception e)
                        {
                            XwtRender = TryLoad(ToolkitType.Wpf);
                        }
                    }
                    else
                    {
                        XwtRender = TryLoad(ToolkitType.Wpf);
                    }
                }
#endif
            }
            catch (Exception e)
            {
                return;
            }
            try
            {
                Program.Xwt = BaseLib.Xwt.XwtImpl.Create();

                UIHelpers.NewWindow();
                Application.Run();
            }
            catch (Exception e)
            {
            }
        }
        public static void AddWindow(mainwindow window)
        {
            openwindows.Add(window);
        }
        public static bool RemoveWindow(mainwindow window)
        {
            openwindows.Remove(window);
            return openwindows.Count == 0;
        }
    }
}