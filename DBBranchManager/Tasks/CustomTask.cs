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

        public void Execute(TaskExecutionContext context)
        {
            RecipeConfig recipe;
            if (!mTaskDefinition.Commands.TryGetRecipe(context.CommandLine.Command, out recipe))
                return;

            context.Log.LogFormat("Running task '{0}'", Name);

            using (context.IndentScope())
            {
                foreach (var taskConfig in recipe)
                {
                    var task = mManager.CreateTask(taskConfig);
                    context.Log.LogFormat("Running sub-task '{0}'", task.Name);

                    using (context.IndentScope())
                    {
                        var ctx = new TaskExecutionContext(context.Context, context.Feature, taskConfig, context.Replacer.WithSubTask(mTaskDefinition, taskConfig));
                        task.Execute(ctx);
                    }
                }
            }
        }
    }
}
