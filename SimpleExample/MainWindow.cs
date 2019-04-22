using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Xwt;

namespace SimpleExample
{
    class MainWindow : Window
    {
        private Canvas3D Canvas => this.Content as Canvas3D;

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

            private IRenderer Renderer;
            private vertices<vertex> vertices;
            private shader shader;

            private int test;

            internal void OnLoaded()
            {
                this.Renderer = Program.RenderFactory.Open(Program.XwtRender, this, this, new Xwt.Size(1920, 1080));

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
                }
                this.Renderer.Start();
            }

            internal void OnUnloding()
            {
                this.Renderer?.Stop();
                using (var lck = this.Renderer.GetDrawLock())
                {
                    this.shader?.Dispose();
                    this.vertices?.Dispose();
                }
                this.Renderer?.Dispose();
                this.Renderer = null;
            }

            void IRenderOwner.DoEvents()
            {
                Program.Xwt.DoEvents();
            }
            void IRenderOwner.EndRender(IRenderer renderer)
            {
                Program.XwtRender.EndRender(renderer, this);
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

                this.vertices.Apply(this.shader);

                GL.DrawArrays(PrimitiveType.Triangles, 0, 3); // Starting from vertex 0; 3 vertices total -> 1 triangle
                GL.DisableVertexAttribArray(0);

                this.Renderer.EndRender(state);

                this.Renderer.Present(destination, r, IntPtr.Zero);
            }
            void IRenderOwner.StartRender(IRenderer renderer)
            {
                Program.XwtRender.StartRender(renderer, this);
            }
        }
        public MainWindow()
        {
            this.Content = new Canvas3D()
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
            this.Canvas.OnUnloding();
            return true;// base.OnCloseRequested();
        }
        protected override void OnClosed()
        {
            base.OnClosed();
            Application.Exit();
        }
    }
}
