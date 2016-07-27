using System;

namespace DBBranchManager.Entities.Config
{
    internal class CacheConfig
    {
        private readonly string mRootPath;
        private readonly TimeSpan mMinDeployTime;
        private readonly bool mDisabled;

        public CacheConfig(string rootPath, TimeSpan minDeployTime, bool disabled)
        {
            mRootPath = rootPath;
            mMinDeployTime = minDeployTime;
            mDisabled = disabled;
        }

        public string RootPath
        {
            get { return mRootPath; }
        }

        public TimeSpan MinDeployTime
        {
            get { return mMinDeployTime; }
        }

        public bool Disabled
        {
            get { return mDisabled; }
        }
    }
}
