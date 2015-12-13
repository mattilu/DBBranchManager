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

        public IEnumerable<string> Run(ComponentRunState runState)
        {
            if (Directory.Exists(mSourcePath))
            {
                if (!Directory.Exists(mDestinationPath))
                {
                    yield return string.Format("Creating {0}", mDestinationPath);
                    if (!runState.DryRun)
                        Directory.CreateDirectory(mDestinationPath);
                }

                foreach (var f in FileUtils.EnumerateFiles2(mSourcePath, mFilter.IsMatch))
                {
                    var fileName = f.FileName;
                    Debug.Assert(fileName != null, "fileName != null");

                    var destFile = Path.Combine(mDestinationPath, fileName);

                    var fileInfo = new FileInfo(f.FullPath);
                    var destFileInfo = new FileInfo(destFile);

                    if (destFileInfo.Exists && destFileInfo.LastWriteTimeUtc == fileInfo.LastWriteTimeUtc)
                    {
                        yield return string.Format("Skipping {0}", fileName);
                    }
                    else
                    {
                        yield return string.Format("Copying {0} -> {1}", fileName, mDestinationPath);

                        if (!runState.DryRun)
                            fileInfo.CopyTo(destFile, true);
                    }
                }
            }
        }
    }
}