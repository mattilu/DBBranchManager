using System.Collections.Generic;

namespace DBBranchManager.Components
{
    internal class ComponentRunState
    {
        public ComponentRunState(bool dryRun)
        {
            DryRun = dryRun;
        }

        public bool DryRun { get; private set; }
        public bool Error { get; set; }
    }

    internal interface IComponent
    {
        IEnumerable<string> Run(ComponentRunState runState);
    }
}