using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace DBBranchManager.Utils
{
    internal class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> mQueue;

        public SingleThreadSynchronizationContext()
        {
            mQueue = new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            mQueue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
        }

        public void Run()
        {
            KeyValuePair<SendOrPostCallback, object> work;
            while (mQueue.TryTake(out work, Timeout.Infinite))
                work.Key(work.Value);
        }

        public void Complete()
        {
            mQueue.CompleteAdding();
        }
    }
}