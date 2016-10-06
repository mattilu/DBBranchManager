using DBBranchManager.Logging;
using DBBranchManager.Tasks;

namespace DBBranchManager.Caching
{
    internal class TaskExecutionTransform : IStateTransform
    {
        private readonly ITask mTask;
        private readonly TaskExecutionContext mContext;

        public TaskExecutionTransform(ITask task, TaskExecutionContext context)
        {
            mTask = task;
            mContext = context;
        }

        public void GetRequirements(IRequirementSink sink)
        {
            mTask.GetRequirements(mContext, sink);
        }

        public StateHash CalculateTransform(StateHash currentHash)
        {
            mTask.Simulate(mContext, ref currentHash);
            return currentHash;
        }

        public StateHash RunTransform(StateHash currentHash, bool dryRun, ILog log)
        {
            mTask.Execute(mContext, ref currentHash);
            return currentHash;
        }
    }
}
