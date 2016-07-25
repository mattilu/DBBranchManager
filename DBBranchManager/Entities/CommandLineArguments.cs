using DBBranchManager.Config;

namespace DBBranchManager.Entities
{
    internal class CommandLineArguments
    {
        private readonly string mConfigFile;
        private readonly string mCommand;
        private readonly string[] mUnparsed;

        public CommandLineArguments(string configFile, string command, string[] unparsed)
        {
            mConfigFile = configFile;
            mCommand = command;
            mUnparsed = unparsed;
        }

        public string ConfigFile
        {
            get { return mConfigFile; }
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