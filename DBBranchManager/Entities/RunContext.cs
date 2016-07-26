using DBBranchManager.Constants;
using DBBranchManager.Entities.Config;
using DBBranchManager.Exceptions;
using DBBranchManager.Logging;
using DBBranchManager.Tasks;

namespace DBBranchManager.Entities
{
    internal class RunContext
    {
        private readonly CommandLineArguments mCommandLine;
        private readonly string mProjectRoot;
        private readonly UserConfig mUserConfig;
        private readonly ProjectConfig mProjectConfig;
        private readonly ReleasesConfig mReleases;
        private readonly FeatureConfigCollection mFeatures;
        private readonly TaskDefinitionConfigCollection mTasks;
        private readonly TaskManager mTaskManager;
        private readonly ILog mLog;
        private readonly ReleaseConfig mActiveRelease;
        private readonly EnvironmentConfig mActiveEnvironment;
        private readonly bool mDryRun;

        public RunContext(CommandLineArguments commandLine, string projectRoot, UserConfig userConfig, ProjectConfig projectConfig, ReleasesConfig releases, FeatureConfigCollection features, TaskDefinitionConfigCollection tasks, TaskManager taskManager, ILog log)
        {
            mCommandLine = commandLine;
            mProjectRoot = projectRoot;
            mUserConfig = userConfig;
            mProjectConfig = projectConfig;
            mReleases = releases;
            mFeatures = features;
            mTasks = tasks;
            mTaskManager = taskManager;
            mLog = log;

            if (!releases.Releases.TryGet(releases.DefaultRelease, out mActiveRelease))
            {
                throw new SoftFailureException(string.Format("Cannot find Release '{0}'", releases.DefaultRelease));
            }

            var userEnv = mUserConfig.EnvironmentVariables.GetOrDefault(EnvironmentConstants.Environment, EnvironmentConstants.DefaultEnvironment);
            if (!mProjectConfig.Environments.TryGet(userEnv, out mActiveEnvironment))
            {
                throw new SoftFailureException(string.Format("Cannot find Environment '{0}'", userEnv));
            }

            mDryRun = commandLine.DryRun;
        }

        public CommandLineArguments CommandLine
        {
            get { return mCommandLine; }
        }

        public string ProjectRoot
        {
            get { return mProjectRoot; }
        }

        public UserConfig UserConfig
        {
            get { return mUserConfig; }
        }

        public ProjectConfig ProjectConfig
        {
            get { return mProjectConfig; }
        }

        public ReleasesConfig Releases
        {
            get { return mReleases; }
        }

        public FeatureConfigCollection Features
        {
            get { return mFeatures; }
        }

        public TaskDefinitionConfigCollection Tasks
        {
            get { return mTasks; }
        }

        public TaskManager TaskManager
        {
            get { return mTaskManager; }
        }

        public ILog Log
        {
            get { return mLog; }
        }

        public ReleaseConfig ActiveRelease
        {
            get { return mActiveRelease; }
        }

        public EnvironmentConfig ActiveEnvironment
        {
            get { return mActiveEnvironment; }
        }

        public bool DryRun
        {
            get { return mDryRun; }
        }
    }
}
