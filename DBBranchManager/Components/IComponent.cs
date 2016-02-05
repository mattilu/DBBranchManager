using System.Collections.Generic;

namespace DBBranchManager.Components
{
    internal class ComponentRunState
    {
        public ComponentRunState(bool dryRun, string environment)
        {
            DryRun = dryRun;
            Environment = environment;
        }

        public bool DryRun { get; private set; }
        public string Environment { get; private set; }
        public bool Error { get; set; }
    }

    internal interface IComponent
    {
        IEnumerable<string> Run(ComponentRunState runState);
    }
}