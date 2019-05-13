using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using BaseLib.Media;
using BaseLib.Media.Audio;
using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using BaseLib.Media.Video;
using BaseLib.Xwt;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Xwt;

namespace MovieExample
{
    public class MainWindow : Window
    {
        private Canvas3D Canvas => this.Content as Canvas3D;

        public IRendererFactory RenderFactory { get; }
        public IXwtRender XwtRender { get; }
        public IXwt Xwt { get; }

        public IRenderer Renderer => Canvas.Renderer;
        public AudioOut Audio => Canvas.Audio;
        public IMixer Mixer => Canvas.Mixer;

        internal class Canvas3D : Canvas, IRenderOwner
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

            public readonly MainWindow owner;
            private readonly IRendererFactory RenderFactory;
            private readonly IXwtRender XwtRender;
            private readonly IXwt Xwt;

            private Player MoviePlayer;
            private vertices<vertex> vertices;
            private vertices<vertex_tex> verticestex;
            private shader shader, shadertex;

            private int test;
            private frameinfo frame;
            private Thread audiothread;
            private ManualResetEvent audiostop = new ManualResetEvent(false);

            public IRenderer Renderer { get; internal set; }
            public AudioOut Audio { get; internal set; }
            public IMixer Mixer { get; internal set; }

            public Canvas3D(MainWindow window)
            {
                this.owner = window;
                this.RenderFactory = window.RenderFactory;
                this.XwtRender = window.XwtRender;
                this.Xwt = window.Xwt;
            }
            internal void OnLoaded()
            {
                try
                {
                    this.Renderer = this.RenderFactory.Open(this.XwtRender, this, this, new size(1920, 1080));

                    this.Audio = new AudioOut(48000, AudioFormat.Float32, ChannelsLayout.Stereo, 2);
                    this.Mixer = new Mixer(this.Audio.SampleRate, this.Audio.Format, this.Audio.ChannelLayout);

                    try
                    {
                        if (BaseLib.Xwt.Platform.OSPlatform == PlatformID.MacOSX)
                        {
                            this.MoviePlayer = new Player(this.owner, @"/Volumes/Projects/movies/Yamaha_final.avi", TimeBase);
                        }
                        else if (BaseLib.Xwt.Platform.OSPlatform == PlatformID.Unix)
                        {
                            this.MoviePlayer = new Player(this.owner, @"/home/bert/Projects/movies/Yamaha_final.avi", TimeBase);
                        }
                        else
                        {
                            this.MoviePlayer = new Player(this.owner, @"e:\movies\Yamaha_final.avi", TimeBase);
                        }
                    }
                    catch { }
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
                            new vertex_tex(new Vector3(-1, -1, 0), new Vector2(0,0)),
                            new vertex_tex(new Vector3(1, -1, 0), new Vector2(1,0)),
                            new vertex_tex(new Vector3(-1, 1, 0), new Vector2(0,1)),

                            new vertex_tex(new Vector3(1, -1, 0), new Vector2(1,0)),
                            new vertex_tex(new Vector3(-1, 1, 0), new Vector2(0,1)),
                            new vertex_tex(new Vector3(1, 1, 0), new Vector2(1,1)),
                            });

                        this.shadertex = new shader(
            @"#version 150 core

in vec4 position;
in vec2 texcoord0;

out vec2 Texcoord0;

void main()
{
gl_Position = position*vec4(1,-1,1,1);
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


                    //this.Display.WaitBuffered();
                    //    this.Audio?.Buffered.WaitOne(-1, false);

                    this.audiostop.Reset();
                    this.audiothread = new Thread(() =>
                      {
                          while (!audiostop.WaitOne(0, false))
                          {
                              try
                              {
                                  var data = this.Mixer.Read(0, 48000 / 25);
                                  Audio.Write(data, data.Length / 8);
                              }
                              catch { }
                          }
                      });

                    this.audiothread.Start();

                    this.Audio.Buffered.WaitOne(-1, false);

                    this.Audio?.Start();
                    this.Renderer.Start();

                    //this.movie.p
                }
                catch (Exception e)
                {
                    throw;
                }
            }

            internal void OnUnloading()
            {
                if (this.Renderer != null)
                {
                    this.audiostop.Set();
                    this.Renderer.Stop();
                    this.Audio?.Stop();
                    this.MoviePlayer?.Stop();
                    try { this.audiothread.Join(); } catch { }

                    this.MoviePlayer?.Dispose();
                    this.MoviePlayer = null;
                    this.Mixer?.Dispose();
                    this.Mixer = null;
                    this.Audio?.Dispose();
                    this.Audio = null;

                    using (var lck = this.Renderer.GetDrawLock())
                    {
                        this.shader?.Dispose();
                        this.vertices?.Dispose();
                        this.shadertex?.Dispose();
                        this.verticestex?.Dispose();
                    }
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
            bool IRenderOwner.preparerender(IRenderFrame destination, long time, bool dowait)
            {
                this.frame = this.MoviePlayer?.GetFrame(time, 0);

                return frame!=null;
            }
            void IRenderOwner.render(IRenderFrame destination, long time, Rectangle r)
            {
              // var state = this.Renderer.StartRender(destination, r);

                var c = (test++ % 25) / 25f;
                var cc = new Xwt.Drawing.Color(c, c, c, 255);

                GL.ClearColor((float)cc.Red, (float)cc.Green, (float)cc.Blue, (float)cc.Alpha);
                GL.Clear(ClearBufferMask.ColorBufferBit/*ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit*/); // We're not using stencil buffer so why bother with clearing?            

                this.verticestex.Apply(this.shadertex);

                if (frame != null)
                {
                    GL.BindTexture(TextureTarget.Texture2D, (frame.Buffer as IOpenGLFrame).Textures[0]);

               // this.vertices.Apply(this.shader);

                    GL.DrawArrays(PrimitiveType.Triangles, 0, 6); // Starting from vertex 0; 3 vertices total -> 2 triangle
                    GL.DisableVertexAttribArray(0);

                    frame.Dispose();
                    frame = null;
                }
              //  this.Renderer.EndRender(state);

                this.Renderer.Present(destination, r, IntPtr.Zero);
            }
            void IRenderOwner.StartRender(IRenderer renderer)
            {
                this.XwtRender.StartRender(renderer, this);
            }
        }
        public MainWindow(IRendererFactory renderfactory, IXwtRender xwtrender, IXwt xwt)
        {
            this.RenderFactory = renderfactory;
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

        //    this.Xwt.SetCapture(this.Content);

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