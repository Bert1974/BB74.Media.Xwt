using BaseLib.Media;
using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using BaseLib.Media.Video;
using BaseLib.Platforms;
using BaseLib.Threading;
using SharpDX;
using SharpDX.Direct3D9;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Xwt;
using Matrix = SharpDX.Matrix;
using Xwt = global::Xwt;

namespace BaseLib.Display.WPF
{
    using Xwt = global::Xwt;

    public interface IDirectXFrame
    {
        Texture[] Textures { get; }
    }
    internal class _rendererlock : IDisposable
    {
        private DirectX9Renderer renderer;

        public _rendererlock(IRenderer renderer)
        {
            this.renderer = renderer as DirectX9Renderer;
            this.renderer.Lock();
        }
        ~_rendererlock()
        {
            Debug.Assert(false);
        }
        public void Dispose()
        {
            this.renderer.Unlock();
            GC.SuppressFinalize(this);
        }
    }

    public class DirectX9Renderer : IRenderer
    {
        public bool ForceNoThreading => throw new NotImplementedException();
        public bool UseNoThreading => throw new NotImplementedException();

        public VideoFormat AlphaFormat => VideoFormat.ARGB;

        public class WorkerThread : IDisposable
        {
            public class actioninvoker
            {
                internal Action methodinvoker;
                internal Exception exception;
                public readonly ManualResetEvent Completed = new ManualResetEvent(false);

                public actioninvoker(Action m)
                {
                    this.methodinvoker = m;
                }
            }

            protected ManualResetEvent stopevent = new ManualResetEvent(false), doaction = new ManualResetEvent(false);
            protected AutoResetEvent quited = new AutoResetEvent(false);
            protected Thread thread;
            private BlockingCollection<actioninvoker> actions = new BlockingCollection<actioninvoker>();
            private CancellationTokenSource cancel = new CancellationTokenSource();

            public void Do(Action m)
            {
                var a = new actioninvoker(m);
                actions.Add(a);
                a.Completed.WaitOne(-1, false);

                if (a.exception != null)
                {
                    throw new Exception("invoke, exception thrown", a.exception);
                }
            }
            public WorkerThread()
            {
                this.thread = new Thread(this.threadrun) { Name = "directx9" };
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
                cancel.Cancel();
                this.stopevent.Set();
                this.quited.WaitOne(-1, false);
            }
            protected virtual void threadrun()
            {
                WaitHandle[] wh = { this.stopevent, this.doaction };

                try
                {
                    while (!this.stopevent.WaitOne(0, false))
                    {
                        actioninvoker action;

                        if (actions.TryTake(out action, -1, cancel.Token))
                        {
                            try
                            {
                                action.methodinvoker.Invoke();
                            }
                            catch (Exception e)
                            {
                                action.exception = e;
                            }
                            finally
                            {
                                action.Completed.Set();
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    this.quited.Set();
                }
            }
        }

        internal void CheckPos()
        {
      //      this._layer?.rec.Arrange(new Rect(0, 0, this.window.ActualWidth, this.window.ActualHeight));

        }

        public Matrix CreateViewMatrix(double w, double h)
        {
            Matrix worldMatrix = Matrix.Identity;
            Matrix view = Matrix.Identity;
            Matrix proj = Matrix.Translation((float)(-w - .5f) / 2f, (float)(-h - .5f) / 2f, 0) * Matrix.Scaling(2 / (float)w, -2 / (float)h, 1);
            return Matrix.Multiply(view, proj);
        }
        public Matrix CreateViewMatrix2(double w, double h)
        {
            // Prepare matrices

            Matrix worldMatrix = Matrix.Identity;
            var view = Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
            var proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, (float)w / (float)h, 0.1f, 100.0f);
            return Matrix.Multiply(view, proj);
        }
        [StructLayout(LayoutKind.Explicit, Size = 4 * 6, CharSet = CharSet.Ansi)]
        public struct vertex
        {
            [FieldOffset(0)] Vector4 pos;
            [FieldOffset(4 * 4)] Vector2 tex0;

            public vertex(float x, float y, float tx0, float ty0)
            {
                this.pos = new Vector4(x, y, 0, 1);
                this.tex0 = new Vector2(tx0, ty0);
            }

            public const int SizeOf = sizeof(float) * 6;
        }
        // Allocate Vertex Elements
        VertexElement[] vertexElems2 = new[] {
                new VertexElement(0, 0, DeclarationType.Float4, DeclarationMethod.Default, DeclarationUsage.Position, 0),
                new VertexElement(0, 16, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                VertexElement.VertexDeclarationEnd
        };
        vertex[] initdata = {new vertex(0,0,0,0),new vertex(1,0,1,0),new vertex(0,1,0,1),
                             new vertex(1,0,1,0),new vertex(1,1,1,1),new vertex(0,1,0,1)};

        int[] initdata2 = { 0, 1, 1, 4, 4, 2, 2, 0 };

        private Direct3D direct3D;
        private bool isex = false;

        public Device device { get; private set; }
        private Texture depthtexture;
        private Surface depthsurface, olddepth;
        private Effect presenteffect, effect2, effect3;
        public VertexDeclaration vertexDecl2 { get; private set; }
        private EffectHandle technique, technique2;
        public VertexBuffer vertices2 { get; private set; }
        
        private IndexBuffer indices;

        //  private IntPtr ctlhandle;
        private readonly size videosize;
        private readonly IRenderOwner renderer;
        private size viewsize;
        private ReaderWriterLockNoThreading movieslock = new ReaderWriterLockNoThreading();

        private ManualResetEvent ready = new ManualResetEvent(true);
        internal readonly List<RenderFrame> renderframes = new List<RenderFrame>();
        internal readonly List<VideoFrame> videoframes = new List<VideoFrame>();
        private PresentParameters pp;
        private WorkerThread thread;
        private bool islost;

        private int _lockcnt;
        private Thread lockthread;
        private readonly FrameFactory owner;
        private readonly Widget widget;
        private readonly System.Windows.Controls.Panel window;
        private System.Windows.Window mainwindow;
        private IntPtr hwnd;
        private layer _layer;
        
        private IXwtRender xwt { get; }
        public IXwtRender Xwt => this.opentk.Xwt;
        private IRenderer opentk;
        private IVideoFrame frame;
        private IRenderFrame renderframe;
        private IDisposable gllock;

        public void Lock()
        {
            if (this.lockthread == Thread.CurrentThread)
            {
                this._lockcnt++;
                return;
            }
            Monitor.Enter(this.owner);
            if (this._lockcnt++ == 0)
            {
                this.lockthread = Thread.CurrentThread;

                this.renderer.StartRender(this);
                //         window.MakeCurrent();
            }
        }

        public void Unlock()
        {
            if (--_lockcnt == 0)
            {
                this.renderer.EndRender(this);
                this.lockthread = null;
                //    this.window.Context.MakeCurrent(null);
                Monitor.Exit(this.owner);
            }
        }

        public Effect LoadEffect(string effectsource)
        {
            return Effect.FromString(this.device, effectsource, ShaderFlags.None);
        }

        private Effect _LoadEffect(string resourcename)
        {
            try
            {
                var pi = typeof(BaseLib.Display.WPF.Properties.Resources).GetProperty(resourcename, BindingFlags.Static | BindingFlags.NonPublic);
                byte[] data = (byte[])pi.GetValue(null, null);
                return Effect.FromMemory(this.device, data, ShaderFlags.None);
            }
            catch (CompilationException e)
            {
                //  BaseLib.Windows.Forms.MessageBox.Show(e);
                throw;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        class layer : D3DImage
        {
            private static int id = 0;
            private IntPtr _scene;
            private Surface rt;
            internal System.Windows.Shapes.Rectangle rec;
            private IntPtr surfptr;
            private long lastupdate=-1, timebase;
            private long frametime;
            private readonly DirectX9Renderer owner;
            private readonly string layername;

            public static implicit operator FrameworkElement(layer l)
            {
                return l.rec;
            }

            public layer(DirectX9Renderer owner)
            {
                this.owner = owner;
                this.layername = $"Image_3d_{id++}";

                base.IsFrontBufferAvailableChanged += Layer_IsFrontBufferAvailableChanged;

                //  owner.window.Resources[this.layername] = ;

          //      BeginRenderingScene();

                this.rec = new System.Windows.Shapes.Rectangle()
                {
                  //  Width = owner.viewsize.width,
                 //   Height = owner.viewsize.height,
                    Fill =/* new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 0))*/  new ImageBrush(this),
                    HorizontalAlignment=HorizontalAlignment.Stretch,
                    VerticalAlignment =VerticalAlignment.Stretch
                };

                //  DrawingVisual drawingVisual = new DrawingVisual();

                // Retrieve the DrawingContext in order to create new drawing content.
                /*   DrawingContext drawingContext = this.RenderOpen();

                   // Create a rectangle and draw it in the DrawingContext.
                   Rect rect = new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(320, 80));
                   drawingContext.DrawRectangle(System.Windows.Media.Brushes.LightBlue, (System.Windows.Media.Pen)null, rect);

                   // Persist the drawing content.
                   drawingContext.Close();*/

            }

            private void Layer_IsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
            {
                // if the front buffer is available, then WPF has just created a new
                // D3D device, so we need to start rendering our custom scene
                if (this.IsFrontBufferAvailable)
                {
                    BeginRenderingScene();
                }
                else
                {
                    // If the front buffer is no longer available, then WPF has lost its
                    // D3D device so there is no reason to waste cycles rendering our
                    // custom scene until a new device is created.
                    StopRenderingScene();
                }
            }

            internal void StopRenderingScene()
            {
                // This method is called when WPF loses its D3D device.
                // In such a circumstance, it is very likely that we have lost 
                // our custom D3D device also, so we should just release the scene.
                // We will create a new scene when a D3D device becomes 
                // available again.
                CompositionTarget.Rendering -= OnRendering;
                ReleaseScene();
                _scene = IntPtr.Zero;
            }

            internal void BeginRenderingScene()
            {
                if (this.IsFrontBufferAvailable)
                {
                    // create a custom D3D scene and get a pointer to its surface
                    _scene = InitializeScene();

                    // set the back buffer using the new scene pointer
                    this.Lock();
                    this.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _scene);
                    this.Unlock();

                    // leverage the Rendering event of WPF's composition target to
                    // update the custom D3D scene
                    CompositionTarget.Rendering += OnRendering;

                    OnRendering(null, EventArgs.Empty);
                }
            }

            private IntPtr InitializeScene()
            {
                var h = IntPtr.Zero;

                if (owner.isex)
                {
                    //this.rt = Surface.CreateRenderTargetEx(this.owner.device as DeviceEx, this.owner.videosize.Width, this.owner.videosize.Height, Format.A8R8G8B8, MultisampleType.None, 0);
                    this.rt = Surface.CreateRenderTarget(this.owner.device, this.owner.viewsize.width, this.owner.viewsize.height, Format.A8R8G8B8, MultisampleType.None, 0, false);
                }
                else
                {
                    this.rt = Surface.CreateRenderTarget(this.owner.device, this.owner.viewsize.width, this.owner.viewsize.height, Format.A8R8G8B8, MultisampleType.None, 0, !this.owner.isex);
                }
                Guid g = new Guid(0xcfbaf3a, 0x9ff6, 0x429a, 0x99, 0xb3, 0xa2, 0x79, 0x6a, 0xf8, 0xb8, 0x9b);

                Marshal.QueryInterface(this.rt.NativePointer, ref g, out this.surfptr);

                return this.surfptr;
            }
            private void ReleaseScene()
            {
                this.Lock();
                this.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                this.Unlock();

                if (this.surfptr != IntPtr.Zero)
                {
                    Marshal.Release(this.surfptr);
                    this.surfptr = IntPtr.Zero;
                }
                this.rt?.Dispose();
                this.rt = null;
            }

            internal void Dispose()
            {
                StopRenderingScene();
                GC.SuppressFinalize(this);
            //    ReleaseScene();
            }


            private void OnRendering(object sender, EventArgs e)
            {
                if (this.IsFrontBufferAvailable && _scene != IntPtr.Zero)
                {
                    var now = DateTime.Now.Ticks;
                    if (lastupdate == -1)
                    {
                        this.timebase = now;
                        lastupdate = 0;
                    }
                    var time = now - this.timebase;

                    if (lastupdate != -1 && (time - lastupdate) > 25000000)
                    {
                        this.timebase += time - lastupdate;
                        time = lastupdate;
                    }
                    lastupdate = time;

                    this.frametime = time;
                    if (this.owner.renderer.preparerender(null, time, false))
                    {

                        // lock the D3DImage
                        this.Lock();

                        using (this.owner.opentk.GetDrawLock())
                        {
                            // update the scene (via a call into our custom library)
                            RenderScene();
                        }
                        // invalidate the updated region of the D3DImage (in this case, the whole image)
                        this.AddDirtyRect(new Int32Rect(0, 0, this.owner.viewsize.width, this.owner.viewsize.height));

                        // unlock the D3DImage
                        this.Unlock();
                    }
                }
            }
            private void RenderScene() // present (using IRenderOwner)
            {
         //       owner.Lock();
                try
                {
                    var oldtargets = new Surface[1];

                    for (int nit = 0; nit < 1; nit++)
                    {
                        try
                        {
                            oldtargets[nit] = this.owner.device.GetRenderTarget(nit);
                        }
                        catch { }
                    }
                    this.owner.device.SetRenderTarget(0, this.rt);

                    // clear the surface to transparent
                    //     this.owner.device.Clear(ClearFlags.Target, new global::SharpDX.Color(0, 0, 0, 255), 1.0f, 0);

                    // this.owner.device.BeginScene();

              //      using (var dl = (this.owner as ).GetDrawLock()) // render using opengl
                    {
                       var state = this.owner.StartRender(null, rectangle.Zero);
                        var r = new rectangle(0,0,this.owner.renderframe.Width,this.owner.renderframe.Height);
                        this.owner.renderer.render(null, this.frametime, rectangle.Zero);
                        this.owner.EndRender(state);
                    }
                    /*
                                    // render the scene
                                    if (SUCCEEDED(g_pd3dDevice->BeginScene()))
                                    {
                                        // setup the world, view, and projection matrices
                                        SetupMatrices();

                                        // render the vertex buffer contents
                                        g_pd3dDevice->SetStreamSource(0, g_pVB, 0, sizeof(CUSTOMVERTEX));
                                        g_pd3dDevice->SetFVF(D3DFVF_CUSTOMVERTEX);
                                        g_pd3dDevice->DrawPrimitive(D3DPT_TRIANGLESTRIP, 0, 1);

                                        // end the scene
                                        g_pd3dDevice->EndScene();
                                    }

                                    // return the full size of the surface
                                    pSize->cx = WIDTH;
                                    pSize->cy = HEIGHT;*/
                    //  this.dev.EndScene();

              //      this.owner.device.EndScene();
                    this.owner.device.SetRenderTarget(0, oldtargets[0]);
                    oldtargets[0]?.Dispose();

                }
                finally
                {
               //     owner.Unlock();
                }
            }
            
        }

        internal DirectX9Renderer(FrameFactory owner, IXwtRender xwt, Canvas widget, System.Windows.FrameworkElement window, System.Windows.Window main, IRenderOwner renderer, FPS fps, size videosize)
        {
            this.owner = owner;
            this.xwt = xwt;
            this.widget = widget;
            this.window = window as System.Windows.Controls.Panel;
            this.mainwindow = main;
            this.videosize = videosize;
            this.renderer = renderer;

            var w = System.Windows.Window.GetWindow(this.mainwindow);
            var h = new WindowInteropHelper(w);
            this.hwnd = h.Handle;

            /*    mainwindow.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                mainwindow.Arrange(new Rect(0, 0, mainwindow.Width, mainwindow.Height));

                this.window.Arrange(ew Rect(0, 0, this.window.ActualWidth, this.window.ActualHeight));*/



            //window..CompositionTarget

            //    OpenTKRenderer.usecnt = 1;            //  this.ctlhandle = this.ctl.Handle;
            this.viewsize = new size(Convert.ToInt32(window.ActualWidth), Convert.ToInt32(window.ActualHeight));

            this.window.SizeChanged += Ctl_SizeChanged;

            xwt.CreateForWidgetContext(this, renderer, widget);

            this.opentk = this.owner.opentk.Open(this.owner.opentkxwt, widget, renderer,fps, videosize);

            this.thread = new WorkerThread();
            this.thread.Do(() =>
            {
                //     System.Drawing.Rectangle r = new System.Drawing.Rectangle(System.Drawing.Point.Empty, this.viewsize);// Win32Helper.GetClientRect(ctlhandle);

                //     this.lastsize = new System.Drawing.Size(r.Width, r.Height);

                this.pp = new PresentParameters(this.videosize.width, this.videosize.height);
                pp.DeviceWindowHandle = this.hwnd;
                pp.EnableAutoDepthStencil = true;
                pp.SwapEffect = SwapEffect.Copy;
                pp.PresentationInterval = PresentInterval.Immediate;

                try
                {
                    /*       this.direct3D = new Direct3DEx();
                           this.isex = true;
                           this.device = new DeviceEx(this.direct3D as Direct3DEx, 0, DeviceType.Hardware, this.hwnd, CreateFlags.Multithreaded | CreateFlags.HardwareVertexProcessing, pp);*/
                }
                catch
                {
                    if (this.direct3D != null)
                    {
                        throw;
                    }
                }
                if (this.direct3D == null)
                {
                    this.direct3D = new Direct3D();
                    this.device = new Device(this.direct3D, 0, DeviceType.Hardware, this.hwnd, CreateFlags.Multithreaded | CreateFlags.HardwareVertexProcessing, pp);
                }
                this.depthtexture = new Texture(this.device, 4096, 4096, 1, Usage.DepthStencil, Format.D24S8, Pool.Default);
                this.depthsurface = this.depthtexture.GetSurfaceLevel(0);
                this.olddepth = this.device.DepthStencilSurface;
                this.device.DepthStencilSurface = this.depthsurface;

                //       this.lastsize = r.Size;

                // Compiles the effect
                this.presenteffect = _LoadEffect("render");// Effect.FromFile(device, "render.fx", ShaderFlags.None);
                this.technique = presenteffect.GetTechnique(0);
                this.effect2 = _LoadEffect("render2");
                this.technique2 = effect2.GetTechnique(0);
                this.effect3 = _LoadEffect("render3");


                // Get the technique

                // Prepare matrices

                // Creates and sets the Vertex Declaration
                this.vertexDecl2 = new VertexDeclaration(device, vertexElems2);
                //    device.SetStreamSource(0, vertices2, 0, Utilities.SizeOf<vertex>());
                //      device.VertexDeclaration = vertexDecl2;

                this.vertices2 = new VertexBuffer(device, Utilities.SizeOf<vertex>() * 6, Usage.WriteOnly, VertexFormat.None, isex ? Pool.Default : Pool.Managed);
                vertices2.Lock(0, 0, LockFlags.None).WriteRange(this.initdata);
                vertices2.Unlock();

                this.indices = new IndexBuffer(device, sizeof(int) * initdata2.Length, Usage.WriteOnly, isex ? Pool.Default : Pool.Managed, false);
                this.indices.Lock(0, 0, LockFlags.None).WriteRange(this.initdata2);
                this.indices.Unlock();
                
                this.frame = new VideoFrame(this);
                this.frame.Set(opentk.AlphaFormat);
            });

            this._layer = new layer(this);
            this._layer?.rec.Arrange(new Rect(0, 0, this.window.ActualWidth, this.window.ActualHeight));

            this.window.Children.Add((FrameworkElement)_layer);

            //this.initdone.Set();
        }

        void IRenderer.Start()
        {
            this._layer.BeginRenderingScene();
        }

        private void Ctl_SizeChanged(object sender, EventArgs e)
        {
            _layer?.StopRenderingScene();
            this.viewsize = new size(Convert.ToInt32(window.ActualWidth), Convert.ToInt32(window.ActualHeight));
            _layer?.rec.Arrange(new Rect(0, 0, this.window.ActualWidth, this.window.ActualHeight));
            _layer?.rec.UpdateLayout();
            _layer?.BeginRenderingScene();
            //     this._layer?.rec.Arrange(new Rect(0, 0, this.window.ActualWidth, this.window.ActualHeight));
            //this.viewsize = this.ctl.ClientSize;
        }

        ~DirectX9Renderer()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Reset()
        {
            this.thread.Do(() =>
            {
                ready.Reset();

                Debug.WriteLine($"{GetHashCode()} getting write lock");

                this.movieslock.LockWrite();
                lock (this)
                {

                    lock (this.videoframes) { this.videoframes.ForEach(_f => _f.OnLost()); }
                    lock (this.renderframes) { this.renderframes.ForEach(_f => _f.OnLost()); }

                    pp.BackBufferWidth = this.videosize.width;
                    pp.BackBufferHeight = this.videosize.height;

                    this.technique2.Dispose();
                    this.technique.Dispose();
                    this.effect3.Dispose();
                    this.effect2.Dispose();
                    this.presenteffect.Dispose();

                    this.device.DepthStencilSurface = this.olddepth;
                    this.olddepth.Dispose();
                    this.depthtexture.Dispose();
                    this.depthsurface.Dispose();

                    device.Reset(pp);

                    this.depthtexture = new Texture(this.device, 4096, 4096, 1, Usage.DepthStencil, Format.D24S8, Pool.Default);
                    this.depthsurface = this.depthtexture.GetSurfaceLevel(0);

                    this.olddepth = this.device.DepthStencilSurface;
                    this.device.DepthStencilSurface = this.depthsurface;

                    this.presenteffect = _LoadEffect("render");
                    this.effect2 = _LoadEffect("render2");
                    this.effect3 = _LoadEffect("render3");

                    // Compiles the effect

                    //  this.effect = Effect.FromFile(device, "render.fx", ShaderFlags.None);
                    this.technique = presenteffect.GetTechnique(0);
                    //this.effect2 = Effect.FromFile(device, "render2.fx", ShaderFlags.None);
                    this.technique2 = effect2.GetTechnique(0);
                    // Get the technique

                    lock (this.videoframes) { this.videoframes.ForEach(_f => _f.OnReset()); }
                    lock (this.renderframes) { this.renderframes.ForEach(_f => _f.OnReset()); }

                }
                this.movieslock.UnlockWrite();
                ready.Set();
            });
        }
        public void Stop()
        {
            //       stopevent.Set();
        }

        public void Dispose(bool disposing)
        {
            this.window.Children.Remove((FrameworkElement)_layer);

            this._layer.Dispose();
            this._layer = null;
            
            this.thread.Do((Action)(() =>
            {
                this.renderframe?.Dispose();
                this.opentk?.Dispose();

                ready.Set();

                this.window.SizeChanged -= Ctl_SizeChanged;

                if (this.olddepth != null)
                {
                    this.device.DepthStencilSurface = this.olddepth;
                    this.olddepth?.Dispose();
                    this.olddepth = null;
                }
                this.frame?.Dispose();
                this.Xwt.FreeWindowInfo(this.widget);
                depthsurface?.Dispose(); depthsurface = null;
                depthtexture?.Dispose(); depthtexture = null;
                effect3?.Dispose(); effect3 = null;
                effect2?.Dispose(); effect2 = null;
                presenteffect?.Dispose(); presenteffect = null;
                vertices2?.Dispose(); vertices2 = null;
                indices?.Dispose(); indices = null;
                device?.Dispose(); device = null;
                direct3D?.Dispose(); direct3D = null;

            }));
            this.thread.Dispose();
        }

        IVideoFrame IRenderer.GetFrame()
        {
            return this.opentk.GetFrame();

       /*     movieslock.Lock();
            var result = new VideoFrame(this);

            lock (this.videoframes)
            {
                this.videoframes.Add(result);
            }
            movieslock.Unlock();
            return result;*/
        }

        IRenderFrame IRenderer.GetRenderFrame(int levels)
        {
            return this.opentk.GetRenderFrame(levels);
        /*    movieslock.Lock();
            var result = new RenderFrame(this, levels);
            lock (this.renderframes)
            {
                this.renderframes.Add(result);
            }
            movieslock.Unlock();
            return result;*/
        }

        /*    private void Paint(System.Drawing.Rectangle dstrec, Effect effect, int pass)
            {
                device.SetStreamSource(0, vertices2, 0, Utilities.SizeOf<vertex>());
                device.VertexDeclaration = vertexDecl2;

                //  effect.Technique = technique;
                effect.Begin();
                effect.BeginPass(pass);

                device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);

                effect.EndPass();
                effect.End();

                // device.SetStreamSource(0, null, 0, 0);
            }*/

        internal void Combine(IVideoFrame[] frames, RenderFrame frame)
        {
            //return this.opentk.Combine(frame, frame);
            /*  object state = this.StartRender(frame,Color.Green);

              try
              {
                  var dstrec = new System.Drawing.Rectangle(0, 0, this.videosize.Width, this.videosize.Height);
                  var effect = this.effect3;
                  var m = Matrix.Scaling(dstrec.Width, dstrec.Height, 1) * Matrix.Translation(dstrec.Left, dstrec.Top, 0);
                  var worldViewProj = m * this.CreateViewMatrix(this.videosize.Width, this.videosize.Height);

                  //  var texturematrix = Matrix.Scaling(dstrec.Width-1, dstrec.Height-1, 1);

                  effect.SetValue("worldViewProj", worldViewProj);
                  //     effect.SetValue("texturematrix", texturematrix);
                  effect.SetTexture("texture0", (frames[0] as IDirectXFrame).Textures[0]);
                  effect.SetTexture("texture1", (frames[1] as IDirectXFrame).Textures[0]);
                  effect.SetValue("vpHeight", this.videosize.Height);

                  this.Paint(
                      new System.Drawing.Rectangle(0, 0, this.videosize.Width, this.videosize.Height),
                      effect, 0);
              }
              catch { }
              finally
              {
                  this.EndRender(state);
              }*/
        }
        public object StartRender(IRenderFrame destination, rectangle r)
        {
            Monitor.Enter(this);

            if (destination == null) // render direct
            {
                if (this.renderframe == null || this.renderframe.Width!=this.videosize.width||this.renderframe.Height!=this.videosize.height)
                {
                    this.renderframe?.Dispose();
                    this.renderframe = this.opentk.GetRenderFrame(1);
                    this.renderframe.Set(this.opentk.AlphaFormat);
                    this.renderframe.Set(0, this.videosize.width, this.videosize.height, 0);
                }
                this.gllock = this.opentk.GetDrawLock();
                this.opentk.StartRender(this.renderframe, new rectangle(point.Zero, this.videosize));
                return null;
            }
            else
            {
                return this.opentk.StartRender(destination, r);
            }
          /*  RenderFrame frame = destination as RenderFrame;

            var oldtargets = new Surface[destination.Levels];

            for (int nit = 0; nit < 1; nit++)
            {
                try
                {
                    oldtargets[nit] = this.device.GetRenderTarget(nit);
                }
                catch { }
            }
            for (int nit = 0; nit < destination.Levels; nit++)
            {
                this.device.SetRenderTarget(nit, (destination as RenderFrame).rendertarget[nit]);
            }
            this.device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, new global::SharpDX.Mathematics.Interop.RawColorBGRA((byte)(color.Red * 255), (byte)(color.Green * 255), (byte)(color.Blue * 255), (byte)(color.Alpha * 255)), 0, 0);

            this.device.BeginScene();

            return oldtargets;*/
        }
      /*  void IRenderer.Paint(IRenderFrame destination, IVideoFrame src, global::Xwt.Rectangle dstrec)
        {
            (this as IRenderer).Paint(destination, src, 0, dstrec);
        }
        void IRenderer.Paint(IRenderFrame destination, IVideoFrame src, int index, global::Xwt.Rectangle dstrec)
        {
            IDirectXFrame framesrc = (IDirectXFrame)src;

            device.SetStreamSource(0, vertices2, 0, Utilities.SizeOf<vertex>());
            device.VertexDeclaration = vertexDecl2;

            var m = Matrix.Scaling(dstrec.Width, dstrec.Height, 1) * Matrix.Translation(dstrec.Left, dstrec.Top, 0);

            System.Drawing.Size s = destination != null ? new System.Drawing.Size(destination.Width, destination.Height) : this.videosize;

            var worldViewProj = m * CreateViewMatrix(s.Width, s.Height);

            effect.SetValue("worldViewProj", worldViewProj);
            effect.SetValue("alpha", 1.0f);
            effect.SetTexture("texture0", framesrc.Textures[index]);

            //       effect.Technique = technique;
            effect.Begin();
            effect.BeginPass(1);

            device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);

            effect.EndPass();
            effect.End();

            effect.SetTexture("texture0", null);
        }*/
        public void EndRender(object state)
        {
            if (state == null)
            {
               opentk.EndRender(this.renderframe);
                this.gllock.Dispose();
 /*
                this.frame.Set(0, this.viewsize.width, this.viewsize.height, 0);

                var dr = (this.frame as IDirectXFrame).Textures[0].LockRectangle(0, LockFlags.Discard);
                using (var lck = this.opentk.GetDrawLock())
                {
                    renderframe.CopyTo(dr.DataPointer, dr.Pitch);
                }
                (this.frame as IDirectXFrame).Textures[0].UnlockRectangle(0);*/

            }
            else
            {
                /*   var oldtargets = (Surface[])state;

                   this.device.EndScene();
                   for (int nit = oldtargets.Length - 1; nit >= 0; nit--)
                   {
                       try
                       {
                           this.device.SetRenderTarget(nit, oldtargets[nit]);
                       }
                       catch { }
                   }
                   for (int nit = 0; nit < oldtargets.Length; nit++)
                   {
                       oldtargets[nit]?.Dispose();
                   }*/
                opentk.EndRender(state);
            }
            Monitor.Exit(this);
        }

        byte[] fill = Enumerable.Repeat((byte)0xff, 1920 * 1080 * 4).ToArray();

        void IRenderer.Present(IVideoFrame src, rectangle dstrec, IntPtr window) // painting on block or rpreview-something with alpha=255
        {
            dstrec = new rectangle(point.Zero, this.viewsize);

            if (islost || device.TestCooperativeLevel() == ResultCode.DeviceLost /*||
                this.lastsize.Width != r.Width || this.lastsize.Height != r.Height*/)
            {

                Reset();
                //      this.lastsize = r.Size;

                islost = false;

            }
            if (src != null)
            {
                this.frame.Set(0, src.Width, src.Height, 0);

                var dr = (this.frame as IDirectXFrame).Textures[0].LockRectangle(0, LockFlags.Discard);

                Debug.Assert(this.frame.Width == src.Width);
                Debug.Assert(this.frame.Height == src.Height);


                using (var lck = this.opentk.GetDrawLock())
                {
                    src.CopyTo(dr.DataPointer, dr.Pitch);
             //       Marshal.Copy(fill, 0, dr.DataPointer, dr.Pitch * src.Height);
                }
                (this.frame as IDirectXFrame).Textures[0].UnlockRectangle(0);
            }
            else
            {
                this.frame.Set(0, this.renderframe.Width, this.renderframe.Height, 0);

                var dr = (this.frame as IDirectXFrame).Textures[0].LockRectangle(0, LockFlags.Discard);

                Debug.Assert(this.frame.Width == this.renderframe.Width);
                Debug.Assert(this.frame.Height == this.renderframe.Height);

                using (var lck = this.opentk.GetDrawLock())
                {
                    this.renderframe.CopyTo(dr.DataPointer, dr.Pitch);
             //       Marshal.Copy(fill, 0, dr.DataPointer, dr.Pitch * renderframe.Height);
                }
                (this.frame as IDirectXFrame).Textures[0].UnlockRectangle(0);
            }
            //    IDirectXFrame framesrc = (IDirectXFrame)src;

         /*   device.Viewport = new SharpDX.Mathematics.Interop.RawViewport()
            {
                X = 0,
                Y = 0,
                Width = this.viewsize.width,
                Height = viewsize.height,
                MinDepth=0,
                MaxDepth=1
            };*/

            device.Clear(ClearFlags.Target, new SharpDX.Mathematics.Interop.RawColorBGRA(0, 0, 255, 255), 1.0f, 0);

            device.BeginScene();

            device.SetStreamSource(0, vertices2, 0, Utilities.SizeOf<vertex>());
            device.VertexDeclaration = vertexDecl2;

            var m = Matrix.Scaling(dstrec.width, -dstrec.height, 1) * Matrix.Translation(dstrec.x, dstrec.height, 0);

         //   Matrix proj = Matrix.Scaling(1, -1, 1);
         //   m= Matrix.Multiply(m, proj);
            var worldViewProj = m * CreateViewMatrix(this.viewsize.width, this.viewsize.height);

            presenteffect.SetValue("worldViewProj", worldViewProj);
            presenteffect.SetValue("alpha", 1.0f);
            presenteffect.SetTexture("texture0", (this.frame as IDirectXFrame).Textures[0]);

            //       effect.Technique = technique;
            presenteffect.Begin();
            presenteffect.BeginPass(0);

            device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);

            presenteffect.EndPass();
            presenteffect.End();

            device.EndScene();

            presenteffect.SetTexture("texture0", null);
        }
        void IRenderer.AllocFunc(int width, int height, VideoFormat fmt, ref IntPtr data, ref int pitch, ref VideoFormat framefmt)
        {
            this.opentk.AllocFunc(width, height, fmt, ref data, ref pitch, ref framefmt);
           // var frame = (this as IRenderer).GetFrame();
        }
        internal void UpdateTexture(Texture src, Texture dst, int h)
        {
            Surface s1 = null, s2 = null;
            try
            {
                s1 = src.GetSurfaceLevel(0);

                /* var lr = s1.LockRectangle(LockFlags.Discard);

                 FFMPEG.memset(lr.DataPointer, 0xff, lr.Pitch *h);

                 s1.UnlockRectangle();*/

                s2 = dst.GetSurfaceLevel(0);
                this.device.UpdateSurface(s1, s2);
            }
            finally
            {
                s2?.Dispose();
                s1?.Dispose();
            }
        }
        void IRenderer.PrepareRender()
        {
            this.opentk.PrepareRender();
        }
        void IRenderer.StopRender()
        {
            this.opentk.StopRender();
        }

        internal void Deinterlace(IVideoFrame frame, IRenderFrame destination, DeinterlaceModes mode)
        {
            if (this.device != null)
            {
                switch (mode)
                {
                    default:
                        throw new NotImplementedException();

                    case DeinterlaceModes.Blend:
                    case DeinterlaceModes.Split:
                        {
                            var state = this.StartRender(destination,rectangle.Zero);
                            try
                            {
                                /*         var dstrec = new System.Drawing.Rectangle(0, 0, destination.Width, destination.Height);
                                         var effect = this.effect2;
                                         var m = Matrix.Scaling(dstrec.Width, dstrec.Height, 1) * Matrix.Translation(dstrec.Left, dstrec.Top, 0);
                                         var worldViewProj = m * this.CreateViewMatrix(destination.Width, destination.Height);
                                         int n;
                                         switch (mode)
                                         {
                                             case DeinterlaceModes.Blend:
                                                 {
                                                     effect.SetValue("worldViewProj", worldViewProj);
                                                     effect.SetTexture("texture0", (frame as IDirectXFrame).Textures[0]);
                                                     effect.SetValue("vpHeight", frame.Height);
                                                     n = 1;
                                                 }
                                                 break;
                                             case DeinterlaceModes.Split:
                                                 {
                                                     effect.SetValue("worldViewProj", worldViewProj);
                                                     effect.SetTexture("texture0", (frame as IDirectXFrame).Textures[0]);
                                                     effect.SetValue("vpHeight", frame.Height);
                                                     n = 0;
                                                 }
                                                 break;
                                             default:
                                                 throw new NotImplementedException();
                                         }
                                         this.Paint(
                                             new System.Drawing.Rectangle(0, 0, destination.Width, destination.Height),
                                             effect, n);*/
                            }
                            finally
                            {
                                this.EndRender(state);
                            }
                        }
                        break;
                }
            }
        }

        public IDisposable GetDrawLock() => opentk.GetDrawLock();
    }
    public class FrameFactory : IRendererFactory
    {
        private readonly List<IRenderer> renderers = new List<IRenderer>();
        internal readonly IRendererFactory opentk;
        internal readonly GTK opentkxwt;

        public string Name { get => "SharpDX"; }

        public VideoFormat AlphaFormat => VideoFormat.ARGB;

        public bool NeedPresentThread => false;// update done by wpf

        public bool OpenGLInUIThread => false; // 3d device not thread bound

        public FrameFactory()
        {
          //  this.opentk = new BaseLib.Media.OpenTK.FrameFactory();
            this.opentkxwt = new BaseLib.Platforms.GTK(out this.opentk);
        }
        protected virtual void Dispose(bool disposing)
        {
            this.opentk?.Dispose();
            //   (this as IRendererFactory).Close();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        void IRendererFactory.Initialize()
        {
        }
       /* IRenderer IRendererFactory.Open(object ctl, Xwt.Size videosize)
        {
            throw new NotImplementedException();
        }*/
        IRenderer IRendererFactory.Open(IXwtRender xwt, Canvas w, IRenderOwner renderer, FPS fps, size videosize)
        {
            lock (this)
            {
                var window = w.ParentWindow;
                var wFrame = global::Xwt.Toolkit.CurrentEngine.GetSafeBackend(window) as global::Xwt.WPFBackend.WindowFrameBackend;
                var wBackend = global::Xwt.Toolkit.CurrentEngine.GetSafeBackend(w);
                var e = (System.Windows.FrameworkElement)wBackend.GetType().GetPropertyValue(wBackend, "Widget");

                return new DirectX9Renderer(this, xwt,w,e, wFrame.Window, renderer, fps, videosize);
            }
        }
        internal void Close(IRenderer renderer)
        {
        }
    }
    internal static class Extension
    {
        public static object InvokeStatic(this Type type, string method, params object[] arguments)
        {
            return type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, arguments);
        }

        public static object Invoke(this Type type, object instance, string method, params object[] arguments)
        {
            return type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(instance, arguments);
        }
        public static object GetPropertyValue(this Type type, object instance, string propertyname)
        {
            return type.GetProperty(propertyname, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty).GetValue(instance, new object[0]);
        }
        public static object GetPropertyValueStatic(this Type type, string propertyname)
        {
            return type.GetProperty(propertyname, BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty).GetValue(null, new object[0]);
        }

        /*     public static T InvokePrivate<T>(this Type type, string method, params object[] arguments)
             {
                 return (T)type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, arguments);
             }
             public static T GetPropertyValue<T>(this Type type, string propertyname)
             {
                 return (T)type.GetProperty(propertyname, BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty).GetValue(null, new object[0]);
             }
             public static T GetPropertyValuePrivate<T>(this Type type, string propertyname)
             {
                 return (T)type.GetProperty(propertyname, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetProperty).GetValue(null, new object[0]);
             }
             public static T GetFieldValuePrivate<T>(this Type type, string propertyname)
             {
                 return (T)type.GetField(propertyname, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField).GetValue(null);
             }
             public static void SetFieldValuePrivate(this Type type, string propertyname, object value)
             {
                 type.GetField(propertyname, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.SetField).SetValue(null, value);
             }*/
    }
}
