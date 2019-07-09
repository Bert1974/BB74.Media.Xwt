using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using BaseLib.Media.Display;
using BaseLib.Media.Video;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using Xwt;

namespace BaseLib.Media.OpenTK
{
    using Xwt = global::Xwt;

    public class _rendererlock : IDisposable
    {
        private OpenTKRenderer renderer;

        public _rendererlock(IRenderer renderer)
        {
            this.renderer = renderer as OpenTKRenderer;
            this.renderer.Lock();
        }
        ~_rendererlock()
        {
            Debug.Assert(this.renderer == null);
        }
        public void Dispose()
        {
            this.renderer.Unlock();
            this.renderer = null;
            GC.SuppressFinalize(this);
        }
    }
    public class OpenTKRenderer : IRenderer
    {
        public bool ForceNoThreading => throw new Exception();
        public bool UseNoThreading => throw new Exception();

        public VideoFormat AlphaFormat => VideoFormat.RGBA;

        static float[] _vertices = {
    0, 0, 0, 0, // Top-left
    1, 0, 1, 0, // Top-right
    0, 1f, 0, 1,  // Bottom-left

    1, 1, 1, 1, // Bottom-right
    1, 0, 1, 0, // Top-right
    0, 1f, 0, 1,  // Bottom-left
    };
        static float[] _vertices2 = {
    -1.0f,  1.0f, 0.0f, 1.0f, // Top-left
     1.0f,  1.0f, 1.0f, 1.0f, // Top-right
    -1.0f, -1.0f, 0.0f, 0.0f,  // Bottom-left

     1.0f, -1.0f, 1.0f, 0.0f, // Bottom-right
     1.0f,  1.0f, 1.0f, 1.0f, // Top-right
    -1.0f, -1.0f, 0.0f, 0.0f  // Bottom-left
    };
        static vertex[] _vertices3 = {
   new vertex(-1.0f,  1.0f, 0.0f, 0.0f), // Top-left
   new vertex( 1.0f,  1.0f, 1.0f, 0.0f), // Top-right
   new vertex(-1.0f, -1.0f, 0.0f, 1.0f),  // Bottom-left
   
   new vertex(1.0f, -1.0f, 1.0f, 1.0f), // Bottom-right
   new vertex(1.0f,  1.0f, 1.0f, 0.0f), // Top-right
   new vertex(-1.0f, -1.0f, 0.0f, 1.0f)  // Bottom-left
    };


        [StructLayout(LayoutKind.Explicit, Size = 4 * 4, CharSet = CharSet.Ansi)]
        struct vertex
        {
            public vertex(float x,float y,float tx,float ty)
            {
                this.position = new Vector2(x, y);
                this.texcoord = new Vector2(tx, ty);
            }
            [FieldOffset(0)]
            public Vector2  texcoord;
            [FieldOffset(8)]
            public Vector2 position;
        }


        const string shadervertex = @"#version 150 core

        in vec2 position;
        in vec2 texcoord;

        out vec2 Texcoord;

        void main()
        {
            Texcoord = texcoord;
            gl_Position = vec4(position.x,-position.y, 0.0, 1.0);
        }";

        const string shaderfragment = @"#version 150 core
precision mediump float;

        in vec2 Texcoord;
        
        out vec4 outColor;

         uniform sampler2D tex;

        void main()
        {
            outColor = texture(tex, Texcoord);
        }";
        const string combineshadervertex = @"#version 150 core

        in vec2 position;
        in vec2 texcoord;


        out vec2 Texcoord;
        out vec2 Texcoord1;

        void main()
        {
            Texcoord = texcoord;
            Texcoord1 = position;
            gl_Position = vec4(position, 0.0, 1.0);
        }";

        const string combineshaderfragment = @"#version 150 core
precision mediump float;

        in vec2 Texcoord;
        in vec2 Texcoord1;
        
        out vec4 outColor;

         uniform sampler2D tex;
         uniform sampler2D tex2;

       uniform float vpHeight;

        void main()
        {
            float v = round(vpHeight * (Texcoord1.y) / 2.0);
	        if (round(v /2.0) == v /2.0)
	        {
                    outColor = texture(tex, Texcoord);
            }
            else
	        {
                    outColor = texture(tex2, Texcoord);
            }
        }";
        const string blendshaderfragment = @"#version 150 core

precision mediump float;
        in vec2 Texcoord;
        in vec2 Texcoord1;
        
        out vec4 outColor;

         uniform sampler2D tex;

       uniform float vpHeight;

        void main()
        {
	        float d = .5 / vpHeight;
	        float x = Texcoord.x, y = Texcoord.y;
	        //return float4(input.tex0.xy,input.tex0.xy);
	        vec4 color0 = vec4(texture(tex, vec2(x, y)).rgb, 1);
	        vec4 color1 = vec4(texture(tex, vec2(x, y + d)).rgb, 1);
	        outColor= (color0 + color1) / 2.0;
        }";
        const string splitshaderfragment = @"#version 150 core

precision mediump float;
        in vec2 Texcoord;
        in vec2 Texcoord1;
        
        out vec4 outColor;
        out vec4 outColor2;

         uniform sampler2D tex;

       uniform float vpHeight;

        void main()
        {
	        float d = .5 / vpHeight;
	        float x = Texcoord.x, y = Texcoord.y;
	        //return float4(input.tex0.xy,input.tex0.xy);
	        outColor = vec4(texture(tex, vec2(x, y)).rgb, 1);
	        outColor2 = vec4(texture(tex, vec2(x, y + d)).rgb, 1);
        }";

        private vertices vertices1, vertices2;

        private vertices<vertex> vertices3;

        private size videosize;
        private readonly Xwt.Widget window;
        private readonly IRenderOwner renderer;
        private FrameFactory owner;

        public IXwtRender Xwt { get; }

            private Size viewsize;

        //  public int buf_vertices;//todo abc bert
        //  public int buf_vertices2, buf_vertices3;
        //     private IAsyncResult update_ar;

        WaitHandle IRenderer.stopevent => this.stopevent;
        public ManualResetEvent stopevent  = new ManualResetEvent(false);
        private int _lockcnt;
        private Thread lockthread;

        shader combineshader, deinterlaceblendshader, deinterlacesplitshader, presentshader;

        private Thread thread, actionthread;
        private ManualResetEvent stoppedevent = new ManualResetEvent(true);
        private long timebase, lastupdate;

        public void Lock()
        {
            try
            {
                if (this.lockthread == Thread.CurrentThread)
                {
                    this._lockcnt++;
                    return;
                }
                Monitor.Enter(this);
                if (this._lockcnt++ == 0)
                {
                    this.lockthread = Thread.CurrentThread;
                    this.renderer.StartRender(this);
                }
            }
            catch (Exception e)
            {
                //     Log.LogException(e);
            }
        }

        public void Unlock()
        {
            Debug.Assert(this._lockcnt > 0);
            try
            {
                if (--_lockcnt == 0)
                {
                    this.renderer.EndRender(this);
                    this.lockthread = null;
                    Monitor.Exit(this);
                }
            }
            catch (Exception e)
            {
                //        Log.LogException(e);
            }
        }

        private void run()
        {
            while (true)
            {
                var a = actions.Take();

                try
                {
                    if (a.action != null)
                    {
                        a.action();
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    //       Log.LogException(e);
                }
                finally
                {
                    a.done.Set();
                }
                if (a.action == null)
                {
                    return;
                }
            }
        }
        private void Invoke(Action function)
        {
            var a = new actioninfo() { action = function };
            
            actions.Add(a);

          //  while (!a.done.WaitOne(0, false))
         //   {
                this.renderer.DoEvents(()=> !a.done.WaitOne(0, false));
         //   }
            a.Dispose();
        }
        class actioninfo
        {
            public ManualResetEvent done = new ManualResetEvent(false);
            public Action action;

            internal void Dispose()
            {
                done.Dispose();
                GC.SuppressFinalize(this);
            }
        }
        private readonly BlockingCollection<actioninfo> actions = new BlockingCollection<actioninfo>();

        internal OpenTKRenderer(FrameFactory owner, IXwtRender xwtrender, Xwt.Canvas window, IRenderOwner renderer, size videosize)
        {
            //    OpenTKRenderer.usecnt = 1;
            this.owner = owner;
            this.Xwt = xwtrender;
            this.videosize = videosize;
            this.window = window;
            this.renderer = renderer;

            this.viewsize = window.Size;
            window.BoundsChanged += viewsizechanged;

            this.actionthread = new Thread(run) { Name = "opentk" };
            this.actionthread.Start();

            var _this = this;

            this.Xwt.CreateForWidgetContext(this, this.renderer, window);

           // Invoke(() =>
          //  {
                this.Lock();
                try
                {
                    try
                    {
                        //        GL.GetInternalformat(TextureTarget.Texture2D, SizedInternalFormat.Rgba8, InternalFormatParameter..TEXTUREIMAGEFORMAT, 1, &preferred_format).

                        this.vertices1 = new vertices(_vertices);
                        this.vertices2 = new vertices(_vertices2);

                        /*   GL.GenBuffers(1, out buf_vertices2); // Generate 1 buffer
                           GL.BindBuffer(BufferTarget.ArrayBuffer, buf_vertices2);
                           GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
                           GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
                           GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * vertices2.Length, vertices2, BufferUsageHint.StaticDraw);
                           GL.EnableVertexAttribArray(0);
                           GL.EnableVertexAttribArray(1);

                           GL.GenBuffers(1, out buf_vertices3); // Generate 1 buffer
                           GL.BindBuffer(BufferTarget.ArrayBuffer, buf_vertices3);
                           GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
                           GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
                           GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * vertices3.Length, vertices3, BufferUsageHint.StaticDraw);
                           GL.EnableVertexAttribArray(0);
                           GL.EnableVertexAttribArray(1);*/

                   //     GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                        /*    this.presentshader = new shader(shadervertex, shaderfragment, vertices1);
                            GL.UseProgram(this.presentshader);
                            var pos = GL.GetUniformLocation(this.presentshader, "tex");
                            GL.Uniform1(pos, 0);
                            */
           /*             this.combineshader = new shader(combineshadervertex, combineshaderfragment, vertices1);

                        GL.UseProgram(combineshader);
                        var pos = GL.GetUniformLocation(this.combineshader, "tex");
                        GL.Uniform1(pos, 0);
                        pos = GL.GetUniformLocation(this.combineshader, "vpHeight");
                        GL.Uniform1(pos, (float)videosize.height);

                        this.deinterlaceblendshader = new shader(combineshadervertex, blendshaderfragment, vertices1);
                        GL.UseProgram(deinterlaceblendshader);
                        pos = GL.GetUniformLocation(this.deinterlaceblendshader, "vpHeight");
                        GL.Uniform1(pos, (float)videosize.height);

                        this.deinterlacesplitshader = new shader(combineshadervertex, splitshaderfragment, vertices1);

                        GL.UseProgram(deinterlacesplitshader);
                        pos = GL.GetUniformLocation(this.deinterlacesplitshader, "vpHeight");
                        GL.Uniform1(pos, (float)videosize.height);*/

                    }
                    catch (Exception e)
                    {
                        //         Log.LogException(e);
                        Dispose(true);
                        GC.SuppressFinalize(this);
                        throw;
                    }
                }
                finally
                {
                    Unlock();
                }
         //   });

        }
        ~OpenTKRenderer()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Stop()
        {
            this.stopevent.Set();
            this.stoppedevent.WaitOne(-1, false);
        }

        public void Start()
        {
            if (this.owner.NeedPresentThread)
            {
                this.stopevent.Reset();
                this.stoppedevent.Reset();
                this.thread = new Thread(this.main) { Name = "opentk-present", IsBackground = true };

                this.lastupdate = -1;
                this.thread.Start();
            }
        }

        public void Dispose(bool disposing)
        {
            this.stopevent.Set();
            this.stoppedevent.WaitOne(-1, false);

            this.Lock();
            try
            {
                this.combineshader?.Dispose();
                this.deinterlaceblendshader?.Dispose();
                this.deinterlacesplitshader?.Dispose();
                this.presentshader?.Dispose();
                this.vertices1?.Dispose();
                this.vertices2?.Dispose();
                this.vertices3?.Dispose();

                this.Xwt.FreeWindowInfo(window);
            }
            finally
            {
                this.Unlock();
            }
            window.BoundsChanged -= viewsizechanged;
            Invoke(null);
            this.owner.Close(this);
        }
        private void viewsizechanged(object sender, EventArgs e)
        {
            this.viewsize = window.Size;
        }
        private void main()
        {
            while (true)
            {
                //int waitres = WaitHandle.WaitAny(new WaitHandle[] { this.stopevent }, -1, false);

                if (this.stopevent.WaitOne(0, false))
                {
                    this.stoppedevent.Set();
                    return;
                }
                try
                {
                    //       Xwt.Application.InvokeAsync(() => {
                    try
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

                        if (this.renderer.preparerender(null, time, true))
                        {
                            using (var lck = this.GetDrawLock())
                            {
                                var r = new rectangle(0, 0,
                                    Convert.ToInt32((this.window as Xwt.Canvas).ParentBounds.Width),
                                    Convert.ToInt32((this.window as Xwt.Canvas).ParentBounds.Height));
                                /*   Convert.ToInt32((this.window as Xwt.Canvas).ParentBounds.Left), 
                                   Convert.ToInt32((this.window as Xwt.Canvas).ParentBounds.Top),
                                   Convert.ToInt32((this.window as Xwt.Canvas).ParentBounds.Width),
                                   Convert.ToInt32((this.window as Xwt.Canvas).ParentBounds.Height));*/

                                var state = (this as IRenderer).StartRender(null, r);

                                this.renderer.render(null, time, r);
                                (this as IRenderer).EndRender(state);
                                this.Xwt.SwapBuffers(this.window);
                            }
                        }
		           }
		           catch (Exception e)
		           {
		               //  Log.LogException(e);
		           }
		           //         }).Wait();
		       }
		       catch
		       {
		           Thread.Sleep(100);
		       }
		   }
		}

		internal void Deinterlace(IVideoFrame frame, IRenderFrame destination, DeinterlaceModes mode)
		{
		   try
		   {
		       using (var ll = this.GetDrawLock())
		       {
		           //   Lock();
		           GL.BindFramebuffer(FramebufferTarget.Framebuffer, (destination as RenderFrame).framebuffer);
		
		           var err = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
		
		           GL.Viewport(0, 0, destination.Width, destination.Height);// new Rectangle(this.window.Location,this.window.ClientSize));
		
		           GL.ClearColor(1, 1, 0, 1);
		           GL.Clear(ClearBufferMask.ColorBufferBit /*| ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit*/); // We're not using stencil buffer so why bother with clearing?            

                    GL.Disable(EnableCap.DepthTest);
                    GL.Disable(EnableCap.StencilTest);

                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, (frame as IOpenGLFrame).Textures[0]);

                    shader pp;
                    switch (mode)
                    {
                        default:
                            throw new NotImplementedException();
                        case DeinterlaceModes.Blend:
                            pp = deinterlaceblendshader;
                            break;
                        case DeinterlaceModes.Split:
                            pp = deinterlacesplitshader;
                            break;
                    }
                    pp.Bind(vertices2);
                    var locvpheight = GL.GetUniformLocation(pp, "vpHeight");
                    GL.Uniform1(locvpheight, destination.Height);


                    GL.DrawArrays(PrimitiveType.Quads, 0, 4);

                    GL.DisableVertexAttribArray(0);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                    GL.UseProgram(0);

                    //  vertices3.Bind();
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                }
            }
            catch (Exception e)
            {
                //      Log.LogException(e);
            }
		}

        IVideoFrame IRenderer.GetFrame()
        {
            return new VideoFrame(this);
        }

        IRenderFrame IRenderer.GetRenderFrame(int levels)
        {
            return new RenderFrame(this, levels);
        }


        internal void Combine(IVideoFrame[] frames, RenderFrame frame)
        {
            try
            {
                //   Lock();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, frame.framebuffer);

                GL.Viewport(0, 0, frame.Width, frame.Height);// new Rectangle(this.window.Location,this.window.ClientSize));

                GL.ClearColor(0, 0, 1, 1);
                GL.Clear(ClearBufferMask.ColorBufferBit /*| ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit*/); // We're not using stencil buffer so why bother with clearing?            

                GL.Disable(EnableCap.DepthTest);
                GL.Disable(EnableCap.StencilTest);

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, (frames[0] as IOpenGLFrame).Textures[0]);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, (frames[1] as IOpenGLFrame).Textures[0]);

                combineshader.Bind(vertices3);

                var locvpheight = GL.GetUniformLocation(this.combineshader, "vpHeight");
                GL.Uniform1(locvpheight, frame.Height);

                GL.DrawArrays(PrimitiveType.Quads, 0, 4);

                GL.DisableVertexAttribArray(0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.UseProgram(0);

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

                //  Unlock();
            }
            catch (Exception e)
            {
                //  Log.LogException(e);
            }
        }
        void IRenderer.PrepareRender()
        {
            Lock();
        }
        void IRenderer.StopRender()
        {
            Unlock();
        }

        object IRenderer.StartRender(IRenderFrame destination, rectangle r)
        {
            var oldframebuffer = (uint)GL.GetInteger(GetPName.FramebufferBinding);
            try
            {
                if (destination == null)
                {
                    GL.Viewport(r.x, r.y, r.width, r.height);// new Rectangle(this.window.Location,this.window.ClientSize));
                }
                else
                {
                    var frame = (RenderFrame)destination;

                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, frame.framebuffer);

                    GL.Viewport(0, 0, frame.Width, frame.Height);// new Rectangle(this.window.Location,this.window.ClientSize));

                }
            }
            catch (Exception e)
            {
                //      Log.LogException(e);
            }
            return oldframebuffer;// destination;
        }
        /*   void IRenderer.Paint(IRenderFrame destination, IVideoFrame src, Xwt.Rectangle dstrec)
           {
           }
           void IRenderer.Paint(IRenderFrame destination, IVideoFrame src, int index, Xwt.Rectangle dstrec)
           {
               if (src != null)
               {
                   try
                   {
                       var frame = (src as IOpenGLFrame);

                       GL.Disable(EnableCap.DepthTest);
                       GL.Disable(EnableCap.StencilTest);
                       GL.Enable(EnableCap.Blend);
                       GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

                       GL.BindTexture(TextureTarget.Texture2D, frame.Textures[index]);
                       this.presentshader.Bind(vertices3);

                       GL.DrawArrays(PrimitiveType.Quads, 0, 4);

                       GL.DisableVertexAttribArray(0);
                       GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                       GL.BindTexture(TextureTarget.Texture2D, 0);
                       GL.UseProgram(0);
                   }
                   catch (Exception e)
                   {
                       Log.LogException(e);
                   }
               }
           }*/
        void IRenderer.EndRender(object state)
        {
        //    if (state == null)
            {

            }
         //   else
            {
                try
                {
                    //     GL.Flush();
                    //    var frame = (RenderFrame)state;
                    //     frame.Save("E:\\bbr_test.png");
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)state);
                    //    Unlock();
                }
                catch (Exception e)
                {
                    //       Log.LogException(e);
                }
            }
        }
        void IRenderer.Present(IVideoFrame renderedframe, rectangle dstrec, IntPtr ctl)
        {
            try
            {
                if (!this.stopevent.WaitOne(0, false))
                {
                    if (renderedframe == null)
                    {
                     //   this.Xwt.SwapBuffers(this.window);
                    }
                    else
                    {
                        if (presentshader == null)
                        {
                            this.vertices3 = new vertices<vertex>(_vertices3);
                            this.presentshader = new shader(shadervertex, shaderfragment, vertices3);
                            // presentshader.Bind(this.vertices3);
                            this.vertices3.define("position", "position");
                            this.vertices3.define("texcoord", "texcoord");
                            GL.UseProgram(this.presentshader);
                            var pos = GL.GetUniformLocation(this.presentshader, "tex");
                            GL.Uniform1(pos, 0);
                        }
                        vertices3.Apply(this.presentshader);
                        var frame = (RenderFrame)renderedframe;

                        GL.Viewport(dstrec.x, dstrec.y, dstrec.width, dstrec.height);// new Rectangle(this.window.Location,this.window.ClientSize));

                        GL.ClearColor(1, 1, 0, 1);
                        GL.Clear(ClearBufferMask.ColorBufferBit); // We're not using stencil buffer so why bother with clearing?            

                        GL.Disable(EnableCap.DepthTest);
                        GL.Disable(EnableCap.StencilTest);

                        GL.Disable(EnableCap.Blend);
                        GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

                        GL.BindTexture(TextureTarget.Texture2D, frame.Textures[0]);

                        //  GL.DrawElements(BeginMode.Triangles,6,DrawElementsType.UnsignedInt,0)
                        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

                        GL.DisableVertexAttribArray(0);
                        GL.Disable(EnableCap.Blend);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                        GL.BindTexture(TextureTarget.Texture2D, 0);
                        GL.UseProgram(0);
                        GL.BindVertexArray(0);

                    //    this.Xwt.SwapBuffers(this.window);
                        //       GL.Flush();
                    }
                }
            }
            catch (Exception e)
            {
                //         Log.LogException(e);
            }
            finally
            {
            }
        }
        void IRenderer.AllocFunc(int width, int height, VideoFormat fmt, ref IntPtr data, ref int pitch, ref VideoFormat framefmt)
        {
            var frame = (this as IRenderer).GetFrame();
        }

        public IDisposable GetDrawLock()
        {
            return new _rendererlock(this);
        }
    }
    public class FrameFactory : IRendererFactory
    {
        internal global::OpenTK.Toolkit toolkit;
        private readonly List<IRenderer> renderers = new List<IRenderer>();
        public static Func<IntPtr> _getcurrentfunc;
        public static GraphicsContext.GetCurrentContextDelegate getcurrentfunc;

        public string Name { get => "OpenTK"; }

        //  VideoFormat AlphaFormat { get; }

        public bool OpenGLInUIThread => FrameFactory.getcurrentfunc != null;
        public bool NeedPresentThread => FrameFactory.getcurrentfunc == null && Xwt.Toolkit.CurrentEngine.Type != ToolkitType.Wpf;//only mac and wpf have auto present for gtk

        public FrameFactory(Func<IntPtr> getcurrentfunc)
        {
            try
            {
                var options = new ToolkitOptions() { EnableHighResolution = true };
                options.Backend = PlatformBackend.PreferNative;
                this.toolkit = global::OpenTK.Toolkit.Init(options);

                if (getcurrentfunc != null)
                {
                    FrameFactory._getcurrentfunc = getcurrentfunc;
                    FrameFactory.getcurrentfunc = new GraphicsContext.GetCurrentContextDelegate(() => new ContextHandle(getcurrentfunc()));
                    
                    typeof(GraphicsContext).SetFieldValuePrivateStatic("GetCurrentContext", FrameFactory.getcurrentfunc);
                }
            }
            catch (Exception e)
            {
                //    Log.LogException(e);
                throw;
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            //   (this as IRendererFactory).Close();
            this.toolkit?.Dispose();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        void IRendererFactory.Initialize()
        {
        }
        /*  IRenderer IRendererFactory.Open(object ctl, System.Drawing.Size videosize)
          {
              throw new NotImplementedException();
          }*/
        IRenderer IRendererFactory.Open(IXwtRender xwt, Canvas widget, OpenTK.IRenderOwner renderer, FPS fps, size videosize)
        {
            lock (this)
            {

                IRenderer result;
                //    if (this.renderer == null)
                {
                    result = /*this.renderer = */new OpenTKRenderer(this, xwt, widget, renderer, videosize);
                }
                //   else
                {
                    //        r = (this.renderer as OpenTKRenderer).Open(window, videosize);
                }

                renderers.Add(result);
                return result;
            }
        }
        internal void Close(IRenderer renderer)
        {
            lock (this)
            {
                renderers.Remove(renderer);
            }
        }
    }
}
