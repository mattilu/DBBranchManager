using System.Collections.Generic;
using System.IO;
using DBBranchManager.Config;
using DBBranchManager.Constants;
using DBBranchManager.Dependencies;

namespace DBBranchManager.Components
{
    internal class ReleaseComponent : AggregatorComponent
    {
        private readonly string mReleaseDir;
        private readonly string mDeployPath;
        private readonly DatabaseConnectionInfo mDbConnection;

        public ReleaseComponent(string releaseDir, string deployPath, DatabaseConnectionInfo dbConnection)
        {
            mReleaseDir = releaseDir;
            mDeployPath = deployPath;
            mDbConnection = dbConnection;

            var name = Path.GetFileName(releaseDir);
            LogPre = string.Format("Release {0}: Begin", name);
            LogPost = string.Format("Release {0}: End", name);
        }

        protected override IEnumerable<IComponent> GetComponentsToRun(string action, ComponentRunContext runContext)
        {
            var graph = new DependencyGraph<IComponent>();

            var name = Path.GetFileName(mReleaseDir);

            var prerequisites = new PrerequisitesComponent(name);
            var tplComponent = new TemplatesComponent(Path.Combine(mReleaseDir, "Templates"), mDeployPath);
            var reportsComponent = new ReportsComponent(Path.Combine(mReleaseDir, "Reports"), mDeployPath);
            var scriptsComponent = new ScriptsComponent(Path.Combine(mReleaseDir, "Scripts"), mDeployPath, name, mDbConnection);

            graph.AddDependency(prerequisites, tplComponent);
            graph.AddDependency(prerequisites, reportsComponent);
            graph.AddDependency(prerequisites, scriptsComponent);
            graph.AddDependency(tplComponent, scriptsComponent);
            graph.AddDependency(reportsComponent, scriptsComponent);

            return graph.GetPath();
        }

        private class PrerequisitesComponent : ComponentBase
        {
            private string mName;

            public PrerequisitesComponent(string name)
            {
                mName = name;
            }

            [RunAction(ActionConstants.MakeReleasePackage)]
            private IEnumerable<string> MakeReleasePackageRun(string action, ComponentRunContext runContext)
            {
                if (!Directory.Exists(runContext.Config.ReleasePackagesPath))
                {
                    yield return string.Format("Creating directory {0}", runContext.Config.ReleasePackagesPath);
                    if (!runContext.DryRun)
                        Directory.CreateDirectory(runContext.Config.ReleasePackagesPath);
                }

                var releaseDir = Path.Combine(runContext.Config.ReleasePackagesPath, mName);
                if (!Directory.Exists(releaseDir))
                {
                    yield return string.Format("Creating directory {0}", releaseDir);
                    if (!runContext.DryRun)
                        Directory.CreateDirectory(releaseDir);
                }
            }
        }
    }
}