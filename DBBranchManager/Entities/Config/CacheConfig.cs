using System;

namespace DBBranchManager.Entities.Config
{
    internal class CacheConfig
    {
        private readonly string mRootPath;
        private readonly TimeSpan mMinDeployTime;
        private readonly long mMaxCacheSize;
        private readonly bool mAutoGC;
        private readonly bool mDisabled;

        public CacheConfig(string rootPath, TimeSpan minDeployTime, long maxCacheSize, bool autoGC, bool disabled)
        {
            mRootPath = rootPath;
            mMinDeployTime = minDeployTime;
            mMaxCacheSize = maxCacheSize;
            mAutoGC = autoGC;
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

        public long MaxCacheSize
        {
            get { return mMaxCacheSize; }
        }

        public bool AutoGC
        {
            get { return mAutoGC; }
        }

        public bool Disabled
        {
            get { return mDisabled; }
        }
    }
}
