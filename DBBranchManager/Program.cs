using System;
using DBBranchManager.Exceptions;

namespace DBBranchManager
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var app = new Application(args);
                return app.Run();
            }
            catch (SoftFailureException ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled {0}", ex.GetType());
                Console.WriteLine("{0}", ex.Message);
                Console.WriteLine("{0}", ex.StackTrace);
                return 2;
            }
        }
    }
}