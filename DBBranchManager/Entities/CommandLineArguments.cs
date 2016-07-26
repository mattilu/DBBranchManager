using DBBranchManager.Config;

namespace DBBranchManager.Entities
{
    internal class CommandLineArguments
    {
        private readonly string mCommand;
        private readonly string mRelease;
        private readonly bool mDryRun;
        private readonly string[] mUnparsed;

        public CommandLineArguments(string command, string release, bool dryRun, string[] unparsed)
        {
            mCommand = command;
            mRelease = release;
            mDryRun = dryRun;
            mUnparsed = unparsed;
        }

        public string Command
        {
            get { return mCommand; }
        }

        public string Release
        {
            get { return mRelease; }
        }

        public bool DryRun
        {
            get { return mDryRun; }
        }

        public string[] Unparsed
        {
            get { return mUnparsed; }
        }


        public static CommandLineArguments Parse(string[] args)
        {
            var parser = new CommandLineArgumentsParser(args);
            return parser.Parse();
        }
    }
}