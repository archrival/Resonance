using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Resonance.Common
{
    public static class FileUtilities
    {
        public static List<FileInfo> FindFiles(string path, string pattern = "*.*", bool recursive = true)
        {
            var files = new List<FileInfo>();

            var di = new DirectoryInfo(path);

            if (!di.Exists)
            {
                return files;
            }

            files = FindFilesRecursive(files, path, pattern, recursive);

            return files;
        }

        private static List<FileInfo> FindFilesRecursive(List<FileInfo> files, string path, string pattern = "*.*", bool recursive = true)
        {
            var di = new DirectoryInfo(path);

            if (!di.Exists)
            {
                return files;
            }

            files.AddRange(di.GetFiles(pattern));

            return !recursive ? files : di.EnumerateDirectories().Aggregate(files, (current, dir) => FindFilesRecursive(current, dir.FullName, pattern));
        }
    }
}