using DBBranchManager.Components;
using DBBranchManager.Config;
using DBBranchManager.Dependencies;
using DBBranchManager.Invalidators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace DBBranchManager
{
    internal class Application
    {
        private static readonly Regex ToDeployRegex = new Regex("^(?:to[ _]deploy).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly List<DatabaseInfo> mDatabases;
        private readonly List<IInvalidator> mInvalidators;
        private readonly Dictionary<string, IComponent> mBranchComponents;
        private readonly IStatefulDependencyGraph<IComponent> mDependencyGraph;
        private readonly string mActiveBranch;
        private readonly string mBackupBranch;
        private readonly Timer mDelayTimer;
        private readonly int mTimerDelay;
        private readonly bool mDryRun;

        public Application(Configuration config)
        {
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
            var scriptsComponent = new ScriptsComponent(Path.Combine(releaseDir, "Scripts"), dbConnectionInfo);

            graph.AddDependency(componentIn, tplComponent);
            graph.AddDependency(componentIn, reportsComponent);
            graph.AddDependency(tplComponent, scriptsComponent);
            graph.AddDependency(reportsComponent, scriptsComponent);
            graph.AddDependency(scriptsComponent, componentOut);

            return component;
        }

        private void OnTimerTick(object state)
        {
            lock (this)
            {
                // Delay elapsed without modifications. DO IT!
                Console.WriteLine("Shit's going down!\n");

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
                            Console.WriteLine(logLine);
                            if (s.Error)
                            {
                                Console.WriteLine("Blocking Errors Detected ):");
                                return;
                            }
                        }

                        //mDependencyGraph.Validate(component);
                    }
                }

                Console.WriteLine("\nSuccess!\n");
            }
        }

        private void OnInvalidated(object sender, InvalidatedEventsArgs args)
        {
            lock (this)
            {
                Console.WriteLine("Changes detected...");

                foreach (var invalidatedComponent in args.InvalidatedComponents)
                {
                    mDependencyGraph.InvalidateGraph(invalidatedComponent);
                }

                mDelayTimer.Change(mTimerDelay, Timeout.Infinite);
            }
        }

        public void Run()
        {
            System.Windows.Forms.Application.Run();
        }
    }
}