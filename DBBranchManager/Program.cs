using DBBranchManager.Config;
using DBBranchManager.Utils;
using System;
using System.Threading;

namespace DBBranchManager
{
    public static class Program
    {
        private static readonly SingleThreadSynchronizationContext mSyncContext = new SingleThreadSynchronizationContext();

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
            SynchronizationContext.SetSynchronizationContext(mSyncContext);

            Post(func);

            mSyncContext.Run();
        }

        public static void Post(Action func)
        {
            mSyncContext.Post(state => func(), null);
        }
    }
}