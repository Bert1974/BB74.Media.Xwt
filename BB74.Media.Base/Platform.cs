using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Base
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
    }
}
