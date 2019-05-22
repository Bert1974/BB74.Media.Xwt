using BaseLib.Media;
using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using BaseLib.Media.Video;
using BaseLib.Xwt;
using BaseLib.Xwt.Controls.DockPanel;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Xwt;
using Xwt.Drawing;

namespace DockExample
{
    public class opentkdoc2 : Canvas, IDockContent, IDockDocument, IDockNotify, IRenderOwner, IDockSerializable
    {
        const Int64 TimeBase = 10000000L;

        private readonly IXwtRender xwtrender;
        private readonly IXwt xwt;
        private readonly IRendererFactory factory;
        private IRenderer Renderer;
        private vertices<vertex> vertices;
        int test;
        private shader shader;


        // private Thread thread;

        Widget IDockContent.Widget => this;

        public string TabText => "opentk2";

        public IDockPane DockPane { get; set; }

        public opentkdoc2(IRendererFactory factory, IXwtRender xwtrender, IXwt xwt)
        {
            this.xwtrender = xwtrender;
            this.xwt = xwt;
            this.factory = factory;

            base.BackgroundColor = Colors.Black;
            base.MinWidth = base.MinHeight = 100;
        }
        void IDockNotify.OnLoaded(IDockPane pane)
        {
            Debug.Assert(this.Renderer == null);

            this.Renderer = factory.Open(xwtrender, this, this, new FPS(1,25,true), new size(1920, 1080));

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
    outColor = vec4(1,0,0,1);
}
",
                         this.vertices);

                vertices.define("position", "pos");
            }
            this.Renderer.Start();
        }

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

        void IDockNotify.OnUnloading()
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
            this.xwt.DoEvents();
        }
        bool IRenderOwner.preparerender(IRenderFrame destination, long time, bool dowait)
        {
            return true;
        }

        void IRenderOwner.render(IRenderFrame destination, long time, rectangle r)
        {
     //       using (var lck = this.Renderer.GetDrawLock())
            {
          //      var state = this.Renderer.StartRender(destination, r);

             //   GL.MatrixMode(MatrixMode.Projection);
             //   GL.LoadIdentity();

                //   this.xwt.StartRender(renderer, this);

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
        }

        void IRenderOwner.StartRender(IRenderer renderer)
        {
            //   renderer.EndRender(state);
            this.xwtrender.StartRender(renderer, this);
        }

        void IRenderOwner.EndRender(IRenderer renderer)
        {
            this.xwtrender.EndRender(renderer, this);
        }

        string IDockSerializable.Serialize()
        {
            return "";
        }
    }
}

