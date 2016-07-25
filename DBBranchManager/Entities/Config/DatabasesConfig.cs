
using System.Text.RegularExpressions;

namespace DBBranchManager.Entities.Config
{
    internal class DatabasesConfig
    {
        private readonly DatabaseConnectionConfig mConnection;
        private readonly BackupsConfig mBackups;

        public DatabasesConfig(DatabaseConnectionConfig connection, BackupsConfig backups)
        {
            mConnection = connection;
            mBackups = backups;
        }

        public DatabaseConnectionConfig Connection
        {
            get { return mConnection; }
        }

        public BackupsConfig Backups
        {
            get { return mBackups; }
        }

        public class BackupsConfig
        {
            private readonly string mRoot;
            private readonly Regex mPattern;

            public BackupsConfig(string root, Regex pattern)
            {
                mRoot = root;
                mPattern = pattern;
            }

            public string Root
            {
                get { return mRoot; }
            }

            public Regex Pattern
            {
                get { return mPattern; }
            }
        }
    }
}