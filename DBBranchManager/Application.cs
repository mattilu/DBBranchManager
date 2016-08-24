using System;
using System.Collections.Generic;
using System.IO;
using DBBranchManager.Commands;
using DBBranchManager.Constants;
using DBBranchManager.Entities;
using DBBranchManager.Entities.Config;
using DBBranchManager.Exceptions;
using DBBranchManager.Logging;
using Mono.Options;

namespace DBBranchManager
{
    internal class Application
    {
        private readonly Dictionary<string, DbbmCommand> mSubCommands;
        private readonly ApplicationContext mApplicationContext;

        public Application()
        {
            mSubCommands = new Dictionary<string, DbbmCommand>
            {
                { CommandConstants.Deploy, new DbbmDeployCommand() },
                { CommandConstants.Run, new DbbmRunCommand() },
                { CommandConstants.GarbageCollect, new DbbmGarbageCollectCommand() }
            };
            mSubCommands.Add(CommandConstants.Help, new DbbmHelpCommand(mSubCommands));

            var projectRoot = DiscoverProjectRoot();
            var projectConfig = ProjectConfig.LoadFromJson(Path.Combine(projectRoot, FileConstants.ProjectFileName));
            var userConfig = UserConfig.LoadFromJson(Path.Combine(projectRoot, FileConstants.UserFileName));

            mApplicationContext = new ApplicationContext(projectRoot, projectConfig, userConfig, new ConsoleLog());
        }

        public int Run(string[] args)
        {
            var showHelp = false;
            var p = new OptionSet
            {
                { "h|help", "Display usage help and exit", v => showHelp = v != null }
            };

            var extra = p.Parse(args);
            if (extra.Count == 0)
                extra.Add(CommandConstants.Help);
            else if (showHelp)
                extra.Add("--help");

            DbbmCommand cmd;
            if (!mSubCommands.TryGetValue(extra[0], out cmd))
                throw new SoftFailureException(string.Format("Unknown command: {0}", extra[0]));

            cmd.Run(mApplicationContext, extra);

            return 0;
        }

        private static string DiscoverProjectRoot()
        {
            var path = Environment.CurrentDirectory;
            do
            {
                if (File.Exists(Path.Combine(path, FileConstants.ProjectFileName)))
                    return path;

                path = Path.GetDirectoryName(path);
            } while (path != null);

            throw new SoftFailureException("Cannot find project root");
        }
    }
}
