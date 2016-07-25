using System;

namespace DBBranchManager
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var app = new Application(args);
                app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled {0}", ex.GetType());
                Console.WriteLine("{0}", ex.Message);
                Console.WriteLine("{0}", ex.StackTrace);
            }
        }
    }
}