using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DBBranchManager.Constants;
using DBBranchManager.Utils;

namespace DBBranchManager.Components
{
    internal class TemplatesComponent : ComponentBase
    {
        private static readonly Regex TemplateFileRegex = new Regex(@"^TPL_\d+_.+\.xls[mx]?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly string mTemplatesPath;
        private readonly string mDeployPath;

        public TemplatesComponent(string templatesPath, string deployPath)
        {
            mTemplatesPath = templatesPath;
            mDeployPath = deployPath;
        }

        [RunAction(ActionConstants.Deploy)]
        public IEnumerable<string> DeployRun(string action, ComponentRunContext runContext)
        {
            if (Directory.Exists(mTemplatesPath))
            {
                yield return string.Format("Templates {0} -> {1}", mTemplatesPath, mDeployPath);


                using (runContext.DepthScope())
                {
                    var synchronizer = new FileSynchronizer(mTemplatesPath, mDeployPath, TemplateFileRegex);
                    foreach (var log in synchronizer.Run(action, runContext))
                    {
                        yield return log;
                    }
                }
            }
        }
    }
}