namespace DBBranchManager.Entities
{
    internal class DatabaseBackupInfo
    {
        private readonly string mName;
        private readonly string mBackupFilePath;

        public DatabaseBackupInfo(string name, string backupFilePath)
        {
            mName = name;
            mBackupFilePath = backupFilePath;
        }

        public string Name
        {
            get { return mName; }
        }

        public string BackupFilePath
        {
            get { return mBackupFilePath; }
        }
    }
}