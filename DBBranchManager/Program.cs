using DBBranchManager.Config;

namespace DBBranchManager
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var config = Configuration.LoadFromJson(@"..\..\config.json");
            var app = new Application(config);
            app.Run();
        }
    }
}