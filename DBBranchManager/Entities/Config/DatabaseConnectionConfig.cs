using System.Security;

namespace DBBranchManager.Entities.Config
{
    internal class DatabaseConnectionConfig
    {
        private readonly string mServer;
        private readonly string mUser;
        private readonly SecureString mPassword;
        private readonly string mRelocatePath;
        private readonly bool mRelocate;

        public DatabaseConnectionConfig(string server, string user, SecureString password, string relocatePath, bool relocate)
        {
            mServer = server;
            mUser = user;
            mPassword = password;
            mRelocatePath = relocatePath;
            mRelocate = relocate;
        }

        public string Server
        {
            get { return mServer; }
        }

        public string User
        {
            get { return mUser; }
        }

        public SecureString Password
        {
            get { return mPassword; }
        }

        public string RelocatePath
        {
            get { return mRelocatePath; }
        }

        public bool Relocate
        {
            get { return mRelocate; }
        }
    }
}