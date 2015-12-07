using System.Collections.Generic;

namespace DBBranchManager.Components
{
    internal class BranchComponent : IComponent
    {
        private readonly string mBranchName;
        private readonly string mState;

        public BranchComponent(string branchName, string state)
        {
            mBranchName = branchName;
            mState = state;
        }

        public IEnumerable<string> Run(ComponentState componentState)
        {
            yield return string.Format("Branch {0}: {1}", mBranchName, mState);
        }
    }
}