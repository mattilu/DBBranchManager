using DBBranchManager.Caching;

namespace DBBranchManager.Tasks
{
    internal interface ITask
    {
        string Name { get; }

        void GetRequirements(TaskExecutionContext context, IRequirementSink sink);

        void Simulate(TaskExecutionContext context, ref StateHash hash);
        void Execute(TaskExecutionContext context, ref StateHash hash);
    }
}
