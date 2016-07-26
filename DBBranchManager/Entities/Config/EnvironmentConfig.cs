using System.Collections.Generic;

namespace DBBranchManager.Entities.Config
{
    internal class EnvironmentConfig
    {
        private readonly string mName;
        private readonly string mDescription;
        private readonly HashSet<string> mInclude;

        public EnvironmentConfig(string name, string description, IEnumerable<string> include)
        {
            mName = name;
            mDescription = description;
            mInclude = new HashSet<string>(include);
        }

        public string Name
        {
            get { return mName; }
        }

        public string Description
        {
            get { return mDescription; }
        }

        public ICollection<string> Include
        {
            get { return mInclude; }
        }
    }
}