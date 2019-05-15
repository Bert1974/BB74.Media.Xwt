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

        private static BaseLib.Media.OpenTK.IXwtRender TryLoad(string type, ToolkitType toolkit)
        {
            try
            {
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
                if (System.Environment.OSVersion.Platform == PlatformID.Unix || System.Environment.OSVersion.Platform == PlatformID.MacOSX)
                {
                    XwtRender = TryLoad("GTK", args.Contains("gtk3") ? ToolkitType.Gtk3 : ToolkitType.Gtk);
                }
                else
                {
                    if (args.Contains("gtk"))
                    {
                        try
                        {
                            XwtRender = TryLoad("GTK", ToolkitType.Gtk); // i386 only
                        }
                        catch (Exception e)
                        {
                            XwtRender = TryLoad("WPF", ToolkitType.Wpf);
                        }
                    }
                    else
                    {
                        XwtRender = TryLoad("WPF", ToolkitType.Wpf);
                    }
                }
#endif
            }
            catch
            {
                return;
            }
            Program.Xwt = BaseLib.Xwt.XwtImpl.Create();

            UIHelpers.NewWindow();
            Application.Run();
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