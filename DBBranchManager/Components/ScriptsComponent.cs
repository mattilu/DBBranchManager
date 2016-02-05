using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DBBranchManager.Config;
using DBBranchManager.Utils;

namespace DBBranchManager.Components
{
    internal class ScriptsComponent : IComponent
    {
        private static readonly Regex ScriptFileRegex = new Regex(@"^\d+(?:-(?<env>[^.]+))?\..*\.sql$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly string mScriptsPath;
        private readonly string mDeployPath;
        private readonly string mReleaseName;
        private readonly DatabaseConnectionInfo mDatabaseConnection;

        public ScriptsComponent(string scriptsPath, string deployPath, string releaseName, DatabaseConnectionInfo databaseConnection)
        {
            mScriptsPath = scriptsPath;
            mDeployPath = deployPath;
            mReleaseName = releaseName;
            mDatabaseConnection = databaseConnection;
        }

        public IEnumerable<string> GenerateScript(string environment, StringBuilder sb)
        {
            sb.AppendFormat(@"
:on error exit
:setvar path ""{0}""

USE [DB_AVIVA_S2]
GO

SET XACT_ABORT ON
GO

BEGIN TRANSACTION
GO

TRUNCATE TABLE [Interdependencies].[TBC_CACHE_ITEM_DEPENDENCY]
", mScriptsPath);

            foreach (var file in FileUtils.EnumerateFiles(mScriptsPath, ScriptFileRegex.IsMatch))
            {
                var match = ScriptFileRegex.Match(file);
                if (match.Success && (!match.Groups["env"].Success || match.Groups["env"].Value == environment))
                {
                    sb.AppendFormat("\nPRINT 'BEGIN {0}'\nGO\n:r $(path)\\\"{0}\"\nGO\nPRINT 'END {0}'", file);
                    yield return string.Format("Adding {0}", file);
                }
                else
                {
                    yield return string.Format("Skipping {0}", file);
                }
            }

            sb.Append("\nGO\n\n--ROLLBACK TRANSACTION\nPRINT 'Committing...'\nCOMMIT TRANSACTION");
        }

        public IEnumerable<string> Run(ComponentRunState runState)
        {
            if (Directory.Exists(mScriptsPath))
            {
                yield return string.Format("Scripts: {0}", mScriptsPath);

                var sb = new StringBuilder();
                GenerateScript(runState.Environment, sb).RunToEnd();

                var script = sb.ToString();
                yield return "Running script...";

                string errors = null;
                try
                {
                    if (!runState.DryRun)
                        SqlUtils.SqlCmdExec(mDatabaseConnection, script);
                }
                catch (SqlCmdFailedException ex)
                {
                    errors = ex.Messages;
                }

                if (errors != null)
                {
                    runState.Error = true;
                    yield return errors;
                    yield break;
                }
            }
        }

        public string DeployPath { get { return mDeployPath; } }

        public string ReleaseName { get { return mReleaseName; } }
    }
}