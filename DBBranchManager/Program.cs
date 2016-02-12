using System;
using System.Threading;
using DBBranchManager.Config;
using DBBranchManager.Utils;

namespace DBBranchManager
{
    public static class Program
    {
        private static readonly SingleThreadSynchronizationContext SyncContext = new SingleThreadSynchronizationContext();
        private const string ConfigFilePath = @"..\..\config.json";

        public static void Main(string[] args)
        {
            bool restart;
            do
            {
                restart = false;
                try
                {
                    var config = Configuration.LoadFromJson(ConfigFilePath);

                    using (var watcher = new EnhanchedFileSystemWatcher())
                    using (var app = new Application(config))
                    {
                        EnhanchedFileSystemWatcherChangeEventHandler handler = null;
                        handler = (sender, eventArgs) =>
                        {
                            Post(() =>
                            {
                                Console.WriteLine("\nChanges detected in config file. Restarting...");
                                restart = true;
                            });
                            watcher.Changed -= handler;
                        };
                        watcher.Changed += handler;
                        watcher.AddWatch(ConfigFilePath, null);
                        Run(app.Start);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Console.WriteLine("\nRestarting...");
                    restart = true;
                }
            } while (restart);
        }

        private static void Run(Action func)
        {
            SynchronizationContext.SetSynchronizationContext(SyncContext);

            Post(func);

            SyncContext.Run();
        }

        public static void Post(Action func)
        {
            SyncContext.Post(state => func(), null);
        }

        public static void Exit()
        {
            SyncContext.Complete();
        }
    }
}