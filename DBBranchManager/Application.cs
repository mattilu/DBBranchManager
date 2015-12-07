using DBBranchManager.Components;
using DBBranchManager.Config;
using DBBranchManager.Dependencies;
using DBBranchManager.Invalidators;
using QuickGraph;
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
        private readonly Regex mToDeployRegex = new Regex("^(?:to[ _]deploy).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly List<IInvalidator> mInvalidators;
        private readonly Dictionary<string, Tuple<IComponent, IComponent>> mBranchComponents;
        private readonly IDependencyGraph mDependencyGraph;
        private readonly string mActiveBranch;
        private readonly List<DatabaseInfo> mDatabases;
        private readonly Timer mDelayTimer;
        private readonly int mTimerDelay;
        private bool mWorking;

        public Application(Configuration config)
        {
            mInvalidators = new List<IInvalidator>();
            mBranchComponents = new Dictionary<string, Tuple<IComponent, IComponent>>();
            mDatabases = config.Databases;
            mDelayTimer = new Timer(OnTimerTick);
            mTimerDelay = config.ExecutionDelay;

            var depGraph = new DependencyGraph();

            var fsInvalidator = new FileSystemWatcherInvalidator();

            foreach (var branchInfo in config.Branches)
            {
                var branchComponentIn = new BranchComponent(branchInfo.Name, "Begin");
                var branchComponentOut = new BranchComponent(branchInfo.Name, "End");
                mBranchComponents[branchInfo.Name] = Tuple.Create<IComponent, IComponent>(branchComponentIn, branchComponentOut);

                var toDeployDirs = Directory.EnumerateDirectories(branchInfo.BasePath)
                    .Where(x => mToDeployRegex.IsMatch(Path.GetFileName(x)));

                IComponent parent = branchComponentIn;
                foreach (var dir in toDeployDirs)
                {
                    var tplComponent = new TemplatesComponent(Path.Combine(dir, "Templates"), branchInfo.DeployPath);
                    var reportsComponent = new ReportsComponent(Path.Combine(dir, "Reports"), branchInfo.DeployPath);
                    var scriptsComponent = new ScriptsComponent(Path.Combine(dir, "Scripts"), config.DatabaseConnections[0]);

                    var releaseName = Path.GetFileName(dir);
                    var releaseComponentIn = new ReleaseComponent(releaseName, "Begin");
                    var releaseComponentOut = new ReleaseComponent(releaseName, "End");

                    depGraph.AddVerticesAndEdge(new Edge<IComponent>(parent, releaseComponentIn));
                    depGraph.AddVerticesAndEdge(new Edge<IComponent>(releaseComponentIn, tplComponent));
                    depGraph.AddVerticesAndEdge(new Edge<IComponent>(releaseComponentIn, reportsComponent));
                    depGraph.AddVerticesAndEdge(new Edge<IComponent>(tplComponent, scriptsComponent));
                    depGraph.AddVerticesAndEdge(new Edge<IComponent>(reportsComponent, scriptsComponent));
                    depGraph.AddVerticesAndEdge(new Edge<IComponent>(scriptsComponent, releaseComponentOut));

                    fsInvalidator.AddWatch(dir, @"^Templates\\TPL_\d+.*$", tplComponent);
                    fsInvalidator.AddWatch(dir, @"^Reports\\[XTD]_\d+.*\.x(?:lsm|ml)$", reportsComponent);
                    fsInvalidator.AddWatch(dir, @"^Scripts\\\d+\..*\.sql$", scriptsComponent);

                    parent = releaseComponentOut;
                }

                depGraph.AddVerticesAndEdge(new Edge<IComponent>(parent, branchComponentOut));
            }

            foreach (var branchInfo in config.Branches)
            {
                if (branchInfo.Parent != null)
                {
                    var from = mBranchComponents[branchInfo.Parent].Item2;
                    var to = mBranchComponents[branchInfo.Name].Item1;
                    depGraph.AddVerticesAndEdge(new Edge<IComponent>(from, to));
                }
            }

            fsInvalidator.Invalidated += OnInvalidated;
            mInvalidators.Add(fsInvalidator);

            mActiveBranch = config.ActiveBranch;

            mDependencyGraph = depGraph;
        }

        private void OnTimerTick(object state)
        {
            lock (this)
            {
                // Delay elapsed without modifications. DO IT!
                Console.WriteLine("Shit's going down!\n");

                var chain = mDependencyGraph.GetValidationChain(mBranchComponents[mActiveBranch].Item2).ToList();
                if (chain.Count > 0)
                {
                    // Add RestoreDatabaseComponents
                    var toRun = mDatabases.Select(x => new RestoreDatabaseComponent(x)).Union(chain);

                    foreach (var component in toRun)
                    {
                        var s = new ComponentState();
                        foreach (var logLine in component.Run(s))
                        {
                            Console.WriteLine(logLine);
                            if (s.Error)
                            {
                                Console.WriteLine("Blocking Errors Detected ):");
                                return;
                            }
                        }

                        mDependencyGraph.Validate(component);
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
                    mDependencyGraph.Invalidate(invalidatedComponent);
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