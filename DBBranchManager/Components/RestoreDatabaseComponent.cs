using DBBranchManager.Config;
using System.Collections.Generic;

namespace DBBranchManager.Components
{
    internal class RestoreDatabaseComponent : IComponent
    {
        private readonly DatabaseInfo mDatabaseInfo;

        public RestoreDatabaseComponent(DatabaseInfo databaseInfo)
        {
            mDatabaseInfo = databaseInfo;
        }

        public IEnumerable<string> Run(ComponentState componentState)
        {
            var script = string.Format(@"
USE [master]
ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
RESTORE DATABASE [{0}] FROM  DISK = N'{1}' WITH  FILE = 1,  NOUNLOAD,  STATS = 5
ALTER DATABASE [{0}] SET MULTI_USER
", mDatabaseInfo.Name, mDatabaseInfo.BackupFilePath);

            yield return string.Format("Restoring {0}", mDatabaseInfo.Name);

            Utils.SqlUtils.SqlCmdExec(mDatabaseInfo.Connection, script);
        }
    }
}

namespace DBBranchManager.Utils
{
}