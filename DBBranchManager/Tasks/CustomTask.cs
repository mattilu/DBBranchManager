using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBBranchManager.Caching;
using DBBranchManager.Entities.Config;
using DBBranchManager.Exceptions;
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

        public void GetRequirements(TaskExecutionContext context, IRequirementSink sink)
        {
            var feature = new Lazy<string>(() => string.Format("Feature '{0}'", context.Feature.Name));

            foreach (var kvp in mTaskDefinition.Requirements)
            {
                var v = CreateVerifier(kvp.Key);
                foreach (var val in kvp.Value)
                {
                    var arg = val;
                    sink.Add(feature.Value, () => v.Verify(context, arg));
                }
            }
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

        private static IRequirementVerifier CreateVerifier(string type)
        {
            switch (type)
            {
                case "paths":
                    return new PathsVerifier();

                default:
                    throw new SoftFailureException(string.Format("Unknown requirement type '{0}'", type));
            }
        }

        private interface IRequirementVerifier
        {
            Tuple<bool, string> Verify(TaskExecutionContext context, string arg);
        }

        private class PathsVerifier : IRequirementVerifier
        {
            public Tuple<bool, string> Verify(TaskExecutionContext context, string arg)
            {
                var dir = new DirectoryInfo(context.Replacer.ReplaceVariables(arg));
                return dir.Exists ?
                    Tuple.Create(true, string.Format("Directory '{0}' exists", dir.FullName)) :
                    Tuple.Create(false, string.Format("Directory '{0}' does not exist", dir.FullName));
            }
        }
    }
}
