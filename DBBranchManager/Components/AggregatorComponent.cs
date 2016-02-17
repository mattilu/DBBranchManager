using System.Collections.Generic;
using DBBranchManager.Utils;

namespace DBBranchManager.Components
{
    internal abstract class AggregatorComponent : IComponent
    {
        public IEnumerable<string> Run(string action, ComponentRunContext runContext)
        {
            using (runContext.DepthScope())
            {
                foreach (var log in GetComponentsToRun(action, runContext).Run(action, runContext))
                {
                    yield return log;
                }
            }
        }

        protected abstract IEnumerable<IComponent> GetComponentsToRun(string action, ComponentRunContext runContext);
    }
}