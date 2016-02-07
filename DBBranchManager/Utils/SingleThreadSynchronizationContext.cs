using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace DBBranchManager.Utils
{
    internal class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private enum Status
        {
            NotStarted,
            Running,
            Resetting,
            Complete,
            Error
        }

        private BlockingCollection<KeyValuePair<SendOrPostCallback, object>> mQueue;
        private Status mStatus;

        public SingleThreadSynchronizationContext()
        {
            mQueue = new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();
            mStatus = Status.NotStarted;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (mStatus != Status.NotStarted && mStatus != Status.Running)
                throw new InvalidOperationException(string.Format("Cannot Post() in state {0}.", mStatus));

            mQueue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
        }

        public void Run()
        {
            if (mStatus != Status.NotStarted)
                throw new InvalidOperationException(string.Format("Cannot Run() in state {0}.", mStatus));
            mStatus = Status.Running;

            try
            {
                KeyValuePair<SendOrPostCallback, object> work;
                while (mQueue.TryTake(out work, Timeout.Infinite))
                {
                    if (mStatus == Status.Running)
                        work.Key(work.Value);
                }

                if (mStatus == Status.Resetting)
                {
                    mStatus = Status.NotStarted;
                    mQueue = new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();
                }
                else
                {
                    mStatus = Status.Complete;
                }
            }
            catch
            {
                mStatus = Status.Error;
                throw;
            }
        }

        public void Complete()
        {
            mQueue.CompleteAdding();
        }

        public void Reset()
        {
            if (mStatus == Status.NotStarted || mStatus == Status.Resetting)
                throw new InvalidOperationException(string.Format("Cannot Reset() in state {0}", mStatus));

            if (mStatus == Status.Running)
            {
                if (!mQueue.IsAddingCompleted)
                    mQueue.CompleteAdding();
                mStatus = Status.Resetting;
            }
            else
            {
                if (mStatus == Status.Error)
                    mQueue = new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();
                mStatus = Status.NotStarted;
            }
        }
    }
}