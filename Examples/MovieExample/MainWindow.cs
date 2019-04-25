using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using BaseLib.Media;
using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using BaseLib.Media.Video;
using BaseLib.Xwt;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Xwt;

namespace SimpleExample
{
    public class frameinfo : IDisposable
    {
        private Player _player;
        private IRenderer _renderer;
        private IntPtr _avframe;
        internal long _time, _duration;
        private VideoStream _video;

        public BaseLib.Media.VideoFrame Frame { get; private set; } // movieplayer
        public IVideoFrame Buffer { get; private set; } //opentk,directx

        private uint usagecnt = 1;

        /// <summary>
        /// saves values and allocates directx-buffer
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="video"></param>
        /// <param name="player"></param>
        public frameinfo(IRenderer renderer, VideoStream video, Player player)
        {
            this._player = player;
            this._renderer = renderer;
            this._video = video;
            this._avframe = IntPtr.Zero;
            this.Frame = video.AllocateFrame(this.allocfunc, this.lockfunc, this.unlockfunc); // wrapper to lock Buffer for interop
            this.Buffer = this._renderer.GetFrame();
        }
        /// <summary>
        /// saves values, but doesn't create render-buffer for frame
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="video"></param>
        /// <param name="player"></param>
        /// <param name="avframe"></param>
        /// <param name="time"></param>
        public frameinfo(IRenderer renderer, VideoStream video, Player player, IntPtr avframe, long time, long duration)
        {
            this._player = player;
            this._renderer = renderer;
            this._video = video;
            this._avframe = avframe;
            this._time = time;
            this._duration = duration;
        }
        /// <summary>
        /// saves values, but doesn't create render-buffer for frame
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="video"></param>
        /// <param name="player"></param>
        /// <param name="avframe"></param>
        /// <param name="time"></param>
        public frameinfo(IRenderer renderer, VideoStream video, IntPtr avframe, long time, long duration)
        {
            this._renderer = renderer;
            this._video = video;
            this._avframe = avframe;
            this._time = time;
            this._duration = duration;
        }
        /// <summary>
        /// fill from avframe to renderbuffer
        /// </summary>
        /// <param name="renderbuffer"></param>
        public void Update(ref frameinfo renderbuffer)
        {
            Update(ref renderbuffer, this._avframe);
        }
        /// <summary>
        /// fill from avframe to renderbuffer
        /// </summary>
        /// <param name="renderbuffer"></param>
        public void Update(ref frameinfo renderbuffer, IntPtr avframe)
        {
            if (renderbuffer == null)
            {
                renderbuffer = new frameinfo(_renderer, _video, _player);
                //this.Frame = this._video.AllocateFrame(this.allocfunc, this.lockfunc, this.unlockfunc);
                //this.Buffer = this._renderer.GetFrame();
            }
            this._video.FillFrame(renderbuffer.Frame, avframe);

            // this._lasttime = frame.Frame.Time;

            renderbuffer.Frame.Time += _video.Frame(this._player.basetime, this._player.timebase);
        }

        /*   internal IRenderFrame Deinterlace(IDocumentTracks doc, DeinterlaceModes deinterlace)
           {
               if (deinterlace == DeinterlaceModes.Auto)
               {
                   deinterlace = DeinterlaceModes.Split; // todo abc bert
               }
               if (deinterlace == DeinterlaceModes.None)
               {
                   return null;
               }
               switch (deinterlace)
               {
                   case DeinterlaceModes.Blend:
                       {
                           var _deinterlace = doc.GetRenderBuffer(this.Buffer.Width, this.Buffer.Height / 2, 1);

                           _deinterlace.Set(doc.display.Renderer.AlphaFormat);
                           _deinterlace.Set(this._time, this.Buffer.Width, this.Buffer.Height / 2, this.Buffer.Duration);

                           this.Buffer.Deinterlace(_deinterlace, deinterlace);
                           return _deinterlace;
                       }
                   case DeinterlaceModes.Split:
                       {
                           var _deinterlace = doc.GetRenderBuffer(this.Buffer.Width, this.Buffer.Height / 2, 2);
                           _deinterlace.Set(doc.display.Renderer.AlphaFormat);
                           _deinterlace.Set(this._time, this.Buffer.Width, this.Buffer.Height / 2, this.Buffer.Duration);
                           this.Buffer.Deinterlace(_deinterlace, deinterlace);
                           return _deinterlace;
                       }
               }
               throw new NotImplementedException();
           }*/

        ~frameinfo()
        {
            Debug.Assert(false);
        }
        public void Dispose()
        {
            if (--usagecnt == 0)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
        void Dispose(bool disposing)
        {
            if (this._avframe != IntPtr.Zero)
            {
                BaseLib.Media.VideoFrame.FreeAVFrame(this._avframe);
                this._avframe = IntPtr.Zero;
            }
            this.Buffer?.Dispose();
            this.Frame?.Dispose();
        }

        internal void allocfunc(IntPtr stream, long time, long duration, int width, int height, VideoFormat fmt, ref IntPtr data, ref int pitch, ref VideoFormat framefmt)
        {
            /*   switch (framefmt)
               {
                   case VideoFormat.ARGB:
                   case VideoFormat.RGBA:
                   default:
                       framefmt = _renderer.AlphaFormat;
                       break;
                   case VideoFormat.RGB:
                       break;
               }*/
            this.Buffer.Set(framefmt);
            framefmt = this.Buffer.PixelFormat;
            this.Buffer.Set(this.Frame.Time, width, height, 400000);
        }

        private void lockfunc(IntPtr stream, ref IntPtr data, ref int pitch)
        {
            this.Buffer.Lock();

            data = this.Buffer.Data;
            pitch = this.Buffer.Stride;
        }
        private void unlockfunc()
        {
            this.Buffer.Unlock();
        }
        public void Inc()
        {
            this.usagecnt++;
        }

    }
    public class Player : IDisposable
    {
        const int buffertotal = 4;

        internal long timebase, basetime;
        private IRenderer renderer;
        private MoviePlayer player;
        private VideoStream video;
        private long starttime = 0;

        private ManualResetEvent stopevent = new ManualResetEvent(false), emptyevent = new ManualResetEvent(true), readyevent = new ManualResetEvent(false), running = new ManualResetEvent(true);

        List<frameinfo> frames = new List<frameinfo>();
        private frameinfo framebuffer;

        public Player(IRenderer renderer, string filename, long timebase)
        {
            try
            {
                this.timebase = timebase;
                this.renderer = renderer;
                this.player = BaseLib.Media.MoviePlayer.Open(() => { }, filename);

                if (player.VideoStreams.Length > 0)
                {
                    this.video = player.open_video(0, frameready);
                }
                /*    if (!preview && !nosound && _player.AudioStreams.Length > 0)
                    {
                        this._audio = _player.open_audio(0, _owner.mixer, audioready);
                        this._audiobuffer = new FifoStream(_owner.audio.SampleSize * this._owner.audio.SampleRate * 3);
                        this._owner.mixer.Register(this._audiobuffer, _owner.mixer.Channels, true);
                    }*/

                this.player.start(0, timebase);
            }
            catch { }
        }
        ~Player()
        {
            Debug.Assert(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            this.framebuffer?.Dispose();
            this.framebuffer = null;
            this.player?.Dispose();
        }
        private bool frameready(IntPtr avframe, long time, long duration)
        {
            if (video.Time(time, this.timebase) < this.starttime)
            {
                return false;
            }
            if (WaitHandle.WaitAny(new WaitHandle[] { this.stopevent, this.emptyevent }, -1, false) == 1)
            {
                frameinfo frame;
                frame = new frameinfo(this.renderer, video, this, avframe, time, duration);
                lock (frames)
                {
                    frames.Add(frame);
                    this.readyevent.Set();

                    if (frames.Count >= buffertotal)
                    {
                        this.emptyevent.Reset();
                    }
                }
                return true;
            }
            return false;
        }
        public frameinfo GetFrame(long frametime, long duration)
        {
            if (WaitHandle.WaitAny(new WaitHandle[] { this.stopevent, this.running }, 0, false) == 1)
            {
                while (WaitHandle.WaitAny(new WaitHandle[] { this.stopevent, this.readyevent }, -1, false) == 1)
                {
                    lock (frames)
                    {
                        while (frames.Count > 1 && video.Time(frames[0]._time + 1, this.timebase) - 1 <= frametime)
                        {
                            this.frames[0].Dispose();
                            this.frames.RemoveAt(0);
                            this.emptyevent.Set();
                        }
                        var frame = this.frames.First();
                        if (video.Time(frame._time + 1, this.timebase) < frametime)
                        {
                            this.readyevent.Reset();
                            continue;
                        }
                        frame.Update(ref this.framebuffer); // avframe->texture, set time
                        this.framebuffer.Inc();
                        return this.framebuffer;
                    }
                }
            }
            return null;
        }
        internal void Stop()
        {
            this.player.preparestop();
          //  this._audiobuffer?.Close();
            this.stopevent.Set();
            this.player.stop();
            foreach (var f in this.frames)
            {
                f.Dispose();
            }
            this.frames.Clear();
        }
    }
    public class MainWindow : Window
    {
        private Canvas3D Canvas => this.Content as Canvas3D;

        public IRendererFactory Renderfactory { get; }
        public IXwtRender XwtRender { get; }
        public IXwt Xwt { get; }

        class Canvas3D : Canvas, IRenderOwner
        {
            const long TimeBase = 10000000L;

            [StructLayout(LayoutKind.Explicit, Size = 4 * 3, CharSet = CharSet.Ansi)]
            struct vertex
            {
                public vertex(Vector3 pos)
                {
                    this.pos = pos;
                }
                [FieldOffset(0)]
                public Vector3 pos;
            }
            [StructLayout(LayoutKind.Explicit, Size = 4 * 3, CharSet = CharSet.Ansi)]
            struct vertex_tex
            {
                public vertex_tex(Vector3 pos, Vector2 texture0)
                {
                    this.pos = pos;
                    this.tex0 = texture0;
                }
                [FieldOffset(0)]
                public Vector3 pos;
                [FieldOffset(sizeof(float)*3)]
                public Vector2 tex0;
            }

            private readonly IRendererFactory RenderFactory;
            private readonly IXwtRender XwtRender;
            private readonly IXwt Xwt;

            private IRenderer Renderer;
            private Player movie;
            private vertices<vertex> vertices;
            private vertices<vertex_tex> verticestex;
            private shader shader, shadertex;

            private int test;
            private long timebase;

            public Canvas3D(MainWindow window)
            {
                this.RenderFactory = window.Renderfactory;
                this.XwtRender = window.XwtRender;
                this.Xwt = window.Xwt;
            }
            internal void OnLoaded()
            {
                this.Renderer = this.RenderFactory.Open(this.XwtRender, this, this, new size(1920, 1080));

                this.movie = new Player(this.Renderer, @"e:\movies\Yamaha_final.avi", TimeBase);

                using (var lck = this.Renderer.GetDrawLock())
                {

                    /*    List<Vector3> simpleVertices = new List<Vector3>();
                        simpleVertices.Add(new Vector3(0, 0, 0));
                        simpleVertices.Add(new Vector3(100, 0, 0));
                        simpleVertices.Add(new Vector3(100, 100, 0));*/

                    this.vertices = new vertices<vertex>(
                        new vertex[] { new vertex(new Vector3(0, -1, 0)), new vertex(new Vector3(-1, 1, 0)), new vertex(new Vector3(1, 1, 0)) });

                    vertices.define("position", "pos");

                    this.shader = new shader(
        @"#version 150 core

in vec4 position;
void main()
{
gl_Position = position;
}",
         @"#version 150 core
precision mediump float;

out vec4 outColor;

void main()
{
    outColor = vec4(1,0,0,1);
}
",
                             this.vertices);


                    this.verticestex = new vertices<vertex_tex>(
                        new vertex_tex[] {
                            new vertex_tex(new Vector3(0, 0, 0), new Vector2(0,0)),
                            new vertex_tex(new Vector3(1, 0, 0), new Vector2(1,0)),
                            new vertex_tex(new Vector3(0, 1, 0), new Vector2(0,1)),

                            new vertex_tex(new Vector3(1, 0, 0), new Vector2(1,0)),
                            new vertex_tex(new Vector3(0, 1, 0), new Vector2(0,1)),
                            new vertex_tex(new Vector3(1, 1, 0), new Vector2(1,1)),
                        });

                    this.shadertex = new shader(
        @"#version 150 core

in vec4 position;
in vec2 texcoord0;

out vec2 Texcoord0;

void main()
{
gl_Position = position;
Texcoord0 = texcoord0;
}",
         @"#version 150 core
precision mediump float;

in vec2 Texcoord0;

uniform sampler2D texure0;

out vec4 outColor;

void main()
{
     outColor = texture(texure0, Texcoord0);
}
",
                             this.verticestex);
                    verticestex.define("position", "pos");
                    verticestex.define("texcoord0", "tex0");

                    GL.UseProgram(this.shadertex);
                    var pos = GL.GetUniformLocation(this.shadertex, "texture0");
                    GL.Uniform1(pos, 0);
                }
                this.timebase = DateTime.Now.Ticks;
                this.Renderer.Start();

                //this.movie.p

            }

            internal void OnUnloading()
            {
                if (this.Renderer != null)
                {
                    this.Renderer.Stop();
                    using (var lck = this.Renderer.GetDrawLock())
                    {
                        this.shader?.Dispose();
                        this.vertices?.Dispose();
                    }
                    this.movie?.Dispose();
                    this.movie = null;
                    this.Renderer.Dispose();
                    this.Renderer = null;
                }
            }

            void IRenderOwner.DoEvents()
            {
                this.Xwt.DoEvents();
            }
            void IRenderOwner.EndRender(IRenderer renderer)
            {
                this.XwtRender.EndRender(renderer, this);
            }
            bool IRenderOwner.preparerender(IRenderFrame destination, bool dowait)
            {
                return true;
            }
            void IRenderOwner.render(IRenderFrame destination, Rectangle r)
            {
                var state = this.Renderer.StartRender(destination, r);

                var c = (test++ % 25) / 25f;
                var cc = new Xwt.Drawing.Color(c, c, c, 255);

                GL.ClearColor((float)cc.Red, (float)cc.Green, (float)cc.Blue, (float)cc.Alpha);
                GL.Clear(ClearBufferMask.ColorBufferBit/*ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit*/); // We're not using stencil buffer so why bother with clearing?            

                this.verticestex.Apply(this.shadertex);

                var time = DateTime.Now.Ticks- this.timebase;
                var frame = this.movie.GetFrame(time, 0);

                if (frame != null)
                {
                    GL.BindTexture(TextureTarget.Texture2D, (frame.Buffer as IOpenGLFrame).Textures[0]);

               // this.vertices.Apply(this.shader);

                    GL.DrawArrays(PrimitiveType.Triangles, 0, 6); // Starting from vertex 0; 3 vertices total -> 2 triangle
                    GL.DisableVertexAttribArray(0);
                }
                this.Renderer.EndRender(state);

                this.Renderer.Present(destination, r, IntPtr.Zero);
            }
            void IRenderOwner.StartRender(IRenderer renderer)
            {
                this.XwtRender.StartRender(renderer, this);
            }
        }
        public MainWindow(IRendererFactory renderfactory, IXwtRender xwtrender, IXwt xwt)
        {
            this.Renderfactory = renderfactory;
            this.XwtRender = xwtrender;
            this.Xwt = xwt;

            this.Content = new Canvas3D(this)
            {
                MinWidth = 100,
                MinHeight = 100,
                HorizontalPlacement = WidgetPlacement.Fill,
                VerticalPlacement = WidgetPlacement.Fill,
                ExpandHorizontal = true,
                ExpandVertical = true
            };
        }
        protected override void OnShown()
        {
            base.OnShown();

            this.Canvas.OnLoaded();
        }
        protected override bool OnCloseRequested()
        {
            this.Canvas.OnUnloading();
            return true;// base.OnCloseRequested();
        }
        protected override void OnClosed()
        {
            base.OnClosed();
            Application.Exit();
        }
    }
}