using System.Collections.Generic;

namespace DBBranchManager.Entities.Config
{
    internal class ReleaseConfig
    {
        private readonly string mName;
        private readonly string mBaseline;
        private readonly List<string> mFeatures;

        public ReleaseConfig(string name, string baseline, List<string> features)
        {
            mName = name;
            mBaseline = baseline;
            mFeatures = features;
        }

        public string Name
        {
            get { return mName; }
        }

        public string Baseline
        {
            get { return mBaseline; }
        }

        public List<string> Features
        {
            get { return mFeatures; }
        }
    }
}