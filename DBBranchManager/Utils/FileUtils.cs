using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DBBranchManager.Utils
{
    static class FileUtils
    {
        public static IEnumerable<string> EnumerateFiles(string path, Func<string, bool> filter)
        {
            return Directory.EnumerateFiles(path)
                .Select(Path.GetFileName)
                .Where(filter)
                .OrderBy(x => x, new NaturalSortComparer());
        }

        public static IEnumerable<Tuple<string, string>> EnumerateFiles2(string path, Func<string, bool> filter)
        {
            return Directory.EnumerateFiles(path)
                .Select(x => Tuple.Create(x, Path.GetFileName(x)))
                .Where(x => filter(x.Item2))
                .OrderBy(x => x.Item2, new NaturalSortComparer());
        }
    }
}
