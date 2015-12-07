using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace DBBranchManager
{
    [Serializable]
    public class State
    {
        public State()
        {
            BranchStates = new Dictionary<string, BranchState>();
        }

        public State(IEnumerable<string> branches) :
            this()
        {
            foreach (var branch in branches)
            {
                BranchStates.Add(branch, new BranchState());
            }
        }

        public Dictionary<string, BranchState> BranchStates { get; private set; }

        public bool FileSystemDifferences(string path, string branchPath, string glob, string pattern)
        {
            var branch = BranchStates.Single(x => x.Key.Equals(branchPath, StringComparison.InvariantCultureIgnoreCase)).Value;
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var changed = false;

            // Files in FS
            foreach (var file in Directory.EnumerateFiles(path, glob).Where(x => regex.IsMatch(Path.GetFileName(x))))
            {
                changed |= branch.CheckDifferences(file);
            }

            // Files in cache but not in FS
            var toRemove = new List<string>();
            foreach (var fileState in branch.FileStates)
            {
                if (!File.Exists(fileState.Key))
                {
                    toRemove.Add(fileState.Key);
                    changed = true;
                }
            }

            foreach (var s in toRemove)
            {
                branch.FileStates.Remove(s);
            }

            return changed;
        }
    }

    [Serializable]
    public class BranchState
    {
        public Dictionary<string, FileState> FileStates { get; set; }

        public BranchState()
        {
            FileStates = new Dictionary<string, FileState>();
        }

        public bool CheckDifferences(string path)
        {
            var lowerPath = path.ToLower();

            FileState state;
            if (!FileStates.TryGetValue(lowerPath, out state))
            {
                FileStates.Add(lowerPath, new FileState(path));
                return true;
            }

            return state.UpdateIfChanged(path);
        }
    }

    [Serializable]
    public class FileState
    {
        public FileState(string path)
        {
            LastEditTime = GetLastEditTime(path);
            Hash = GetHash(path);
        }

        public DateTime LastEditTime { get; private set; }
        public byte[] Hash { get; private set; }

        public bool UpdateIfChanged(string path)
        {
            var lastEditTime = GetLastEditTime(path);
            if (lastEditTime != LastEditTime)
            {
                LastEditTime = lastEditTime;
                var hash = GetHash(path);
                if (!Hash.SequenceEqual(hash))
                {
                    Hash = hash;
                    return true;
                }
            }

            return false;
        }

        private static DateTime GetLastEditTime(string path)
        {
            return File.GetLastWriteTimeUtc(path);
        }

        private static byte[] GetHash(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return sha.ComputeHash(stream);
            }
        }
    }
}