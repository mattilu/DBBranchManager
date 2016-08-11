using DBBranchManager.Entities.Config;
using DBBranchManager.Logging;

namespace DBBranchManager.Entities
{
    internal class ApplicationContext
    {
        private readonly string mProjectRoot;
        private readonly ProjectConfig mProjectConfig;
        private readonly UserConfig mUserConfig;
        private readonly ILog mLog;

        public ApplicationContext(string projectRoot, ProjectConfig projectConfig, UserConfig userConfig, ILog log)
        {
            mProjectRoot = projectRoot;
            mProjectConfig = projectConfig;
            mUserConfig = userConfig;
            mLog = log;
        }

        public string ProjectRoot
        {
            get { return mProjectRoot; }
        }

        public ProjectConfig ProjectConfig
        {
            get { return mProjectConfig; }
        }

        public UserConfig UserConfig
        {
            get { return mUserConfig; }
        }

        public ILog Log
        {
            get { return mLog; }
        }
    }
}
