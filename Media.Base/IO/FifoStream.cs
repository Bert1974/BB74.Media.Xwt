using System;
using System.Collections;
using System.IO;
using System.Threading;

namespace BaseLib
{
    namespace IO
    {
        public class FifoStream : Stream
        {
            private const int BlockSize = 1024 * 64;
            //    private const int MaxBlocksInCache = 25;

            private int m_Size;
            private int m_RPos;
            private int m_WPos;
            //      private Stack m_UsedBlocks = new Stack();
            private ArrayList m_Blocks = new ArrayList();

            public ManualResetEvent DataReady { get; private set; } = new ManualResetEvent(false);
            public ManualResetEvent WriteReady { get; private set; } = new ManualResetEvent(true);
            public ManualResetEvent IsEmpty { get; private set; } = new ManualResetEvent(true);
            private ManualResetEvent readclosevent = new ManualResetEvent(false), writecloseevent = new ManualResetEvent(false);
            private int toread;
            private int towrite;
            public ManualResetEvent EOS { get; private set; } = new ManualResetEvent(false);

            public int MaxLength { get; private set; }

            public FifoStream()
            {
                this.MaxLength = BlockSize * 10;
                this.ReadTimeout = this.WriteTimeout = -1;
            }
            public FifoStream(int bufsize)
                : this()
            {
                this.MaxLength = bufsize;
            }

            private byte[] AllocBlock()
            {
                byte[] Result = null;
                Result = new byte[BlockSize];
                return Result;
            }
            private void FreeBlock(byte[] block)
            {
            }
            private byte[] GetWBlock()
            {
                byte[] Result = null;
                if (m_WPos < BlockSize && m_Blocks.Count > 0)
                    Result = (byte[])m_Blocks[m_Blocks.Count - 1];
                else
                {
                    Result = AllocBlock();
                    m_Blocks.Add(Result);
                    m_WPos = 0;
                }
                return Result;
            }

            // Stream members
            public override bool CanRead
            {
                get { return true; }
            }
            public override bool CanSeek
            {
                get { return false; }
            }
            public override bool CanWrite
            {
                get { return true; }
            }
            public override bool CanTimeout => true;
            public override int ReadTimeout { get; set; } = -1;
            public override int WriteTimeout { get; set; } = -1;

            public override long Length
            {
                get
                {
                    lock (this)
                    {
                        return m_Size;
                    }
                }
            }
            public override long Position
            {
                get { throw new InvalidOperationException(); }
                set { throw new InvalidOperationException(); }
            }

            public string Name { get; set; } = "fifo";

            public void Open()
            {
                lock (this)
                {
                    //System.Diagnostics.Trace.WriteLine($"{Name} Open");
                    readclosevent.Reset();
                    writecloseevent.Reset();
                }
            }
            public override void Close()
            {
                lock (this)
                {
                    //System.Diagnostics.Trace.WriteLine($"{Name} Close");
                    Clear();
                    readclosevent.Set();
                    writecloseevent.Set();
                    EOS.Set();
                }
            }
            public void Clear()
            {
                lock (this)
                {
                    IsEmpty.Set();
                    DataReady.Reset();
                    WriteReady.Set();
                    readclosevent.Reset();
                    writecloseevent.Reset();
                    foreach (byte[] block in m_Blocks)
                        FreeBlock(block);
                    m_Blocks.Clear();
                    m_RPos = 0;
                    m_WPos = 0;
                    m_Size = 0;
                    EOS.Reset();
                }
            }
            public void WaitAllReadDone()
            {
                //System.Diagnostics.Trace.WriteLine($"{Name} WaitAllWriteDone1");
                this.IsEmpty.WaitOne(-1, false);
                //System.Diagnostics.Trace.WriteLine($"{Name} WaitAllWriteDone2");

                this.writecloseevent.Reset();
            }

            public override void Flush()
            {
                //System.Diagnostics.Trace.WriteLine($"{Name} Flush");
                lock (this)
                {
                    this.writecloseevent.Set();
                }
                //    lock (this)
                {
                    //    Trace.WriteLine("Flush/Trash");
                    //    writeready.Set();
                    /*     dataready.Reset();
                           foreach (byte[] block in m_Blocks)
                               FreeBlock(block);
                           m_Blocks.Clear();
                           m_RPos = 0;
                           m_WPos = 0;
                           m_Size = 0;*/
                }
            }

            public void CloseRead()
            {
                this.readclosevent.Set();
            }

            public override void SetLength(long len)
            {
                throw new InvalidOperationException();
            }

            public void OpenRead()
            {
                this.readclosevent.Reset();
            }


            public void CloseWrite()
            {
                this.writecloseevent.Set();
            }

            public void OpenWrite()
            {
                this.writecloseevent.Reset();
            }
            public override long Seek(long pos, SeekOrigin o)
            {
                throw new InvalidOperationException();
            }
            public override int Read(byte[] buf, int ofs, int count)
            {
                int Result = 0;
                while (count > 0)
                {
                    int n = WaitHandle.WaitAny(new WaitHandle[] { this.DataReady, this.EOS, this.readclosevent }, this.ReadTimeout, false);
                    if (n != 0 && n != 1)
                    {
                        //System.Diagnostics.Trace.WriteLine($"{Name} read, closed {Result}");
                        lock (this)
                        {
                         //   if (m_Size == 0) // EOS? .. recording needs real flush
                            {
                                this.toread = 0;
                                return Result;
                            }
                        }
                    }
                    lock (this)
                    {
                        if (this.EOS.WaitOne(0, false) && m_Size == 0)
                        {
                            return Result;
                        }
                        var total = Peek(buf, Result + ofs, count);
                        Advance(total);
                        Result += total; count -= total;
                        this.toread = count;
                    }
                }
                //System.Diagnostics.Trace.WriteLine($"{Name} Read {Result}");
                return Result;
            }

            public override void Write(byte[] buf, int ofs, int count)
            {
                try
                {
                    int Left = count;
                    while (Left > 0)
                    {
                        switch (WaitHandle.WaitAny(new WaitHandle[] { this.WriteReady, this.writecloseevent }, this.WriteTimeout, false))
                        {
                            case 0:
                                break;
                            case 1:
                                //System.Diagnostics.Trace.WriteLine($"{Name} Write closed");
                                this.towrite = 0;
                                throw new EndOfStreamException();
                            default:
                                this.towrite = 0;
                                throw new TimeoutException();
                        }
                        lock (this)
                        {
                            int ToWrite = Math.Min(BlockSize - m_WPos, Left);
                            var block = GetWBlock();
                            ToWrite = Math.Min(BlockSize - m_WPos, Left);
                            System.Array.Copy(buf, ofs + count - Left, block, m_WPos, ToWrite);
                            m_WPos += ToWrite;
                            Left -= ToWrite;

                            m_Size += ToWrite;

                            this.towrite = Left;

                            IsEmpty.Reset();

                            if (m_Size >= this.MaxLength)
                            {
                                //      Trace.WriteLine("Write, buffer full, writeready.reset()");
                                this.WriteReady.Reset();
                            }
                         /*   if (Length >= this.toread)
                            {
                                //    Trace.WriteLine("Write, readready, dataready.set()");
                                DataReady.Set();
                            }*/
                        }
                    }
                }
                finally
                {
               //     var doset = false;
                    lock (this)
                    {
                        if (Length >= this.toread)
                        {
                   //         doset = true;
                            //    Trace.WriteLine("Write, readready, dataready.set()");
                            DataReady.Set();
                        }
                    }
                }
            }
            // extra stuff
            public int Advance(int count)
            {
                lock (this)
                {
                    int SizeLeft = count;
                    while (SizeLeft > 0 && m_Size > 0)
                    {
                        if (m_RPos == BlockSize)
                        {
                            m_RPos = 0;
                            FreeBlock((byte[])m_Blocks[0]);
                            m_Blocks.RemoveAt(0);
                        }
                        int ToFeed = m_Blocks.Count == 1 ? Math.Min(m_WPos - m_RPos, SizeLeft) : Math.Min(BlockSize - m_RPos, SizeLeft);
                        m_RPos += ToFeed;
                        SizeLeft -= ToFeed;
                        m_Size -= ToFeed;
                    }
                    if (m_Size == 0)
                    {
                        //System.Diagnostics.Trace.WriteLine($"{Name} buffer empty");
                        this.IsEmpty.Set();
                        this.DataReady.Reset();
                    }
                    if (MaxLength - m_Size >= this.towrite)
                    {
                        //Trace.WriteLine("Advance, writeready.set()");
                        WriteReady.Set();
                    }
                    return count - SizeLeft;
                }
            }
            public int Peek(byte[] buf, int ofs, int count)
            {
                lock (this)
                {
                    int SizeLeft = count;
                    int TempBlockPos = m_RPos;
                    int TempSize = m_Size;

                    int CurrentBlock = 0;
                    while (SizeLeft > 0 && TempSize > 0)
                    {
                        var check = false;
                        if (TempBlockPos == BlockSize)
                        {
                            TempBlockPos = 0;
                            CurrentBlock++;
                            if (CurrentBlock >= m_Blocks.Count)
                            {
                                break;
                            }
                        }
                        else
                        {
                            check = true;
                        }
                        int Upper = CurrentBlock < m_Blocks.Count - 1 ? BlockSize : m_WPos;
                        int ToFeed = Math.Min(Upper - TempBlockPos, SizeLeft);
                        System.Array.Copy((byte[])m_Blocks[CurrentBlock], TempBlockPos, buf, ofs + count - SizeLeft, ToFeed);
                        SizeLeft -= ToFeed;
                        TempBlockPos += ToFeed;
                        TempSize -= ToFeed;

                        if ((check && TempBlockPos != Upper) && ToFeed == 0) { break; }
                    }
                    return count - SizeLeft;
                }
            }
            protected override void Dispose(bool disposing)
            {
                Close();
                base.Dispose(disposing);
            }

            public void SetEOS()
            {
                this.EOS.Set();
            }
        }
    }
}