using System.Collections.Generic;

namespace DBBranchManager.Components
{
    internal class ReleaseComponent : IComponent
    {
        private readonly string mReleaseName;
        private readonly string mState;

        public ReleaseComponent(string releaseName, string state)
        {
            mReleaseName = releaseName;
            mState = state;
        }

        public IEnumerable<string> Run(ComponentRunState runState)
        {
            yield return string.Format("Release {0}: {1}", mReleaseName, mState);
        }
    }
}