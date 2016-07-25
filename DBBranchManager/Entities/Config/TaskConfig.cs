using System.Collections.Generic;

namespace DBBranchManager.Entities.Config
{
    internal class TaskConfig
    {
        private readonly string mTaskName;
        private readonly Dictionary<string, string> mParameters;

        public TaskConfig(string taskName, Dictionary<string, string> parameters)
        {
            mTaskName = taskName;
            mParameters = parameters;
        }

        public string TaskName
        {
            get { return mTaskName; }
        }

        public Dictionary<string, string> Parameters
        {
            get { return mParameters; }
        }
    }
}