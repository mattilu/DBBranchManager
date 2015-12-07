using DBBranchManager.Components;
using System;
using System.Collections.Generic;

namespace DBBranchManager.Invalidators
{
    internal delegate void InvalidatedEventHandler(object sender, InvalidatedEventsArgs args);

    internal class InvalidatedEventsArgs : EventArgs
    {
        public InvalidatedEventsArgs(IReadOnlyCollection<IComponent> invalidatedComponents)
        {
            InvalidatedComponents = invalidatedComponents;
        }

        public IReadOnlyCollection<IComponent> InvalidatedComponents { get; private set; }
    }

    internal interface IInvalidator
    {
        event InvalidatedEventHandler Invalidated;
    }
}