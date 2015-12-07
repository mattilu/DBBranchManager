using DBBranchManager.Components;
using DBBranchManager.Utils;
using System.Collections.Generic;
using System.Linq;

namespace DBBranchManager.Invalidators
{
    internal class FileSystemWatcherInvalidator : IInvalidator
    {
        private readonly EnhanchedFileSystemWatcher mWatcher;

        public FileSystemWatcherInvalidator()
        {
            mWatcher = new EnhanchedFileSystemWatcher();
            mWatcher.Changed += OnChanged;
        }

        public event InvalidatedEventHandler Invalidated;

        public void AddWatch(string path, IComponent component)
        {
            mWatcher.AddWatch(path, component);
        }

        public void AddWatch(string path, string filter, IComponent component)
        {
            mWatcher.AddWatch(path, filter, component);
        }

        private void OnChanged(object sender, EnhanchedFileSystemWatcherChangeEventArgs e)
        {
            OnInvalidate(e.AffectedItems.Cast<IComponent>().ToList());
        }

        private void OnInvalidate(List<IComponent> components)
        {
            var evt = Invalidated;
            if (evt != null)
                evt(this, new InvalidatedEventsArgs(new List<IComponent>(components)));
        }
    }
}