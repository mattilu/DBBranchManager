using System;
using System.Collections.Generic;
using DBBranchManager.Utils;

namespace DBBranchManager.Components
{
    internal abstract class AggregatorComponent : IComponent
    {
        public IEnumerable<string> Run(string action, ComponentRunContext runContext)
        {
            using (new DepthScope(runContext))
            {
                foreach (var log in GetComponentsToRun(action, runContext).Run(action, runContext))
                {
                    yield return log;
                }
            }
        }

        protected abstract IEnumerable<IComponent> GetComponentsToRun(string action, ComponentRunContext runContext);


        private class DepthScope : IDisposable
        {
            private readonly ComponentRunContext mRunContext;

            public DepthScope(ComponentRunContext runContext)
            {
                mRunContext = runContext;
                runContext.IncreaseDepth();
            }

            public void Dispose()
            {
                mRunContext.DecreaseDepth();
            }
        }
    }
}