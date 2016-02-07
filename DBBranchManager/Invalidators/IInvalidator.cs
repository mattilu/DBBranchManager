using System;
using System.Collections.Generic;
using DBBranchManager.Components;

namespace DBBranchManager.Invalidators
{
    internal delegate void InvalidatedEventHandler(object sender, InvalidatedEventsArgs args);

    internal class InvalidatedEventsArgs : EventArgs
    {
        public InvalidatedEventsArgs(string reason, IReadOnlyCollection<IComponent> invalidatedComponents)
        {
            Reason = reason;
            InvalidatedComponents = invalidatedComponents;
        }

        public object Reason { get; private set; }

        public IReadOnlyCollection<IComponent> InvalidatedComponents { get; private set; }
    }

    internal interface IInvalidator : IDisposable
    {
        event InvalidatedEventHandler Invalidated;
    }
}