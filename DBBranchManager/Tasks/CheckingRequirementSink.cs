using System;
using DBBranchManager.Logging;

namespace DBBranchManager.Tasks
{
    internal class CheckingRequirementSink : IRequirementSink
    {
        private readonly ILog mLog;
        private bool mGotErrors;
        private string mLastGroup;

        public CheckingRequirementSink(ILog log)
        {
            mLog = log;
        }

        public void Add(string group, Func<Tuple<bool, string>> checker)
        {
            var result = checker();
            if (!result.Item1)
            {
                EnsureHeader();
                SetGroup(group);
                mLog.Log(result.Item2);
            }
        }

        private void SetGroup(string group)
        {
            if (mLastGroup == group)
                return;

            if (mLastGroup != null)
                mLog.UnIndent();

            mLog.LogFormat("In {0}:", group);
            mLog.Indent();

            mLastGroup = group;
        }

        private void EnsureHeader()
        {
            if (mGotErrors)
                return;

            mLog.Log("The following requirements were not met:");
            mLog.Indent();

            mGotErrors = true;
        }

        public bool Finish()
        {
            if (mGotErrors)
            {
                mLog.UnIndent();
                mLog.UnIndent();
            }

            return mGotErrors;
        }
    }
}
