using System;
using System.Collections.Generic;
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
            var mainWindow = new mainwindow(Program.Xwt)
            {
            };
            Program.AddWindow(mainWindow);
            mainWindow.Show();
        }
    }

    class Program
    {
        static readonly List<mainwindow> openwindows = new List<mainwindow>();


        public static BaseLib.Media.OpenTK.IXwtRender Xwt { get; private set; }
        public static BaseLib.Media.Display.IRendererFactory Render { get; private set; }

        private static BaseLib.Media.OpenTK.IXwtRender TryLoad(string type)
        {
            try
            {
                var a = Assembly.Load($"Media.OpenTK.{type}");
                var t = a.GetType($"BaseLib.Platforms.{type}");
                var o = new object[] { null };
                var r = (BaseLib.Media.OpenTK.IXwtRender)Activator.CreateInstance(t, o);
                Program.Render = o[0] as BaseLib.Media.Display.IRendererFactory;
                return r;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
             //   Log.LogException(e);
            }
            throw new Exception();
            return null;
            // Program.IXwt = new BBR.Platforms.WPF(out Program.Render);
        }

        [STAThread()]
        static void Main(string[] args)
        {
#if (__MACOS__)
           Xwt = TryLoad("XamMac");
#else
            if (System.Environment.OSVersion.Platform == PlatformID.Unix || System.Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                Xwt = TryLoad("GTK");

          //      BaseLib.Xwt.PlatForm.Initialize(args.Contains("gtk3")? ToolkitType.Gtk3 : ToolkitType.Gtk);
            }
            else
            {
                  try
                   {
                  //     var a = Assembly.Load(new AssemblyName("gdk-sharp"));

                   //   if (a != null)
                       {
                //           Xwt = TryLoad("GTK"); // i386 only
                       }
                   }
                   catch(Exception e)
                   {
                   }
                 Xwt = TryLoad("WPF");
                //   Application.Initialize(ToolkitType.Wpf);
            }
#endif
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