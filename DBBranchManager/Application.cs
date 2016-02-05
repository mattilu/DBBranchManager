using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DBBranchManager.Components;
using DBBranchManager.Config;
using DBBranchManager.Dependencies;
using DBBranchManager.Invalidators;
using DBBranchManager.Utils;

namespace DBBranchManager
{
    internal class Application
    {
        private static readonly Regex ToDeployRegex = new Regex("^(?:to[ _]deploy).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Configuration mConfiguration;
        private readonly List<DatabaseInfo> mDatabases;
        private readonly List<IInvalidator> mInvalidators;
        private readonly Dictionary<string, IComponent> mBranchComponents;
        private readonly IStatefulDependencyGraph<IComponent> mDependencyGraph;
        private readonly string mActiveBranch;
        private readonly string mBackupBranch;
        private readonly Timer mDelayTimer;
        private readonly int mTimerDelay;
        private readonly bool mDryRun;
        private bool mPaused;
        private bool mPendingChanges;

        public Application(Configuration config)
        {
            mConfiguration = config;

            mDatabases = config.Databases;
            mInvalidators = new List<IInvalidator>();
            mBranchComponents = new Dictionary<string, IComponent>();
            mDelayTimer = new Timer(OnTimerTick);
            mTimerDelay = config.ExecutionDelay;

            var fsInvalidator = new FileSystemWatcherInvalidator();
            mInvalidators.Add(fsInvalidator);

            mDependencyGraph = CreateDependencyGraph(config, fsInvalidator);

            foreach (var invalidator in mInvalidators)
            {
                invalidator.Invalidated += OnInvalidated;
            }

            mActiveBranch = config.ActiveBranch;
            mBackupBranch = config.BackupBranch;
            mDryRun = config.DryRun;

            mPaused = false;
            mPendingChanges = false;
        }

        private IStatefulDependencyGraph<IComponent> CreateDependencyGraph(Configuration config, FileSystemWatcherInvalidator fsInvalidator)
        {
            var fullGraph = new StatefulDependencyGraph<IComponent>();

            foreach (var branchInfo in config.Branches)
            {
                var branchComponent = CreateBranchComponent(branchInfo, config.DatabaseConnections[0], fsInvalidator);
                mBranchComponents[branchInfo.Name] = branchComponent;
            }

            foreach (var branchInfo in config.Branches)
            {
                if (branchInfo.Parent != null)
                {
                    var source = mBranchComponents[branchInfo.Parent];
                    var target = mBranchComponents[branchInfo.Name];
                    fullGraph.AddDependency(source, target);
                }
                else
                {
                    fullGraph.AddNode(mBranchComponents[branchInfo.Name]);
                }
            }

            return fullGraph;
        }

        private static IComponent CreateBranchComponent(BranchInfo branchInfo, DatabaseConnectionInfo dbConnectionInfo, FileSystemWatcherInvalidator fsInvalidator)
        {
            var graph = new DependencyGraph<IComponent>();
            var component = new SuperComponent(graph);

            var componentIn = new BranchComponent(branchInfo.Name, "Begin");
            var componentOut = new BranchComponent(branchInfo.Name, "End");

            IComponent parent = componentIn;
            if (Directory.Exists(branchInfo.BasePath))
            {
                var toDeployDirs = Directory.EnumerateDirectories(branchInfo.BasePath)
                    .Where(x => ToDeployRegex.IsMatch(Path.GetFileName(x)));

                foreach (var deployDir in toDeployDirs)
                {
                    var releaseComponent = CreateReleaseComponent(deployDir, branchInfo.DeployPath, dbConnectionInfo);

                    fsInvalidator.AddWatch(deployDir, @"^Templates\\TPL_\d+.*$", component);
                    fsInvalidator.AddWatch(deployDir, @"^Reports\\[XTD]_\d+.*\.x(?:lsm|ml)$", component);
                    fsInvalidator.AddWatch(deployDir, @"^Scripts\\\d+\..*\.sql$", component);

                    graph.AddDependency(parent, releaseComponent);
                    parent = releaseComponent;
                }
            }
            graph.AddDependency(parent, componentOut);

            return component;
        }

        private static IComponent CreateReleaseComponent(string releaseDir, string deployPath, DatabaseConnectionInfo dbConnectionInfo)
        {
            var releaseName = Path.GetFileName(releaseDir);

            var graph = new DependencyGraph<IComponent>();
            var component = new SuperComponent(graph);

            var componentIn = new ReleaseComponent(releaseName, "Begin");
            var componentOut = new ReleaseComponent(releaseName, "End");

            var tplComponent = new TemplatesComponent(Path.Combine(releaseDir, "Templates"), deployPath);
            var reportsComponent = new ReportsComponent(Path.Combine(releaseDir, "Reports"), deployPath);
            var scriptsComponent = new ScriptsComponent(Path.Combine(releaseDir, "Scripts"), deployPath, releaseName, dbConnectionInfo);

            graph.AddDependency(componentIn, tplComponent);
            graph.AddDependency(componentIn, reportsComponent);
            graph.AddDependency(tplComponent, scriptsComponent);
            graph.AddDependency(reportsComponent, scriptsComponent);
            graph.AddDependency(scriptsComponent, componentOut);

            return component;
        }

        private void OnTimerTick(object state)
        {
            FireWorkOnMainThread();
        }

        private static void RunOnMainThread(Action func)
        {
            Program.Post(func);
        }

        private void FireWorkOnMainThread()
        {
            lock (this)
            {
                RunOnMainThread(FireWork);
            }
        }

        private void FireWork()
        {
            try
            {
                if (mPaused)
                {
                    return;
                }

                // Delay elapsed without modifications. DO IT!
                Console.WriteLine("[{0:T}] Shit's going down!\n", DateTime.Now);
                Beep("start");

                var chain = mDependencyGraph.GetPath(mBranchComponents[mBackupBranch], mBranchComponents[mActiveBranch]).ToList();
                if (chain.Count > 0)
                {
                    // Add RestoreDatabaseComponents
                    var toRun = mDatabases.Select(x => new RestoreDatabaseComponent(x)).Union(chain);

                    var s = new ComponentRunState(mDryRun);
                    foreach (var component in toRun)
                    {
                        foreach (var logLine in component.Run(s))
                        {
                            Console.WriteLine("[{0:T}] {1}", DateTime.Now, logLine);
                            if (s.Error)
                            {
                                Console.WriteLine("[{0:T}] Blocking Errors Detected ):", DateTime.Now);
                                Beep("error");

                                return;
                            }
                        }

                        //mDependencyGraph.Validate(component);
                    }
                }

                Console.WriteLine("\n[{0:T}] Success!\n", DateTime.Now);
                Beep("success");
            }
            finally
            {
                if (!mPaused)
                {
                    mPendingChanges = false;
                }
            }
        }

        private void OnInvalidated(object sender, InvalidatedEventsArgs args)
        {
            lock (this)
            {
                if (mPaused)
                {
                    mPendingChanges = true;
                    return;
                }

                Console.WriteLine("[{0:T}] Changes detected... [{1}]", DateTime.Now, args.Reason);

                foreach (var invalidatedComponent in args.InvalidatedComponents)
                {
                    mDependencyGraph.InvalidateGraph(invalidatedComponent);
                }

                mDelayTimer.Change(mTimerDelay, Timeout.Infinite);
            }
        }

        public void Start()
        {
            BeginConsoleInput();
        }

        private async Task<string> ReadLineAsync()
        {
            return await Task.Run(() => Console.ReadLine());
        }

        private async void BeginConsoleInput()
        {
            while (true)
            {
                var line = await ReadLineAsync();
                OnConsoleInput(line);
            }
        }

        private void OnConsoleInput(string line)
        {
            var argv = line.Trim().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            if (argv.Length == 0)
                return;

            var cmd = argv[0];
            switch (cmd.ToLower())
            {
                case "pause":
                case "p":
                    Pause();
                    break;

                case "resume":
                case "r":
                    Resume();
                    break;

                case "force":
                case "f":
                    FireWorkOnMainThread();
                    break;

                case "quit":
                case "q":
                    Program.Exit();
                    break;

                case "generate-scripts":
                case "gs":
                case "g":
                    GenerateScripts();
                    break;
            }
        }

        private void Pause()
        {
            lock (this)
            {
                Console.WriteLine("[{0:T}] Pausing...", DateTime.Now);
                mPaused = true;
            }
        }

        private void Resume()
        {
            lock (this)
            {
                Console.WriteLine("[{0:T}] Resuming...", DateTime.Now);
                mPaused = false;

                if (mPendingChanges)
                {
                    Console.WriteLine("[{0:T}] Pending changes detected...", DateTime.Now);
                    mDelayTimer.Change(mTimerDelay, Timeout.Infinite);
                }
            }
        }

        private void GenerateScriptsRecursive(IDependencyGraph<IComponent> graph)
        {
            var chain = graph.GetPath();

            foreach (var component in chain)
            {
                var asSuperComponent = component as SuperComponent;
                if (asSuperComponent != null)
                {
                    GenerateScriptsRecursive(asSuperComponent.Components);
                    continue;
                }

                var asScriptsComponent = component as ScriptsComponent;
                if (asScriptsComponent != null)
                {
                    var scriptFile = Path.Combine(asScriptsComponent.DeployPath, asScriptsComponent.ReleaseName + @".sql");

                    Console.WriteLine("[{0:T}] Generating {1}", DateTime.Now, scriptFile);
                    File.WriteAllText(scriptFile, asScriptsComponent.GenerateScript());
                }
            }
        }

        private void GenerateScripts()
        {
            lock (this)
            {
                var chain = mDependencyGraph.GetPath(mBranchComponents[mBackupBranch], mBranchComponents[mActiveBranch]).ToList();
                foreach (var component in chain.OfType<SuperComponent>())
                {
                    GenerateScriptsRecursive(component.Components);
                }
            }
        }

        private void Beep(string reason)
        {
            BeepInfo beep;
            if (mConfiguration.Beeps.TryGetValue(reason, out beep))
            {
                Buzzer.Beep(beep.Frequency, beep.Duration, beep.Times, beep.DutyTime);
            }
        }
    }
}