using DBBranchManager.Config;
using DBBranchManager.Utils;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DBBranchManager.Components
{
    internal class ScriptsComponent : IComponent
    {
        private static readonly Regex ScriptFileRegex = new Regex(@"^\d+\..*\.sql$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly string mScriptsPath;
        private readonly DatabaseConnectionInfo mDatabaseConnection;

        public ScriptsComponent(string scriptsPath, DatabaseConnectionInfo databaseConnection)
        {
            mScriptsPath = scriptsPath;
            mDatabaseConnection = databaseConnection;
        }

        public IEnumerable<string> Run(ComponentRunState runState)
        {
            if (Directory.Exists(mScriptsPath))
            {
                yield return string.Format("Scripts: {0}", mScriptsPath);

                var sb = new StringBuilder();
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
                    if (ScriptFileRegex.IsMatch(file))
                    {
                        sb.AppendFormat("\nGO\n:r $(path)\\\"{0}\"", file);
                    }
                }

                var script = sb.ToString();

                var withRollback = script + "\nGO\n\nROLLBACK TRANSACTION\n--COMMIT TRANSACTION";
                yield return "Running script with ROLLBACK";

                string errors = null;
                try
                {
                    if (!runState.DryRun)
                        SqlUtils.SqlCmdExec(mDatabaseConnection, withRollback);
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

                var withCommit = script + "\nGO\n\n--ROLLBACK TRANSACTION\nCOMMIT TRANSACTION";
                yield return "Running script with COMMIT";

                if (!runState.DryRun)
                    SqlUtils.SqlCmdExec(mDatabaseConnection, withCommit);
            }
        }
    }
}