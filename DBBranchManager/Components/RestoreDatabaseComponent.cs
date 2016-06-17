using System.Collections.Generic;
using DBBranchManager.Config;
using DBBranchManager.Constants;
using DBBranchManager.Utils.Sql;

namespace DBBranchManager.Components
{
    internal class RestoreDatabaseComponent : ComponentBase
    {
        private readonly DatabaseInfo mDatabaseInfo;

        public RestoreDatabaseComponent(DatabaseInfo databaseInfo)
        {
            mDatabaseInfo = databaseInfo;
        }

        [RunAction(ActionConstants.Deploy)]
        private IEnumerable<string> DeployRun(string action, ComponentRunContext runContext)
        {
            var script = string.Format("USE [master]\n" +
                                       "ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE\n" +
                                       "RESTORE DATABASE [{0}] FROM  DISK = N\'{1}\' WITH  FILE = 1,  NOUNLOAD,  STATS = 5\n" +
                                       "ALTER DATABASE [{0}] SET MULTI_USER\n",
                mDatabaseInfo.Name, mDatabaseInfo.BackupFilePath);

            yield return string.Format("Restoring {0}", mDatabaseInfo.Name);

            if (!runContext.DryRun)
            {
                using (var process = SqlUtils.Exec(mDatabaseInfo.Connection, script))
                {
                    foreach (var processOutputLine in process.GetOutput())
                    {
                        yield return processOutputLine.Line;
                    }
                }
            }

            yield return "Database restore completed.";
        }
    }
}