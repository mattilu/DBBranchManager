using DBBranchManager.Caching;
using DBBranchManager.Entities.Config;
using DBBranchManager.Utils;

namespace DBBranchManager.Tasks
{
    [DontRegister]
    internal class CustomTask : ITask
    {
        private readonly TaskDefinitionConfig mTaskDefinition;
        private readonly TaskManager mManager;

        public CustomTask(TaskDefinitionConfig taskDefinition, TaskManager manager)
        {
            mTaskDefinition = taskDefinition;
            mManager = manager;
        }

        public string Name
        {
            get { return mTaskDefinition.Name; }
        }

        public void Simulate(TaskExecutionContext context, ref StateHash hash)
        {
            hash = ExecuteCore(context, hash, false);
        }

        public void Execute(TaskExecutionContext context, ref StateHash hash)
        {
            hash = ExecuteCore(context, hash, true);
        }

        private StateHash ExecuteCore(TaskExecutionContext context, StateHash hash, bool execute)
        {
            RecipeConfig recipe;
            if (!mTaskDefinition.Commands.TryGetRecipe(context.Context.Action, out recipe))
                return hash;

            if (execute)
                context.Log.LogFormat("Running task '{0}'", Name);

            using (context.IndentScope())
            {
                foreach (var taskConfig in recipe)
                {
                    var task = mManager.CreateTask(taskConfig);

                    if (execute)
                        context.Log.LogFormat("Running sub-task '{0}'", task.Name);

                    using (context.IndentScope())
                    {
                        var ctx = new TaskExecutionContext(context.Context, context.Feature, taskConfig, context.Replacer.WithSubTask(mTaskDefinition, taskConfig));
                        if (execute)
                            task.Execute(ctx, ref hash);
                        else
                            task.Simulate(ctx, ref hash);
                    }
                }
            }

            return hash;
        }
    }
}
