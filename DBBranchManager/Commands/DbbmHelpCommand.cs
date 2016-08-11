using System;
using System.Collections.Generic;
using System.Linq;
using DBBranchManager.Constants;
using DBBranchManager.Entities;
using DBBranchManager.Exceptions;
using Mono.Options;

namespace DBBranchManager.Commands
{
    internal class DbbmHelpCommand : DbbmCommand
    {
        private readonly Dictionary<string, DbbmCommand> mCommands;

        public DbbmHelpCommand(Dictionary<string, DbbmCommand> commands)
        {
            mCommands = commands;
        }

        public override string Description
        {
            get { return "Display help usage."; }
        }

        public override void Run(ApplicationContext appContext, IEnumerable<string> args)
        {
            var p = new OptionSet();
            var extra = Parse(p, args,
                CommandConstants.Help,
                "[COMMAND]");

            if (extra == null || extra.Count == 0)
            {
                Console.WriteLine("Usage: dbbm <COMMAND> [OPTIONS]+");
                Console.WriteLine();
                Console.WriteLine("Available commands:");
                Console.WriteLine();

                var maxLen = mCommands.Max(x => x.Key.Length);
                foreach (var cmd in mCommands)
                {
                    Console.WriteLine("{0}{1} - {2}", new string(' ', maxLen - cmd.Key.Length + 2), cmd.Key, cmd.Value.Description);
                }
            }
            else
            {
                DbbmCommand cmd;
                if (!mCommands.TryGetValue(extra[0], out cmd))
                    throw new SoftFailureException(string.Format("Unknown command {0}", extra[0]));

                cmd.Run(appContext, new[] { extra[0], "--help" });
            }
        }
    }
}
