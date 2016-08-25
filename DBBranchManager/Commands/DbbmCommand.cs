using System;
using System.Collections.Generic;
using System.Linq;
using DBBranchManager.Entities;
using Mono.Options;

namespace DBBranchManager.Commands
{
    internal abstract class DbbmCommand
    {
        public abstract string Description { get; }

        public abstract void Run(ApplicationContext appContext, IEnumerable<string> args);

        protected List<string> Parse(OptionSet p, IEnumerable<string> args, string command, string prototype)
        {
            var showHelp = false;
            p.Add("h|help", "Display this help and exit.", v => showHelp = v != null);

            var argsList = args as IList<string> ?? args.ToList();
            if (args != null)
            {
                var extra = p.Parse(argsList.Skip(1));
                if (!showHelp)
                    return extra;
            }

            Console.WriteLine("Usage: dbbm {0} {1}", args == null ? command : argsList.First(), prototype);
            Console.WriteLine();
            Console.WriteLine(Description);
            Console.WriteLine();
            Console.WriteLine("Available options:");
            p.WriteOptionDescriptions(Console.Out);

            return null;
        }
    }
}
