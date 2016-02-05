using DBBranchManager.Config;
using DBBranchManager.Utils;
using System;
using System.Threading;

namespace DBBranchManager
{
    public static class Program
    {
        private static readonly SingleThreadSynchronizationContext SyncContext = new SingleThreadSynchronizationContext();

        public static void Main(string[] args)
        {
            try
            {
                Run(() =>
                {
                    var config = Configuration.LoadFromJson(@"..\..\config.json");
                    var app = new Application(config);
                    app.Start();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("\nPress any key to restart.");
                Console.ReadKey(true);
                System.Windows.Forms.Application.Restart();
            }
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