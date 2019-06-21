using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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
    public class MainWindow : Window
    {
        private Canvas3D Canvas => this.Content as Canvas3D;

        public IRendererFactory Renderfactory { get; }
        public IXwtRender XwtRender { get; }
        public IXwt Xwt { get; }

        class Canvas3D : Canvas, IRenderOwner
        {
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

            private readonly IRendererFactory RenderFactory;
            private readonly IXwtRender XwtRender;
            private readonly IXwt Xwt;

            private IRenderer Renderer;
            private vertices<vertex> vertices;
            private shader shader;

            private int test;

            public Canvas3D(MainWindow window)
            {
                this.RenderFactory = window.Renderfactory;
                this.XwtRender = window.XwtRender;
                this.Xwt = window.Xwt;

                base.BackgroundColor = global::Xwt.Drawing.Colors.DarkGreen;
                base.MinWidth = base.MinHeight = 100;
            }
            internal void OnLoaded()
            {
                this.Renderer = this.RenderFactory.Open(this.XwtRender, this, this, new FPS(1,25,true), new size(1920, 1080));

                using (var lck = this.Renderer.GetDrawLock())
                {

                    /*    List<Vector3> simpleVertices = new List<Vector3>();
                        simpleVertices.Add(new Vector3(0, 0, 0));
                        simpleVertices.Add(new Vector3(100, 0, 0));
                        simpleVertices.Add(new Vector3(100, 100, 0));*/

                    this.vertices = new vertices<vertex>(
                        new vertex[] { new vertex(new Vector3(0, -1, 0)), new vertex(new Vector3(-1, 1, 0)), new vertex(new Vector3(1, 1, 0)) });

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
    outColor = vec4(0,1,0,1);
}
",
                             this.vertices);

                    vertices.define("position", "pos");

                }
                this.Renderer.Start();
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
                    this.Renderer.Dispose();
                    this.Renderer = null;
                }
                }

            void IRenderOwner.DoEvents(Func<bool> cancenlfunc)
            {
                this.Xwt.DoEvents();
            }
            void IRenderOwner.EndRender(IRenderer renderer)
            {
                this.XwtRender.EndRender(renderer, this);
            }
            bool IRenderOwner.preparerender(IRenderFrame destination, long time, bool dowait)
            {
                return true;
            }
            void IRenderOwner.render(IRenderFrame destination, long time, rectangle r)
            {
          //      var state = this.Renderer.StartRender(destination, r);
                
                var c = (test++ % 25) / 25f;
                var cc = new Xwt.Drawing.Color(c, c, c, 255);

                GL.ClearColor((float)cc.Red, (float)cc.Green, (float)cc.Blue, (float)cc.Alpha);
                GL.Clear(ClearBufferMask.ColorBufferBit/*ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit*/); // We're not using stencil buffer so why bother with clearing?            

                this.vertices.Apply(this.shader);

                GL.DrawArrays(BeginMode.Triangles, 0, 3); // Starting from vertex 0; 3 vertices total -> 1 triangle
                GL.DisableVertexAttribArray(0);

          //      this.Renderer.EndRender(state);

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
