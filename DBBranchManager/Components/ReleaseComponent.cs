using System.Collections.Generic;
using System.IO;
using DBBranchManager.Config;
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
        }

        protected override IEnumerable<IComponent> GetComponentsToRun(string action, ComponentRunContext runContext)
        {
            var graph = new DependencyGraph<IComponent>();

            var name = Path.GetFileName(mReleaseDir);
            var logBeginComponent = new LogComponent(string.Format("Release {0}: Begin", name));
            var logEndComponent = new LogComponent(string.Format("Release {0}: End", name));

            var tplComponent = new TemplatesComponent(Path.Combine(mReleaseDir, "Templates"), mDeployPath);
            var reportsComponent = new ReportsComponent(Path.Combine(mReleaseDir, "Reports"), mDeployPath);
            var scriptsComponent = new ScriptsComponent(Path.Combine(mReleaseDir, "Scripts"), mDeployPath, name, mDbConnection);

            graph.AddDependency(logBeginComponent, tplComponent);
            graph.AddDependency(logBeginComponent, reportsComponent);
            graph.AddDependency(tplComponent, scriptsComponent);
            graph.AddDependency(reportsComponent, scriptsComponent);
            graph.AddDependency(scriptsComponent, logEndComponent);

            return graph.GetPath();
        }
    }
}