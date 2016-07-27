using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace DBBranchManager.Utils
{
    internal static class FileUtils
    {
        private const uint LOCKFILE_EXCLUSIVE_LOCK = 0x00000002;

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

        public static IDisposable Lock(FileStream stream)
        {
            return new LockFileExWrapper(stream, 0, ulong.MaxValue);
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

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern bool LockFileEx(SafeFileHandle handle, uint flags, uint reserved, uint countLow, uint countHigh, ref OVERLAPPED overlapped);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern bool UnlockFileEx(SafeFileHandle handle, uint reserved, uint countLow, uint countHigh, ref OVERLAPPED overlapped);

        [StructLayout(LayoutKind.Sequential)]
        private struct OVERLAPPED
        {
            public uint internalLow;
            public uint internalHigh;
            public uint offsetLow;
            public uint offsetHigh;
            public IntPtr hEvent;
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

        private class LockFileExWrapper : IDisposable
        {
            private readonly FileStream mStream;
            private readonly ulong mOffset;
            private readonly ulong mCount;

            public LockFileExWrapper(FileStream stream, ulong offset, ulong count)
            {
                mStream = stream;
                mOffset = offset;
                mCount = count;

                var countLow = (uint)count;
                var countHigh = (uint)(count >> 32);

                var overlapped = new OVERLAPPED
                {
                    internalLow = 0,
                    internalHigh = 0,
                    offsetLow = (uint)offset,
                    offsetHigh = (uint)(offset >> 32),
                    hEvent = IntPtr.Zero,
                };

                if (!LockFileEx(stream.SafeFileHandle, LOCKFILE_EXCLUSIVE_LOCK, 0, countLow, countHigh, ref overlapped))
                {
                    throw new IOException();
                }
            }

            public void Dispose()
            {
                var countLow = (uint)mCount;
                var countHigh = (uint)(mCount >> 32);

                var overlapped = new OVERLAPPED
                {
                    internalLow = 0,
                    internalHigh = 0,
                    offsetLow = (uint)mOffset,
                    offsetHigh = (uint)(mOffset >> 32),
                    hEvent = IntPtr.Zero,
                };

                UnlockFileEx(mStream.SafeFileHandle, 0, countLow, countHigh, ref overlapped);
            }
        }
    }
}
