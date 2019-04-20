using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BaseLib.Threading
{
    public sealed class ReaderWriterLockNoThreading : IDisposable
    {
        int lockreadcnt = 0;
        object locklock = new object();
        bool waitforwrite;
        ManualResetEvent writelocked = new ManualResetEvent(false);
        ManualResetEvent readlocked = new ManualResetEvent(false);
        ManualResetEvent readready = new ManualResetEvent(true);
        List<ManualResetEvent> writelockpending = new List<ManualResetEvent>();

        ~ReaderWriterLockNoThreading()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        void Dispose(bool disposing)
        {
            this.writelocked.Dispose();
            this.readlocked.Dispose();
            this.readready.Dispose();
        }
        public void LockWrite()
        {
            ManualResetEvent waitfor = null;

            lock (locklock)
            {
                waitfor = new ManualResetEvent(false);
                writelockpending.Add(waitfor);

                if (!writelocked.WaitOne(0, false))
                {
                    if (!readlocked.WaitOne(0, false))
                    {
                        readready.Reset();
                        writelocked.Set();
                        writelockpending[0].Set();
                        writelockpending.RemoveAt(0);
                    }
                    else
                    {
                        waitforwrite = true;
                    }
                }
            }
            waitfor.WaitOne(-1, false);
        }
        public void UnlockWrite()
        {
            lock (locklock)
            {
                if (writelockpending.Count == 0)
                {
                    this.writelocked.Reset();
                    this.readready.Set();
                }
                else
                {
                    writelockpending[0].Set();
                    writelockpending.RemoveAt(0);
                }
            }
        }
        public void Lock()
        {
            while (true)
            {
                this.readready.WaitOne(-1, false);
                lock (locklock)
                {
                    if (!writelocked.WaitOne(0, false))
                    {
                        lockreadcnt++;
                        readlocked.Set();
                        return;
                    }
                }
            }
        }
        public void Unlock()
        {
            lock (locklock)
            {
                if (--lockreadcnt == 0)
                {
                    readlocked.Reset();

                    if (waitforwrite)
                    {
                        waitforwrite = false;
                        readready.Reset();
                        writelocked.Set();
                        writelockpending[0].Set();
                        writelockpending.RemoveAt(0);
                    }
                }
            }
        }
    }
}
