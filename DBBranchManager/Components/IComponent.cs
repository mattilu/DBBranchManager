using System.Collections.Generic;

namespace DBBranchManager.Components
{
    internal class ComponentRunContext
    {
        private bool mError;
        private int mDepth;

        public ComponentRunContext(bool dryRun, string environment)
        {
            DryRun = dryRun;
            Environment = environment;
            mDepth = 0;
        }

        public bool DryRun { get; private set; }
        public string Environment { get; private set; }

        public bool Error
        {
            get { return mError; }
        }

        public int Depth
        {
            get { return mDepth; }
        }

        public void SetError()
        {
            mError = true;
        }

        public void IncreaseDepth()
        {
            ++mDepth;
        }

        public void DecreaseDepth()
        {
            --mDepth;
        }
    }

    internal interface IComponent
    {
        IEnumerable<string> Run(string action, ComponentRunContext runContext);
    }
}