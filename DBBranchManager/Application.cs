using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBBranchManager.Caching;
using DBBranchManager.Entities;
using DBBranchManager.Entities.Config;
using DBBranchManager.Exceptions;
using DBBranchManager.Logging;
using DBBranchManager.Tasks;
using DBBranchManager.Utils;

namespace DBBranchManager
{
    internal class Application
    {
        private readonly CommandLineArguments mCommandLine;
        private readonly UserConfig mUserConfig;
        private readonly ProjectConfig mProjectConfig;


        public Application(string[] args)
        {
            mCommandLine = CommandLineArguments.Parse(args);
            mUserConfig = UserConfig.LoadFromJson(mCommandLine.ConfigFile ?? "config.json");
            mProjectConfig = ProjectConfig.LoadFromJson(Path.Combine(mUserConfig.ProjectRoot, mUserConfig.ProjectSettingsFile));
        }

        public int Run()
        {
            switch (mCommandLine.Command)
            {
                case "help":
                    return RunHelp();

                case "deploy":
                    return RunDeploy();

                default:
                    throw new SoftFailureException(string.Format("Unknown command: {0}", mCommandLine.Command));
            }
        }

        private int RunHelp()
        {
            return 0;
        }

        private int RunDeploy()
        {
            var context = CreateRunContext();
            var plan = BuildActionPlan(context);

            var root = new ExecutionNode("Begin deploy", "Deploy completed");
            root.AddChild(BuildRestoreDatabasesNode(plan.Databases, context));

            foreach (var release in plan.Releases)
            {
                var releaseNode = BuildReleaseNode(release, context);
                root.AddChild(releaseNode);
            }

            var hash = StateHash.Empty;
            try
            {
                root.Run(context, hash);
            }
            catch (SoftFailureException ex)
            {
                context.Log.LogFormat("Blocking error detected: {0}", ex.Message);
                return 1;
            }

            return 0;
        }

        private ExecutionNode BuildRestoreDatabasesNode(DatabaseBackupInfo[] databases, RunContext context)
        {
            var node = new ExecutionNode("Restoring databases...", "All databases restored!");
            node.AddChild(new ExecutionNode(new RestoreDatabasesTransform(mUserConfig.Databases.Connection, databases)));
            return node;
        }

        private ExecutionNode BuildReleaseNode(ReleaseConfig release, RunContext context)
        {
            var node = new ExecutionNode(string.Format("Begin release {0}", release.Name), string.Format("End release {0}", release.Name));
            foreach (var feature in release.Features)
            {
                node.AddChild(BuildFeatureNode(feature, context));
            }

            return node;
        }

        private ExecutionNode BuildFeatureNode(string featureName, RunContext context)
        {
            FeatureConfig feature;
            if (!context.Features.TryGet(featureName, out feature))
            {
                throw new SoftFailureException(string.Format("Cannot find feature {0}", featureName));
            }

            var node = new ExecutionNode(string.Format("Begin feature {0}", featureName), string.Format("End feature {0}", featureName));
            foreach (var taskConfig in feature.Recipe)
            {
                var task = context.TaskManager.CreateTask(taskConfig);
                var replacer = new VariableReplacer(context, feature, taskConfig);
                node.AddChild(new ExecutionNode(task, new TaskExecutionContext(context, feature, taskConfig, replacer)));
            }

            return node;
        }

        private ActionPlan BuildActionPlan(RunContext context)
        {
            var dbFiles = Directory.EnumerateFiles(mUserConfig.Databases.Backups.Root)
                .Select(x => new
                {
                    FullPath = x,
                    Match = mUserConfig.Databases.Backups.Pattern.Match(Path.GetFileName(x))
                })
                .Where(x => x.Match.Success)
                .Select(x => new
                {
                    x.FullPath,
                    DbName = x.Match.Groups["dbName"].Value,
                    Release = x.Match.Groups["release"].Value,
                    Environment = x.Match.Groups["env"] != null ? x.Match.Groups["env"].Value : null
                })
                .GroupBy(x => x.Release)
                .ToDictionary(x => x.Key, x => x.GroupBy(y => y.Environment)
                    .ToDictionary(y => y.Key, y =>
                        y.ToDictionary(z => z.DbName, z => z.FullPath)));

            var releaseStack = new Stack<ReleaseConfig>();
            var head = context.ActiveRelease;
            while (true)
            {
                var dbsForRelease = TryGetValue(dbFiles, head.Name);
                var userEnv = mUserConfig.EnvironmentVariables.GetOrDefault("environment");

                var dbs = GetDatabaseBackups(dbsForRelease, userEnv);
                if (dbs != null)
                {
                    return new ActionPlan(dbs, releaseStack.ToArray());
                }

                releaseStack.Push(head);
                if (head.Baseline == null)
                {
                    throw new SoftFailureException(string.Format("Cannot find a valid base to start. Last release found: {0}", head.Name));
                }
                if (!context.Releases.Releases.TryGet(head.Baseline, out head))
                {
                    throw new SoftFailureException(string.Format("Cannot find release {0} (baseline of {1})", head.Baseline, head.Name));
                }
            }
        }

        private DatabaseBackupInfo[] GetDatabaseBackups(Dictionary<string, Dictionary<string, string>> dbsByEnv, string userEnv)
        {
            if (dbsByEnv == null)
                return null;

            if (userEnv != null)
            {
                var dbsForEnv = TryGetValue(dbsByEnv, userEnv);
                if (dbsForEnv != null)
                {
                    var dbs = GetDatabaseBackups(dbsForEnv);
                    if (dbs != null)
                        return dbs;
                }
            }

            foreach (var kvp in dbsByEnv)
            {
                var dbs = GetDatabaseBackups(kvp.Value);
                if (dbs != null)
                    return dbs;
            }

            return null;
        }

        private DatabaseBackupInfo[] GetDatabaseBackups(Dictionary<string, string> dbs)
        {
            var result = mProjectConfig.Databases.Select(x => new DatabaseBackupInfo(x, TryGetValue(dbs, x))).ToArray();
            return result.Any(x => x.BackupFilePath == null) ? null : result;
        }

        private static TValue TryGetValue<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey key)
        {
            TValue value;
            return dict.TryGetValue(key, out value) ? value : default(TValue);
        }

        private RunContext CreateRunContext()
        {
            var releasesFile = Path.Combine(mUserConfig.ProjectRoot, mProjectConfig.Releases);
            var releases = ReleasesConfig.LoadFromJson(releasesFile);

            var featuresFiles = FileUtils.ExpandGlob(Path.Combine(mUserConfig.ProjectRoot, mProjectConfig.Features));
            var features = FeatureConfigCollection.LoadFromMultipleJsons(featuresFiles);

            var tasksFiles = FileUtils.ExpandGlob(Path.Combine(mUserConfig.ProjectRoot, mProjectConfig.Tasks));
            var tasks = TaskDefinitionConfigCollection.LoadFromMultipleJsons(tasksFiles);

            return new RunContext(mCommandLine, mUserConfig, mProjectConfig, releases, features, tasks, new TaskManager(tasks), new ConsoleLog());
        }

        private class ExecutionNode
        {
            private readonly string mLogPre;
            private readonly string mLogPost;
            private readonly IStateTransform mTransform;
            private readonly List<ExecutionNode> mChildren = new List<ExecutionNode>();

            public ExecutionNode(string logPre, string logPost)
            {
                mLogPre = logPre;
                mLogPost = logPost;
            }

            public ExecutionNode(ITask task, TaskExecutionContext context) :
                this(new TaskExecutionTransform(task, context))
            {
            }

            public ExecutionNode(IStateTransform transform)
            {
                mTransform = transform;
            }

            public void AddChild(ExecutionNode node)
            {
                if (mTransform != null)
                    throw new InvalidOperationException("Cannot add child nodes to an action-initialized execution node");

                mChildren.Add(node);
            }

            public StateHash Run(RunContext context, StateHash hash)
            {
                if (mLogPre != null)
                    context.Log.Log(mLogPre);

                if (mTransform != null)
                {
                    hash = mTransform.RunTransform(hash, context.DryRun, context.Log);
                }
                else if (mChildren.Count > 0)
                {
                    using (context.Log.IndentScope())
                    {
                        foreach (var child in mChildren)
                        {
                            hash = child.Run(context, hash);
                        }
                    }
                }

                if (mLogPost != null)
                    context.Log.Log(mLogPost);

                return hash;
            }
        }

        private class ActionPlan
        {
            private readonly DatabaseBackupInfo[] mDatabases;
            private readonly ReleaseConfig[] mReleases;

            public ActionPlan(DatabaseBackupInfo[] databases, ReleaseConfig[] releases)
            {
                mDatabases = databases;
                mReleases = releases;
            }

            public DatabaseBackupInfo[] Databases
            {
                get { return mDatabases; }
            }

            public ReleaseConfig[] Releases
            {
                get { return mReleases; }
            }
        }
    }
}