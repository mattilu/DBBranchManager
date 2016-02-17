using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DBBranchManager.Config;

namespace DBBranchManager.Components
{
    internal class BranchComponent : AggregatorComponent
    {
        private static readonly Regex ToDeployRegex = new Regex(@"^(?:to[ _]deploy).*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly string mName;
        private readonly string mBasePath;
        private readonly string mDeployPath;
        private readonly DatabaseConnectionInfo mDbConnection;

        public BranchComponent(string name, string basePath, string deployPath, DatabaseConnectionInfo dbConnection) :
            base(string.Format("Branch {0}", name))
        {
            mName = name;
            mBasePath = basePath;
            mDeployPath = deployPath;
            mDbConnection = dbConnection;
        }

        protected override IEnumerable<IComponent> GetComponentsToRun(string action, ComponentRunContext runContext)
        {
            if (Directory.Exists(mBasePath))
            {
                var toDeployDirs = Directory.EnumerateDirectories(mBasePath)
                    .Where(x => ToDeployRegex.IsMatch(Path.GetFileName(x)));

                foreach (var toDeployDir in toDeployDirs)
                {
                    yield return new ReleaseComponent(toDeployDir, mDeployPath, mDbConnection);
                }
            }
        }
    }
}