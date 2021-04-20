using System;

namespace ThreadPool
{
    internal class SimpleLockThreadPool : IThreadPool
    {
        public SimpleLockThreadPool(in int concurrency)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void EnqueueAction(Action action)
        {
            throw new NotImplementedException();
        }
    }
}