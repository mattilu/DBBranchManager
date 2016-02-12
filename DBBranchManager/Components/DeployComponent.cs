using System.Collections.Generic;
using System.Linq;
using DBBranchManager.Config;
using DBBranchManager.Dependencies;

namespace DBBranchManager.Components
{
    internal class DeployComponent : AggregatorComponent
    {
        private readonly IDependencyGraph<BranchInfo> mBranchGraph;
        private readonly BranchInfo mActiveBranch;
        private readonly BranchInfo mBackupBranch;
        private readonly DatabaseInfo[] mDatabasesInfos;

        public DeployComponent(IDependencyGraph<BranchInfo> branchGraph, string backupBranch, string activeBranch, IEnumerable<DatabaseInfo> databasesInfos)
        {
            var branchesByName = branchGraph.GetPath().ToDictionary(x => x.Name);
            mBranchGraph = branchGraph;
            mBackupBranch = branchesByName[backupBranch];
            mActiveBranch = branchesByName[activeBranch];
            mDatabasesInfos = databasesInfos.ToArray();
        }

        protected override IEnumerable<IComponent> GetComponentsToRun(string action, ComponentRunContext runContext)
        {
            yield return new LogComponent(string.Format("Beginning deploy: {0}", runContext.Environment));

            foreach (var databaseInfo in mDatabasesInfos)
            {
                yield return new RestoreDatabaseComponent(databaseInfo);
            }

            foreach (var branchInfo in mBranchGraph.GetPath(mBackupBranch, mActiveBranch))
            {
                yield return new BranchComponent(branchInfo.Name, branchInfo.BasePath, branchInfo.DeployPath, mDatabasesInfos[0].Connection);
            }

            yield return new LogComponent("Done!");
        }
    }
}