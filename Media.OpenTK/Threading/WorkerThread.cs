using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BaseLib.Threading
{
    public abstract class WorkerThread : IDisposable
    {
        public ManualResetEvent stopevent = new ManualResetEvent(false);
        protected ManualResetEvent running = new ManualResetEvent(false);
        protected AutoResetEvent dopause = new AutoResetEvent(false), paused = new AutoResetEvent(false), quited = new AutoResetEvent(false);
        protected Thread thread;

        public WorkerThread(string threadname)
        {
            this.thread = new Thread(this.threadrun) { Name = threadname ?? "worker", IsBackground = true };
            this.thread.Start();
        }
        ~WorkerThread()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            this.stopevent.Set();
            this.quited.WaitOne(-1, false);
        }
        protected virtual void threadrun()
        {
            WaitHandle[] wh = { this.stopevent, this.running, this.dopause };

            try
            {
                while (!this.stopevent.WaitOne(0, false))
                {
                    int n = WaitHandle.WaitAny(wh, -1, false);

                    if (n == 2 || this.dopause.WaitOne(0, false))
                    {
                        this.running.Reset();
                        this.paused.Set();
                    }
                    else if (this.running.WaitOne(0, false))
                    {
                        try
                        {
                            threadaction();
                        }
                        catch (Exception)
                        {
                            Debug.Assert(false);
                        }
                    }
                }
            }
            finally
            {
                this.quited.Set();
            }
        }
        protected abstract void threadaction();
        public virtual void Pause(bool wait)
        {
            this.dopause.Set();
            if (wait) { this.paused.WaitOne(-1, false); }
        }
        public virtual void WaitPaused()
        {
            this.paused.WaitOne(-1, false);
        }
        public virtual void Run()
        {
            this.running.Set();
        }
    }
}
