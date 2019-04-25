using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using BaseLib.Xwt;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Xwt;

namespace DockExample.OpenTK
{
    class XwtRender : IWxtDisplay, IRenderOwner
    {
        class VideoRun
        {
            private XwtRender owner;
            private Thread thread;
            private ManualResetEvent stopevent = new ManualResetEvent(false), stoppedevent = new ManualResetEvent(false);

            public VideoRun(XwtRender owner)
            {
                this.owner = owner;
                this.thread = new Thread(this.main) { Name = "videorender" };
                this.thread.Start();
            }
            private void main()
            {
                while (true)
                {
                    int waitres = WaitHandle.WaitAny(new WaitHandle[] { this.stopevent }, -1, false);

                    if (this.stopevent.WaitOne(0, false))
                    {
                        this.stoppedevent.Set();
                        return;
                    }
                }
            }

            public void Dispose()
            {
                this.stopevent.Set();
                this.stoppedevent.WaitOne(-1, false);
            }
        }

        private Canvas window;
        private IXwtRender opentkxwt;
        private IXwt xwt;
        private readonly Canvas target;
        private VideoRun videorun;
        // private shader presentshader;
        static int test = 0;
        //   private BBR.main.IWxtRenderer renderer;
        private DisplayStates state = DisplayStates.Stopped;
        private long time = 0L, TimeBase;

        public IRenderer Renderer { get; set; }

        //    private AutoResetEvent pauseevent = new AutoResetEvent(false), pausedevent = new AutoResetEvent(false);
        //     private ManualResetEvent running = new ManualResetEvent(false);
        private ManualResetEvent readyevent = new ManualResetEvent(false), stopevent = new ManualResetEvent(false);

        private IWxtRenderer renderer;
        private long displaytime;
        private IRenderFrame presentframe;

        public IWxtRenderer FrameRenderer
        {
            get => this.renderer;
            set
            {
                if (!object.ReferenceEquals(this.renderer, value))
                {
                    if (this.renderer != null)
                    {
                        this.renderer.Dispose();
                        this.renderer = null;
                    }
                    if ((this.renderer = value) != null)
                    {
                        InitRenderer();
                    }
                }
            }
        }
        public DisplayStates State => this.state;

        public long Time => throw new NotImplementedException();

        public IRendererFactory RenderFactory { get; private set; }

        public XwtRender(Canvas target, IXwtRender xwtrender, IXwt xwt, long timebase)
        {
            this.TimeBase = timebase;
            this.window = target;
            this.opentkxwt = xwtrender;
            this.xwt = xwt;
            this.target = target;

            //    this.opentkxwt.CreateForWidgetContext(this, this.window);
        }
        ~XwtRender()
        {
            Debug.Assert(false);
        }
        public void Dispose()
        {
            this.videorun?.Dispose();
            this.videorun = null;

            this.Renderer?.Stop();

            this.FrameRenderer?.Dispose();
            this.FrameRenderer = null;

            this.Renderer?.Dispose();
            // this.opentkxwt.FreeWindowInfo(this.window);

            GC.SuppressFinalize(this);
        }
        void IRenderOwner.DoEvents()
        {
            this.xwt.DoEvents();
        }
        bool IRenderOwner.preparerender(IRenderFrame desitination, bool dowait)
        {
            if (WaitHandle.WaitAny(new WaitHandle[] { this.readyevent, this.stopevent }, dowait ? -1 : 0, false) == 0)
            {
                long time = BaseLib.Time.FromTicks(DateTime.Now.Ticks - this.displaytime, TimeBase);
                this.presentframe = this.FrameRenderer.GetFrame(time, true);
                return this.presentframe != null;
            }
            return false;
        }
    /*    void IRenderOwner.SkipRender(long ticks)
        {
            this.displaytime += BBR.Base.Time.FromTicks(ticks, TimeBase);
        }*/
        void IRenderOwner.render(IRenderFrame desitination, Xwt.Rectangle r)
        {
            if (this.presentframe != null)
            {
                this.Renderer.Present(this.presentframe, r, IntPtr.Zero);
                this.FrameRenderer.FrameDone(this.presentframe);
                this.presentframe = null;
            }
        }
        public void Initialize(IRendererFactory factory, IXwtRender xwt, size videosize)
        {
            Debug.Assert(this.renderer != null);

            //if (System.Environment.OSVersion.Platform == PlatformID.Unix)
            {
                //  this.window.ge

                //       SwapChainBackgroundPanel

                this.RenderFactory = factory;
                this.Renderer = factory.Open(xwt, this.window, this, videosize);
            }
            this.videorun = new VideoRun(this);
            this.renderer.Initialize(videosize, this.TimeBase);

            this.Renderer.Start();

         //   this.renderer.Play(0);

        }private void InitRenderer()
        {
            Debug.Assert(this.renderer != null);
            this.renderer.Display = this;
        }
        public void Pause()
        {
            Pause(this.time);
        }
        public void Pause(long time)
        {
            switch (this.state)
            {
                case DisplayStates.Running:
                    Stop();
                    goto case DisplayStates.Stopped;

                case DisplayStates.Paused:
                    if (this.time != time)
                    {
                        Stop();

                        Pause(time);
                        goto case DisplayStates.Stopped;
                    }
                    break;

                case DisplayStates.Stopped:
                    this.time = time;
                    this.state = DisplayStates.Paused;
                    this.renderer.Pause(this.time);

                    //    this.running.Set();
                    break;
            }
        }
        public void Play(long time)
        {
            switch (this.state)
            {
                case DisplayStates.Running:
                case DisplayStates.Paused:
                    Stop();
                    goto case DisplayStates.Stopped;

                case DisplayStates.Stopped:
                    this.time = time;
                    this.state = DisplayStates.Running;
                    this.renderer.Play(this.time);
                    this.displaytime = DateTime.Now.Ticks - BaseLib.Time.ToTick(time, TimeBase);
                    this.readyevent.Set();
                    break;

            }
        }
        private void Stop()
        {
            //  this.pauseevent.Set();
            //   this.pausedevent.WaitOne(-1, false);

            this.stopevent.Set();

            this.readyevent.Reset();

            this.renderer.Stop();
            this.state = DisplayStates.Stopped;

            this.readyevent.Reset();
            this.stopevent.Reset();
        }

        public void StartRender(IRenderer renderer) // called by OpenTk/SharpDX-BaseLib.Display.IRenderer
        {
        //    Monitor.Enter(this);
            this.opentkxwt.StartRender(renderer, this.window);
        }

        public void EndRender(IRenderer renderer) // ^^
        {
            this.opentkxwt.EndRender(renderer, this.window);
        //    Monitor.Exit(this);
        }

        /*    public void Lock()
            {
                this.FrameRenderer.Lock();
            }
            public void Unlock()
            {
                this.FrameRenderer.Unlock();
            }*/
#if (false)
        public IRenderFrame GetRenderFrame()
        {
            return new RenderFrame(this.renderer as BaseLib.Display.IRenderer, 1);
        }
        public object StartRender(IRenderFrame destination)
        {
            var frame = (RenderFrame)destination;

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frame.framebuffer);

            GL.Viewport(0, 0, frame.Width, frame.Height);// new Rectangle(this.window.Location,this.window.ClientSize));

      //      GL.ClearColor(1, 1, 0, 1);
      //      GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit); // We're not using stencil buffer so why bother with clearing?            

            GL.Disable(EnableCap.DepthTest);
            //     GL.Disable(EnableCap.Lighting);

            //          ES30.GL.Enable(OpenTK.Graphics.ES30.EnableCap.DepthTest);
            //        ES30.GL.Enable(ES30.EnableCap.Blend);
            //        ES30.GL.BlendFunc(ES30.BlendingFactorSrc.SrcAlpha, ES30.BlendingFactorDest.OneMinusSrcAlpha);

            GL.Disable(EnableCap.StencilTest);

            /* GL.UseProgram(shaderProgram);
             GL.BindBuffer(BufferTarget.ArrayBuffer, buf_vertices);

             GL.DrawArrays(PrimitiveType.Triangles, 0, 3);*/

            return null;
        }
        public void EndRender(object state)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            //    Unlock();
        }
#endif
    }
    public abstract class MovieRender : IWxtRenderer, IDisposable
    {
        public IWxtDisplay /*IRenderer.*/Display { get; set; }
        public size VideoSize { get; private set; }

        private ManualResetEvent stopevent = new ManualResetEvent(false), stoppedevent = new ManualResetEvent(false);
        private AutoResetEvent pauseevent = new AutoResetEvent(false), pausedevent = new AutoResetEvent(false);
        private ManualResetEvent runningevent = new ManualResetEvent(false), readyevent = new ManualResetEvent(false),  notemptyevent = new ManualResetEvent(false);


        private Thread thread;
        private long time, videotime, TimeBase;
        private bool paused;
        protected opentkdoc testdoc;

        private ConcurrentQueue<IRenderFrame> previewqueue = new ConcurrentQueue<IRenderFrame>(), framepool = new ConcurrentQueue<IRenderFrame>();
        private CancellationTokenSource cancelpreview = new CancellationTokenSource();

        public MovieRender(opentkdoc testdoc)
        {
            this.testdoc = testdoc;
        }
        ~MovieRender()
        {
            Debug.Assert(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            this.stopevent.Set();
            this.stoppedevent.WaitOne(-1, false);

            using (var lck = this.Display.Renderer.GetDrawLock())
            {
                while (previewqueue.TryDequeue(out IRenderFrame frame))
                {
                    frame.Dispose();
                }
                while (framepool.TryDequeue(out IRenderFrame frame))
                {
                    frame.Dispose();
                }
            }
            GC.SuppressFinalize(this);
        }
        public virtual void Initialize(size videosize, long timebase)
        {
            this.TimeBase = timebase;
            this.VideoSize = videosize;

            this.thread = new Thread(this.main) { Name = "renderthread" };
            this.thread.Start();

            switch (this.Display.State)
            {
                case DisplayStates.Stopped:
                    return;
                case DisplayStates.Paused:
                    Pause(this.Display.Time);
                    return;
                case DisplayStates.Running:
                    Play(this.Display.Time);
                    return;
            }
        }
        void main()
        {
            while (true)
            {
                int waitres = WaitHandle.WaitAny(new WaitHandle[] { this.stopevent, this.pauseevent, this.readyevent }, -1, false);

                if (waitres == 1 || this.pauseevent.WaitOne(0, false))
                {
                    this.pausedevent.Set();
                    continue;
                }
                else if (this.stopevent.WaitOne(0, false))
                {
                    this.stoppedevent.Set();
                    return;
                }
                else
                {
                    IRenderFrame frame = null;

                    Action action= ()=>
                    {
                        try
                        {
                            using (var lck = this.Display.Renderer.GetDrawLock())
                            {
                                if (!this.framepool.TryDequeue(out frame))
                                {
                                    // get frame
                                    frame = this.Display.Renderer.GetRenderFrame(1);
                                }
                                frame.Set(this.Display.Renderer.AlphaFormat);
                                frame.Set(this.videotime, this.VideoSize.width, this.VideoSize.height, 0);

                                Render(frame, this.videotime);

                                //   int width = this.VideoSize.width, height = this.VideoSize.height;
                                //   GL.Viewport(0, 0, width, height);

                                //   GL.ClearColor((test++ % 10) / 10f, 0, 0, 1);
                                //   GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

                                this.videotime += BaseLib.Time.FromTicks(400000L, this.TimeBase);
                            }
                        }
                        catch (Exception e)
                        {
                         //   Log.LogException(e);
                        }
                    };
                    try
                    {
                     /*  if (this.Display.RenderFactory.OpenGLInUIThread)
                        {
                            Application.InvokeAsync(action).Wait();
                        }
                        else*/
                        {
                            action();
                        }
                    }
                    catch { }
                    if (frame != null)
                    {
                        // queue

                        lock (this.previewqueue)
                        {
                            this.previewqueue.Enqueue(frame);

                            if (this.previewqueue.Count > 3)
                            {
                                this.readyevent.Reset();
                            }
                            this.notemptyevent.Set();
                        }
                    }
                    else
                    {
                   //    Debug.Assert(false);
                }
                }
            }
        }

        public abstract void Render(IRenderFrame frame, long videotime);

        /* public void SyncState(DisplayStates state)
{
switch (state)
{
case DisplayStates.Stopped:
return;
case DisplayStates.Paused:
{
}
return;
case DisplayStates.Running:
{
}
return;
}
throw new NotImplementedException();
}*/

        public virtual void Stop()
        {
            this.pauseevent.Set();
            this.pausedevent.WaitOne(-1, false);

            while (this.framepool.TryDequeue(out IRenderFrame frame))
            {
                frame.Dispose();
            }

            this.readyevent.Reset();
       //     this.runningevent.Reset();
        }

        public virtual void Pause(long time)
        {
            this.time = time;
            this.videotime = time;
            this.paused = true;
            this.readyevent.Set();

            //   this.runningevent.Set();
        }

        public virtual void Play(long time)
        {
            this.time = time;
            this.videotime = time;
            this.paused = false;
            this.readyevent.Set();

            //     this.runningevent.Set();
        }
        /*
        public void Lock()
        {
            try
            {
                if (this.lockthread == Thread.CurrentThread)
                {
                    this._lockcnt++;
                    return;
                }
                this.Display.StartRender();
                if (this._lockcnt++ == 0)
                {
                    this.lockthread = Thread.CurrentThread;
                }
            }
            catch (Exception e)
            {
                Application.Exit();
            }
        }

        public void Unlock()
        {
            try
            {
                if (--_lockcnt == 0)
                {
                    this.lockthread = null;
                    this.Display.EndRender();
                }
            }
            catch (Exception e)
            {
                Application.Exit();
            }
        }*/

        public IRenderFrame GetFrame(long time, bool dowait)
        {
            while (true)
            {
                while (this.previewqueue.Count > 0 && this.previewqueue.First().Time < time)
                {
                    lock (this.previewqueue)
                    {
                        this.previewqueue.TryDequeue(out IRenderFrame frame);
                        FrameDone(frame);

                        this.readyevent.Set();

                        if (this.previewqueue.Count == 0)
                        {
                            this.notemptyevent.Reset();
                        }
                    }
                }
                if (this.previewqueue.Count==0 && !dowait)
                {
                    return null;
                }
                //doevents?
                int n = WaitHandle.WaitAny(new WaitHandle[] { this.notemptyevent, this.stopevent }, -1, false);

                if (n == 1 || this.stopevent.WaitOne(0, false))
                {
                    return null;
                }
                if (this.previewqueue.Count > 0 && this.previewqueue.First().Time >= time)
                {
                    return this.previewqueue.FirstOrDefault();
                }
            }
        }


        public void FrameDone(IRenderFrame frame)
        {
            if (!object.ReferenceEquals(this.previewqueue.FirstOrDefault(), frame))
            {
                this.framepool.Enqueue(frame);
            }
        }       
    }

}
