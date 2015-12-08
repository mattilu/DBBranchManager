using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DBBranchManager.Components
{
    internal class TemplatesComponent : IComponent
    {
        private static readonly Regex TemplateFileRegex = new Regex(@"^TPL_\d+.+\.xls[mx]?$");
        private readonly string mTemplatesPath;
        private readonly string mDeployPath;

        public TemplatesComponent(string templatesPath, string deployPath)
        {
            mTemplatesPath = templatesPath;
            mDeployPath = deployPath;
        }

        public IEnumerable<string> Run(ComponentRunState runState)
        {
            if (Directory.Exists(mTemplatesPath))
            {
                yield return string.Format("Templates {0} -> {1}", mTemplatesPath, mDeployPath);

                var synchronizer = new FileSynchronizer(mTemplatesPath, mDeployPath, TemplateFileRegex);
                foreach (var log in synchronizer.Run(runState))
                {
                    yield return log;
                }
            }
        }
    }
}