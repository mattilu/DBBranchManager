using DBBranchManager.Config;

namespace DBBranchManager.Entities
{
    internal class CommandLineArguments
    {
        private readonly string mCommand;
        private readonly bool mDryRun;
        private readonly string[] mUnparsed;

        public CommandLineArguments(string command, bool dryRun, string[] unparsed)
        {
            mCommand = command;
            mDryRun = dryRun;
            mUnparsed = unparsed;
        }

        public string Command
        {
            get { return mCommand; }
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