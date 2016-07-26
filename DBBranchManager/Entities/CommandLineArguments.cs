using DBBranchManager.Config;

namespace DBBranchManager.Entities
{
    internal class CommandLineArguments
    {
        private readonly string mCommand;
        private readonly string[] mUnparsed;

        public CommandLineArguments(string command, string[] unparsed)
        {
            mCommand = command;
            mUnparsed = unparsed;
        }

        public string Command
        {
            get { return mCommand; }
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