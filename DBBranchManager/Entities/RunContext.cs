using System.IO;
using DBBranchManager.Constants;
using DBBranchManager.Entities.Config;
using DBBranchManager.Exceptions;
using DBBranchManager.Tasks;
using DBBranchManager.Utils;

namespace DBBranchManager.Entities
{
    internal class RunContext
    {
        private readonly ApplicationContext mApplicationContext;
        private readonly string mAction;
        private readonly ReleasesConfig mReleasesConfig;
        private readonly FeatureConfigCollection mFeaturesConfig;
        private readonly TaskDefinitionConfigCollection mTaskDefinitionsConfig;
        private readonly TaskManager mTaskManager;
        private readonly ReleaseConfig mActiveRelease;
        private readonly EnvironmentConfig mActiveEnvironment;
        private readonly bool mDryRun;

        private RunContext(ApplicationContext applicationContext, string action, ReleasesConfig releasesConfig, FeatureConfigCollection featuresConfig, TaskDefinitionConfigCollection taskDefinitionsConfig, TaskManager taskManager, ReleaseConfig activeRelease, EnvironmentConfig activeEnvironment, bool dryRun)
        {
            mApplicationContext = applicationContext;
            mAction = action;
            mReleasesConfig = releasesConfig;
            mFeaturesConfig = featuresConfig;
            mTaskDefinitionsConfig = taskDefinitionsConfig;
            mTaskManager = taskManager;
            mActiveRelease = activeRelease;
            mActiveEnvironment = activeEnvironment;
            mDryRun = dryRun;
        }

        public ApplicationContext ApplicationContext
        {
            get { return mApplicationContext; }
        }

        public string Action
        {
            get { return mAction; }
        }

        public ReleasesConfig ReleasesConfig
        {
            get { return mReleasesConfig; }
        }

        public FeatureConfigCollection FeaturesConfig
        {
            get { return mFeaturesConfig; }
        }

        public TaskDefinitionConfigCollection TaskDefinitionsConfig
        {
            get { return mTaskDefinitionsConfig; }
        }

        public TaskManager TaskManager
        {
            get { return mTaskManager; }
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

        public static RunContext Create(ApplicationContext applicationContext, string action, string release, string env, bool dryRun)
        {
            var releasesFile = Path.Combine(applicationContext.ProjectRoot, applicationContext.ProjectConfig.Releases);
            var releases = ReleasesConfig.LoadFromJson(releasesFile);

            var featuresFiles = FileUtils.ExpandGlob(Path.Combine(applicationContext.ProjectRoot, applicationContext.ProjectConfig.Features));
            var features = FeatureConfigCollection.LoadFromMultipleJsons(featuresFiles);

            var tasksFiles = FileUtils.ExpandGlob(Path.Combine(applicationContext.ProjectRoot, applicationContext.ProjectConfig.Tasks));
            var tasks = TaskDefinitionConfigCollection.LoadFromMultipleJsons(tasksFiles);

            if (string.IsNullOrWhiteSpace(release))
                release = applicationContext.UserConfig.ActiveRelease ?? releases.DefaultRelease;
            if (string.IsNullOrWhiteSpace(env))
                env = applicationContext.UserConfig.EnvironmentVariables.GetOrDefault(EnvironmentConstants.Environment, EnvironmentConstants.DefaultEnvironment);

            ReleaseConfig activeRelease;
            if (!releases.Releases.TryGet(release, out activeRelease))
                throw new SoftFailureException(string.Format("Cannot find Release '{0}'", release));

            EnvironmentConfig activeEnvironment;
            if (!applicationContext.ProjectConfig.Environments.TryGet(env, out activeEnvironment))
                throw new SoftFailureException(string.Format("Cannot find Environment '{0}'", env));

            var taskManager = new TaskManager(tasks);

            return new RunContext(applicationContext, action, releases, features, tasks, taskManager, activeRelease, activeEnvironment, dryRun);
        }
    }
}
