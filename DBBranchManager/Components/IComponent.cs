using System.Collections.Generic;

namespace DBBranchManager.Components
{
    internal class ComponentState
    {
        public bool Error { get; set; }
    }

    internal interface IComponent
    {
        IEnumerable<string> Run(ComponentState componentState);
    }
}