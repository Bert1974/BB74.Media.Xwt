using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLib.IO
{
    public static class PathHelper
    {
        public static string StripPath(string basedir, string filename)
        {
            if (!string.IsNullOrWhiteSpace(basedir))
            {
                if (!basedir.EndsWith(Path.DirectorySeparatorChar.ToString())) { basedir += Path.DirectorySeparatorChar; }

                if (filename.StartsWith(basedir, true, null))
                {
                    return filename.Substring(basedir.Length);
                }
            }
            return filename;
        }
        public static string GetDirectoryName(string dir)
        {
            if (!string.IsNullOrWhiteSpace(dir))
            {
                return System.IO.Path.GetDirectoryName(dir) ?? System.IO.Path.GetPathRoot(dir) ?? dir;
            }
            return dir;
        }
        public static string MakePath(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString())) { return path + Path.DirectorySeparatorChar; }
            return path;
        }
    }
}