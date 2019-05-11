using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace BaseLib.Threading
{
    public class WriteLock : IDisposable
    {
        ReaderWriterLock readerwriterlock;
        public WriteLock(ReaderWriterLock readerwriterlock)
        {
            this.readerwriterlock = readerwriterlock;
            this.readerwriterlock.AcquireWriterLock(-1);
        }
        ~WriteLock()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            this.readerwriterlock.ReleaseWriterLock();
        }
    }

    public class ReadLock : IDisposable
    {
        ReaderWriterLock readerwriterlock;
        public ReadLock(ReaderWriterLock readerwriterlock)
        {
            this.readerwriterlock = readerwriterlock;
            this.readerwriterlock.AcquireReaderLock(-1);
        }
        ~ReadLock()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.readerwriterlock.ReleaseReaderLock();
            }
        }
    }
}