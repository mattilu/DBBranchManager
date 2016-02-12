using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DBBranchManager.Constants;

namespace DBBranchManager.Components
{
    internal class ReportsComponent : ComponentBase
    {
        private static readonly Regex ReportFileRegex = new Regex(@"^[DTX]_\d+.+\.x(?:lsm|ml)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly string mReportsPath;
        private readonly string mDeployPath;

        public ReportsComponent(string reportsPath, string deployPath)
        {
            mReportsPath = reportsPath;
            mDeployPath = deployPath;
        }

        [RunAction(ActionConstants.Deploy)]
        private IEnumerable<string> DeployRun(string action, ComponentRunContext runContext)
        {
            if (Directory.Exists(mReportsPath))
            {
                yield return string.Format("Reports: {0} -> {1}", mReportsPath, mDeployPath);

                runContext.IncreaseDepth();

                var synchronizer = new FileSynchronizer(mReportsPath, mDeployPath, ReportFileRegex);
                foreach (var log in synchronizer.Run(action, runContext))
                {
                    yield return log;
                }

                runContext.DecreaseDepth();
            }
        }
    }
}