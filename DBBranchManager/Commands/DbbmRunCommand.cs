using System;
using System.Collections.Generic;
using System.Linq;
using DBBranchManager.Caching;
using DBBranchManager.Constants;
using DBBranchManager.Entities;
using DBBranchManager.Entities.Config;
using DBBranchManager.Exceptions;
using DBBranchManager.Tasks;
using DBBranchManager.Utils;
using Mono.Options;

namespace DBBranchManager.Commands
{
    internal class DbbmRunCommand : DbbmCommand
    {
        public override string Description
        {
            get { return "Run a custom action"; }
        }

        public override void Run(ApplicationContext appContext, IEnumerable<string> args)
        {
            string release = null;
            string env = null;
            var dryRun = false;

            var p = new OptionSet
            {
                { "r=|release=", "Select which release to use, instead of the default.", v => release = v },
                { "e=|env=|environment=", "Select which environment to use, instead of the default.", v => env = v },
                { "n|dry-run", "Don't actually run actions, just print what would be done and exit.", v => dryRun = v != null }
            };

            var extra = Parse(p, args,
                CommandConstants.Run,
                "<ACTION> [OPTIONS]+");
            if (extra == null)
                return;

            if (extra.Count != 1)
                return;

            var runContext = RunContext.Create(appContext, extra[0], release, env, dryRun);
            RunCore(runContext);
        }

        private static void RunCore(RunContext context)
        {
            var plan = ActionPlan.Build(context);

            var root = new ExecutionNode(string.Format("Running '{0}' action", context.Action), "Success!");
            foreach (var release in plan.Releases)
            {
                root.AddChild(BuildReleaseNode(context, release));
            }

            root.Run(context);
        }

        private static ExecutionNode BuildReleaseNode(RunContext context, ReleaseConfig release)
        {
            var node = new ExecutionNode(string.Format("Begin release {0}", release.Name), string.Format("End release {0}", release.Name));
            foreach (var feature in release.Features)
            {
                node.AddChild(BuildFeatureNode(context, feature));
            }
            return node;
        }

        private static ExecutionNode BuildFeatureNode(RunContext context, string feature)
        {
            FeatureConfig featureConfig;
            if (!context.FeaturesConfig.TryGet(feature, out featureConfig))
                throw new SoftFailureException(string.Format("Cannot find feature '{0}'", feature));

            var node = new ExecutionNode(string.Format("Begin feature {0}", feature), string.Format("End feature {0}", feature));

            foreach (var taskConfig in featureConfig.Recipe)
            {
                var task = context.TaskManager.CreateTask(taskConfig);
                var replacer = new VariableReplacer(context.ApplicationContext, featureConfig, taskConfig);
                node.AddChild(new ExecutionNode(task, new TaskExecutionContext(context, featureConfig, taskConfig, replacer)));
            }

            return node;
        }

        private class ExecutionNode
        {
            private readonly string mLogPre;
            private readonly string mLogPost;
            private readonly ITask mTask;
            private readonly TaskExecutionContext mTaskContext;
            private readonly List<ExecutionNode> mChildren = new List<ExecutionNode>();

            public ExecutionNode(string logPre, string logPost)
            {
                mLogPre = logPre;
                mLogPost = logPost;
            }

            public ExecutionNode(ITask task, TaskExecutionContext taskContext)
            {
                mTask = task;
                mTaskContext = taskContext;
            }

            public void AddChild(ExecutionNode child)
            {
                if (mTask != null)
                    throw new InvalidOperationException("Cannot add child nodes to an action-initialized execution node");

                mChildren.Add(child);
            }

            public void Run(RunContext context)
            {
                if (mLogPre != null)
                    context.ApplicationContext.Log.Log(mLogPre);

                if (mTask != null)
                {
                    StateHash hash = null;
                    mTask.Execute(mTaskContext, ref hash);
                }
                else if (mChildren.Count > 0)
                {
                    using (context.ApplicationContext.Log.IndentScope())
                    {
                        foreach (var child in mChildren)
                        {
                            child.Run(context);
                        }
                    }
                }

                if (mLogPost != null)
                    context.ApplicationContext.Log.Log(mLogPost);
            }
        }
    }
}
