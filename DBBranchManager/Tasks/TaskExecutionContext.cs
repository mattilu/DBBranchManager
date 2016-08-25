using System.Collections.Generic;
using DBBranchManager.Entities;
using DBBranchManager.Entities.Config;
using DBBranchManager.Logging;

namespace DBBranchManager.Tasks
{
    internal class TaskExecutionContext
    {
        private readonly RunContext mContext;
        private readonly FeatureConfig mFeature;
        private readonly TaskConfig mTask;
        private readonly VariableReplacer mReplacer;

        public TaskExecutionContext(RunContext context, FeatureConfig feature, TaskConfig task, VariableReplacer replacer)
        {
            mContext = context;
            mFeature = feature;
            mTask = task;
            mReplacer = replacer;
        }

        public RunContext Context
        {
            get { return mContext; }
        }

        public FeatureConfig Feature
        {
            get { return mFeature; }
        }

        public TaskConfig Task
        {
            get { return mTask; }
        }

        public VariableReplacer Replacer
        {
            get { return mReplacer; }
        }

        public ILog Log
        {
            get { return mContext.ApplicationContext.Log; }
        }

        public bool DryRun
        {
            get { return mContext.DryRun; }
        }

        public string GetParameter(string name)
        {
            string value;
            return !mTask.Parameters.TryGetValue(name, out value) ? null : mReplacer.ReplaceVariables(value);
        }

        public string GetParameter(string name, Dictionary<string, string> additionalReplacements)
        {
            string value;
            return !mTask.Parameters.TryGetValue(name, out value) ? null : mReplacer.WithAdditionalReplacements(additionalReplacements).ReplaceVariables(value);
        }

        public bool AcceptsEnvironment(string value)
        {
            return mContext.ActiveEnvironment.Include.Contains(value);
        }
    }
}
