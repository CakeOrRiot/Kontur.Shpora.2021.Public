using System;
using System.Collections.Generic;
using System.Threading;

namespace ReaderWriterLock
{
    public class RwLock : IRwLock
    {
        private int readersCount;
        private readonly object readerLock = new();
        private readonly object writerLock = new();

        public void ReadLocked(Action action)
        {
            lock (readerLock)
                readersCount++;

            action.Invoke();
            lock (readerLock)
            {
                readersCount--;
                if (readersCount == 0)
                    Monitor.Pulse(readerLock);
            }
        }

        public void WriteLocked(Action action)
        {
            lock (writerLock)
            {
                lock (readerLock)
                {
                    while (readersCount > 0)
                        Monitor.Wait(readerLock);
                    action.Invoke();
                }
            }
        }
    }
}