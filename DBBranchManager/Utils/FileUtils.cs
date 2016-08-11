using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DBBranchManager.Utils
{
    internal static class FileUtils
    {
        public static IEnumerable<string> EnumerateFiles(string path, Func<string, bool> filter)
        {
            return Directory.EnumerateFiles(path)
                .Select(Path.GetFileName)
                .Where(filter)
                .OrderBy(x => x, new NaturalSortComparer());
        }

        public static IEnumerable<FileData> EnumerateFiles2(string path, Func<string, bool> filter)
        {
            return Directory.EnumerateFiles(path)
                .Select(x => new FileData(x))
                .Where(x => filter(x.FileName))
                .OrderBy(x => x.FileName, new NaturalSortComparer());
        }

        public static IEnumerable<FileData> EnumerateDirectories2(string path, Func<string, bool> filter)
        {
            return Directory.EnumerateDirectories(path)
                .Select(x => new FileData(x))
                .Where(x => filter(x.FileName))
                .OrderBy(x => x.FileName, new NaturalSortComparer());
        }

        public static void DeleteDirectory(string directory)
        {
            var dir = new DirectoryInfo(directory);
            RemoveReadOnlyRecursive(dir);
            dir.Delete(true);
        }

        public static FileStream AcquireFile(string path, FileMode mode, FileAccess access, FileShare share, int sleepTime = 250)
        {
            while (true)
            {
                try
                {
                    var fs = new FileStream(path, mode, access, share);

                    fs.ReadByte();
                    fs.Seek(0, SeekOrigin.Begin);

                    return fs;
                }
                catch (IOException)
                {
                    Thread.Sleep(sleepTime);
                }
            }
        }

        private static void RemoveReadOnlyRecursive(DirectoryInfo dir)
        {
            foreach (var subDirectory in dir.GetDirectories())
            {
                RemoveReadOnlyRecursive(subDirectory);
            }
            foreach (var fileInfo in dir.GetFiles())
            {
                fileInfo.Attributes &= ~FileAttributes.ReadOnly;
            }
        }

        public static IEnumerable<string> ExpandGlob(string glob)
        {
            return GlobHelper(PathHead(glob) + Path.DirectorySeparatorChar, PathTail(glob));
        }

        private static IEnumerable<string> GlobHelper(string head, string tail)
        {
            if (PathTail(tail) == tail)
                return Directory.GetFiles(head, tail).OrderBy(x => x);

            return Directory.GetDirectories(head, PathHead(tail)).OrderBy(x => x)
                .SelectMany(x => GlobHelper(Path.Combine(head, x), PathTail(tail)));
        }

        private static string PathHead(string path)
        {
            var index = path.IndexOf(Path.DirectorySeparatorChar);
            if (index < 0)
                return path;
            return path.Substring(0, index);
        }

        private static string PathTail(string path)
        {
            var index = path.IndexOf(Path.DirectorySeparatorChar);
            if (index < 0)
                return path;
            return path.Substring(index + 1);
        }

        public static string ToLocalPath(string path)
        {
            return path == null ? null : path.Replace('/', Path.DirectorySeparatorChar);
        }

        public static string ToLocalPath(params string[] paths)
        {
            return Path.Combine(paths
                .Where(x => x != null)
                .Select(ToLocalPath)
                .ToArray());
        }

        public class FileData
        {
            private readonly string mFullPath;
            private readonly string mFileName;

            public FileData(string fullPath)
            {
                mFullPath = fullPath;
                mFileName = Path.GetFileName(fullPath);
            }

            public string FullPath
            {
                get { return mFullPath; }
            }

            public string FileName
            {
                get { return mFileName; }
            }
        }
    }
}
