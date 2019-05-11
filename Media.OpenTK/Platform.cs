using BaseLib.Media.Display;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xwt;

namespace BaseLib.Media.OpenTK
{
    public static class Platform
    {
        public static PlatformID OSPlatform
        {
            get
            {
                if (System.Environment.OSVersion.Platform != PlatformID.MacOSX &&
                    System.Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    return PlatformID.Win32Windows;
                }
                else
                {
                    if (Directory.Exists("/Applications")
                           & Directory.Exists("/System")
                           & Directory.Exists("/Users")
                           & Directory.Exists("/Volumes"))
                    {
                        return PlatformID.MacOSX;
                    }
                    else
                    {
                        return PlatformID.Unix;
                    }
                }
            }
        }
        public static IXwtRender TryLoad(ToolkitType type, out IRendererFactory renderfactory)
        {
           /* switch (OSPlatform)
            {
                case PlatformID.MacOSX:
                    return TryLoad("XamMac", type, out renderfactory);

                default:*/
                    switch (type)
                    {
                        case ToolkitType.XamMac:
                            return TryLoad("XamMac", type, out renderfactory);
                        case ToolkitType.Wpf:
                            return TryLoad("WPF", type, out renderfactory);
                        case ToolkitType.Gtk:
                        case ToolkitType.Gtk3:
                            return TryLoad("GTK", type, out renderfactory);
                    }
                 /*   break;
            }*/
            throw new NotImplementedException();
        }
        private static BaseLib.Media.OpenTK.IXwtRender TryLoad(string type, ToolkitType toolkit, out IRendererFactory renderfactory)
        {
            try
            {
                var a = Assembly.Load($"Media.OpenTK.{type}");
                var t = a.GetType($"BaseLib.Platforms.{type}");
                var o = new object[] { null };
                var r = (BaseLib.Media.OpenTK.IXwtRender)Activator.CreateInstance(t, o);
                renderfactory = o[0] as BaseLib.Media.Display.IRendererFactory;
                return r;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                //   Log.LogException(e);
                throw;
            }
        }
    }
}
