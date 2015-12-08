using DBBranchManager.Dependencies;
using System.Collections.Generic;

namespace DBBranchManager.Components
{
    internal class SuperComponent : IComponent
    {
        private readonly IDependencyGraph<IComponent> mComponents;

        public SuperComponent(IDependencyGraph<IComponent> components)
        {
            mComponents = components;
        }

        public IEnumerable<string> Run(ComponentRunState runState)
        {
            if (mComponents == null)
                yield break;

            foreach (var component in mComponents.GetPath())
            {
                foreach (var log in component.Run(runState))
                {
                    yield return log;

                    if (runState.Error)
                        yield break;
                }
            }
        }
    }
}