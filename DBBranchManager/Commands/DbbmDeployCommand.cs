using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    internal class DbbmDeployCommand : DbbmCommand
    {
        public override string Description
        {
            get { return "Deploy your project to your environment."; }
        }

        public override void Run(ApplicationContext appContext, IEnumerable<string> args)
        {
            string release = null;
            string env = null;
            var dryRun = false;
            var resume = false;
            var noCache = appContext.UserConfig.Cache.Disabled;

            var p = new OptionSet
            {
                { "r=|release=", "Select which release to use, instead of the default.", v => release = v },
                { "e=|env=|environment=", "Select which environment to use, instead of the default", v => env = v },
                { "n|dry-run", "Don't actually run actions, just print what would be done and exit.", v => dryRun = v != null },
                { "s|resume", "Resume a failed deploy, if possible.", v => resume = v != null }
            };

            var extra = Parse(p, args,
                CommandConstants.Deploy,
                "[OPTIONS]+");
            if (extra == null)
                return;

            var runContext = RunContext.Create(appContext, CommandConstants.Deploy, release, env, dryRun);
            RunCore(DeployContext.Create(runContext, resume, noCache));
        }


        private void RunCore(DeployContext context)
        {
            Beep(context, "start");

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
                StateHash startingHash = null;
                if (context.Resume)
                {
                    startingHash = LoadResumeHash(context);
                }

                var improved = root.Calculate(context, hash, startingHash, context.CacheManager);
                if (improved.Item3)
                {
                    root = improved.Item1;
                    if (improved.Item4 != null)
                    {
                        context.CacheManager.UpdateHits(context.ApplicationContext.ProjectConfig.Databases.Select(x => Tuple.Create(x, improved.Item4)));
                    }
                }

                root.Run(context, startingHash ?? hash, context.CacheManager);

                CleanResumeHash(context);
            }
            catch (SoftFailureException ex)
            {
                Beep(context, "error");
                throw new SoftFailureException("Blocking error detected", ex);
            }

            Beep(context, "success");
        }

        private ActionPlan BuildActionPlan(DeployContext context)
        {
            var dbFiles = Directory.EnumerateFiles(context.ApplicationContext.UserConfig.Databases.Backups.Root)
                .Select(x => new
                {
                    FullPath = x,
                    Match = context.ApplicationContext.UserConfig.Databases.Backups.Pattern.Match(Path.GetFileName(x))
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
                var userEnv = context.ActiveEnvironment.Name;

                var dbs = GetDatabaseBackups(context, dbsForRelease, userEnv);
                if (dbs != null)
                    return new ActionPlan(dbs, releaseStack.ToArray());

                releaseStack.Push(head);

                if (head.Baseline == null)
                    throw new SoftFailureException(string.Format("Cannot find a valid base to start. Last release found: {0}", head.Name));

                if (!context.ReleasesConfig.Releases.TryGet(head.Baseline, out head))
                    throw new SoftFailureException(string.Format("Cannot find release {0} (baseline of {1})", head.Baseline, head.Name));
            }
        }

        private static void Beep(DeployContext context, string type)
        {
            BeepConfig beep;
            if (context.ApplicationContext.UserConfig.Beeps.TryGetValue(type, out beep))
                Buzzer.Beep(beep.Frequency, beep.Duration, beep.Pulses, beep.DutyCycle);
        }

        private static ExecutionNode BuildRestoreDatabasesNode(DatabaseBackupInfo[] databases, DeployContext context)
        {
            var node = new ExecutionNode("Restoring databases...", "All databases restored!");
            node.AddChild(new ExecutionNode(new RestoreDatabasesTransform(context.ApplicationContext.UserConfig.Databases.Connection, databases)));
            return node;
        }

        private static ExecutionNode BuildReleaseNode(ReleaseConfig release, DeployContext context)
        {
            var node = new ExecutionNode(string.Format("Begin release {0}", release.Name), string.Format("End release {0}", release.Name));
            foreach (var feature in release.Features)
            {
                node.AddChild(BuildFeatureNode(feature, context));
            }

            return node;
        }

        private static ExecutionNode BuildFeatureNode(string featureName, DeployContext context)
        {
            FeatureConfig feature;
            if (!context.FeaturesConfig.TryGet(featureName, out feature))
                throw new SoftFailureException(string.Format("Cannot find feature {0}", featureName));

            var node = new ExecutionNode(string.Format("Begin feature {0}", featureName), string.Format("End feature {0}", featureName));
            foreach (var taskConfig in feature.Recipe)
            {
                var task = context.TaskManager.CreateTask(taskConfig);
                var replacer = new VariableReplacer(context.ApplicationContext, feature, taskConfig);
                node.AddChild(new ExecutionNode(task, new TaskExecutionContext(context.RunContext, feature, taskConfig, replacer)));
            }

            return node;
        }

        private static DatabaseBackupInfo[] GetDatabaseBackups(DeployContext context, Dictionary<string, Dictionary<string, string>> dbsByEnv, string userEnv)
        {
            if (dbsByEnv == null)
                return null;

            if (userEnv != null)
            {
                var dbsForEnv = TryGetValue(dbsByEnv, userEnv);
                if (dbsForEnv != null)
                {
                    var dbs = GetDatabaseBackups(context, dbsForEnv);
                    if (dbs != null)
                        return dbs;
                }
            }

            foreach (var kvp in dbsByEnv)
            {
                var dbs = GetDatabaseBackups(context, kvp.Value);
                if (dbs != null)
                    return dbs;
            }

            return null;
        }

        private static DatabaseBackupInfo[] GetDatabaseBackups(DeployContext context, Dictionary<string, string> dbs)
        {
            var result = context.ApplicationContext.ProjectConfig.Databases.Select(x => new DatabaseBackupInfo(x, TryGetValue(dbs, x))).ToArray();
            return result.Any(x => x.BackupFilePath == null) ? null : result;
        }

        private static StateHash LoadResumeHash(DeployContext context)
        {
            var file = GetResumeHashFile(context);
            if (!file.Exists)
                throw new SoftFailureException("Cannot find resume hash");

            using (var fs = file.OpenRead())
            using (var r = new StreamReader(fs))
            {
                var line = r.ReadLine();
                try
                {
                    return StateHash.FromHexString(line);
                }
                catch (Exception ex)
                {
                    throw new SoftFailureException("Invalid resume hash format", ex);
                }
            }
        }

        private static void CleanResumeHash(DeployContext context)
        {
            var file = GetResumeHashFile(context);
            file.Delete();
        }

        private static FileInfo GetResumeHashFile(DeployContext context)
        {
            return new FileInfo(Path.Combine(context.ApplicationContext.ProjectRoot, ".dbbm.resume"));
        }

        private static TValue TryGetValue<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey key)
        {
            TValue value;
            return dict.TryGetValue(key, out value) ? value : default(TValue);
        }

        private class DeployContext
        {
            private readonly RunContext mRunContext;
            private readonly ICacheManager mCacheManager;
            private readonly bool mResume;
            private readonly bool mNoCache;

            private DeployContext(RunContext runContext, ICacheManager cacheManager, bool resume, bool noCache)
            {
                mRunContext = runContext;
                mCacheManager = cacheManager;
                mResume = resume;
                mNoCache = noCache;
            }

            public RunContext RunContext
            {
                get { return mRunContext; }
            }

            public ApplicationContext ApplicationContext
            {
                get { return mRunContext.ApplicationContext; }
            }

            public ReleasesConfig ReleasesConfig
            {
                get { return mRunContext.ReleasesConfig; }
            }

            public FeatureConfigCollection FeaturesConfig
            {
                get { return mRunContext.FeaturesConfig; }
            }

            public TaskDefinitionConfigCollection TaskDefinitionsConfig
            {
                get { return mRunContext.TaskDefinitionsConfig; }
            }

            public TaskManager TaskManager
            {
                get { return mRunContext.TaskManager; }
            }

            public ReleaseConfig ActiveRelease
            {
                get { return mRunContext.ActiveRelease; }
            }

            public EnvironmentConfig ActiveEnvironment
            {
                get { return mRunContext.ActiveEnvironment; }
            }

            public ICacheManager CacheManager
            {
                get { return mCacheManager; }
            }

            public bool DryRun
            {
                get { return mRunContext.DryRun; }
            }

            public bool Resume
            {
                get { return mResume; }
            }

            public bool NoCache
            {
                get { return mNoCache; }
            }

            public static DeployContext Create(RunContext runContext, bool resume, bool noCache)
            {
                return new DeployContext(runContext, CreateCacheManager(runContext, noCache), resume, noCache);
            }

            private static ICacheManager CreateCacheManager(RunContext context, bool noCache)
            {
                if (noCache)
                    return new NullCacheManager();

                return new CacheManager(context.ApplicationContext.UserConfig.Cache.RootPath, true, context.ApplicationContext.UserConfig.Cache.MaxCacheSize, context.ApplicationContext.UserConfig.Cache.AutoGC, context.ApplicationContext.Log);
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

        private class NullCacheManager : ICacheManager
        {
            public bool TryGet(string dbName, StateHash hash, bool updateHit, out string path)
            {
                path = null;
                return false;
            }

            public void Add(DatabaseConnectionConfig dbConfig, string dbName, StateHash hash)
            {
            }

            public void UpdateHits(IEnumerable<Tuple<string, StateHash>> keys)
            {
            }

            public void GarbageCollect(bool silent)
            {
            }
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

            public Tuple<ExecutionNode, StateHash, bool, StateHash> Calculate(DeployContext context, StateHash hash, StateHash startingHash, ICacheManager cacheManager)
            {
                if (mTransform != null)
                {
                    hash = mTransform.CalculateTransform(hash);
                    if (hash == startingHash)
                        return Tuple.Create((ExecutionNode)null, hash, true, (StateHash)null);

                    var backups = GetCachedBackups(cacheManager, hash, context.ApplicationContext.ProjectConfig.Databases);
                    if (backups != null)
                    {
                        var node = new ExecutionNode("Restoring state from cache...", "Cache restored");
                        node.AddChild(new ExecutionNode(new RestoreDatabasesTransform(context.ApplicationContext.UserConfig.Databases.Connection, backups, hash)));

                        return Tuple.Create(node, hash, true, hash);
                    }
                }
                else if (mChildren.Count > 0)
                {
                    var result = new ExecutionNode(mLogPre, mLogPost);
                    var changed = false;
                    StateHash cacheHash = null;

                    foreach (var child in mChildren)
                    {
                        var calc = child.Calculate(context, hash, startingHash, cacheManager);
                        if (calc.Item3)
                        {
                            changed = true;
                            result.mChildren.Clear();
                        }

                        if (calc.Item1 != null)
                            result.mChildren.Add(calc.Item1);

                        hash = calc.Item2;
                        if (calc.Item4 != null)
                            cacheHash = calc.Item4;
                    }

                    if (result.mChildren.Count == 0)
                        result = null;

                    return Tuple.Create(result, hash, changed, cacheHash);
                }

                return Tuple.Create(this, hash, false, (StateHash)null);
            }

            public StateHash Run(DeployContext context, StateHash hash, ICacheManager cacheManager, bool first = true, bool last = true)
            {
                if (mLogPre != null)
                    context.ApplicationContext.Log.Log(mLogPre);

                if (mTransform != null)
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    hash = mTransform.RunTransform(hash, context.DryRun, context.ApplicationContext.Log);
                    stopWatch.Stop();

                    var rhf = GetResumeHashFile(context);
                    File.WriteAllText(rhf.FullName, hash.ToHexString());

                    if (!first && !last && stopWatch.Elapsed >= context.ApplicationContext.UserConfig.Cache.MinDeployTime)
                    {
                        foreach (var db in context.ApplicationContext.ProjectConfig.Databases)
                        {
                            cacheManager.Add(context.ApplicationContext.UserConfig.Databases.Connection, db, hash);
                        }
                    }
                }
                else if (mChildren.Count > 0)
                {
                    using (context.ApplicationContext.Log.IndentScope())
                    {
                        for (var i = 0; i < mChildren.Count; i++)
                        {
                            var child = mChildren[i];
                            hash = child.Run(context, hash, cacheManager, first, last && i == mChildren.Count - 1);
                            first = false;
                        }
                    }
                }

                if (mLogPost != null)
                    context.ApplicationContext.Log.Log(mLogPost);

                return hash;
            }

            private static DatabaseBackupInfo[] GetCachedBackups(ICacheManager cacheManager, StateHash hash, IReadOnlyCollection<string> dbs)
            {
                var result = new DatabaseBackupInfo[dbs.Count];
                var i = 0;

                foreach (var db in dbs)
                {
                    string path;
                    if (!cacheManager.TryGet(db, hash, false, out path))
                        return null;

                    result[i++] = new DatabaseBackupInfo(db, path);
                }

                return result;
            }
        }
    }
}
