using DBBranchManager.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace DBBranchManager.Components
{
    internal class FileSynchronizer : IComponent
    {
        private readonly string mSourcePath;
        private readonly string mDestinationPath;
        private readonly Regex mFilter;

        public FileSynchronizer(string sourcePath, string destinationPath, Regex filter)
        {
            mSourcePath = sourcePath;
            mDestinationPath = destinationPath;
            mFilter = filter;
        }

        public IEnumerable<string> Run(ComponentState componentState)
        {
            if (Directory.Exists(mSourcePath))
            {
                if (!Directory.Exists(mDestinationPath))
                {
                    yield return string.Format("Creating {0}", mDestinationPath);
                    Directory.CreateDirectory(mDestinationPath);
                }

                foreach (var f in FileUtils.EnumerateFiles2(mSourcePath, mFilter.IsMatch))
                {
                    var file = f.Item1;
                    var fileName = f.Item2;
                    Debug.Assert(fileName != null, "fileName != null");

                    var destFile = Path.Combine(mDestinationPath, fileName);

                    var fileInfo = new FileInfo(file);
                    var destFileInfo = new FileInfo(destFile);

                    if (destFileInfo.Exists && destFileInfo.LastWriteTimeUtc == fileInfo.LastWriteTimeUtc)
                    {
                        yield return string.Format("Skipping {0}", fileName);
                        continue;
                    }

                    fileInfo.CopyTo(destFile, true);

                    yield return string.Format("Copying {0} -> {1}", fileName, mDestinationPath);
                }
            }
        }
    }
}