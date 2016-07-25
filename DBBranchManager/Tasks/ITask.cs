namespace DBBranchManager.Tasks
{
    internal interface ITask
    {
        string Name { get; }

        void Execute(TaskExecutionContext context);
    }
}