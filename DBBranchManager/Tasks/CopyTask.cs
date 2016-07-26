using System.IO;
using System.Text.RegularExpressions;
using DBBranchManager.Utils;

namespace DBBranchManager.Tasks
{
    internal class CopyTask : ITask
    {
        public const string TaskName = "copy";

        public string Name
        {
            get { return TaskName; }
        }

        public void Execute(TaskExecutionContext context)
        {
            var from = FileUtils.ToLocalPath(context.GetParameter("from"));
            if (!Path.IsPathRooted(from))
                from = FileUtils.ToLocalPath(context.Feature.BaseDirectory, from);

            if (!Directory.Exists(from))
                return;

            var to = FileUtils.ToLocalPath(context.GetParameter("to"));
            if (!Directory.Exists(to))
            {
                context.Log.LogFormat("Creating directory {0}", to);

                if (!context.DryRun)
                    Directory.CreateDirectory(to);
            }

            var regex = new Regex(context.GetParameter("regex"));

            foreach (var f in FileUtils.EnumerateFiles2(from, regex.IsMatch))
            {
                var fileName = f.FileName;
                var destFile = Path.Combine(to, fileName);

                var fileInfo = new FileInfo(f.FullPath);
                var destFileInfo = new FileInfo(destFile);

                if (destFileInfo.Exists && destFileInfo.LastWriteTimeUtc == fileInfo.LastWriteTimeUtc)
                {
                    context.Log.LogFormat("Skipping {0}", fileName);
                }
                else
                {
                    if (destFileInfo.Exists && (destFileInfo.Attributes & FileAttributes.ReadOnly) != 0)
                    {
                        context.Log.LogFormat("Removing read-only attribute from {0}", destFile);

                        if (!context.DryRun)
                            destFileInfo.Attributes &= ~FileAttributes.ReadOnly;
                    }

                    context.Log.LogFormat("Copying {0} -> {1}", fileName, to);

                    if (!context.DryRun)
                        fileInfo.CopyTo(destFile, true);
                }
            }
        }
    }
}
