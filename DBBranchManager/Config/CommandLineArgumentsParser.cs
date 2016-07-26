using System.Collections.Generic;
using DBBranchManager.Entities;
using DBBranchManager.Exceptions;

namespace DBBranchManager.Config
{
    internal class CommandLineArgumentsParser
    {
        private readonly string[] mArgs;
        private int mIndex;
        private bool mGotCommand;

        public CommandLineArgumentsParser(string[] args)
        {
            mArgs = args;
        }

        public CommandLineArguments Parse()
        {
            var unparsed = new List<string>();
            var command = "help";

            mIndex = 0;
            while (mIndex < mArgs.Length)
            {
                string value;
                if (!mArgs[mIndex].StartsWith("-"))
                {
                    if (mGotCommand)
                        throw new SoftFailureException("Multiple commands specified");

                    command = mArgs[mIndex];
                    mGotCommand = true;
                }
                else
                {
                    unparsed.Add(mArgs[mIndex]);
                }

                ++mIndex;
            }

            return new CommandLineArguments(command, unparsed.ToArray());
        }

        private bool TryGetArg(string shortName, string longName, out string value)
        {
            if (mArgs[mIndex] == shortName)
                value = mArgs[++mIndex];
            else if (mArgs[mIndex].StartsWith(longName))
                value = mArgs[mIndex].Substring(longName.Length);
            else
            {
                value = null;
                return false;
            }

            return true;
        }
    }
}