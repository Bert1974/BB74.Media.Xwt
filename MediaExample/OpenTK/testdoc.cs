using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using BaseLib.Xwt;
using BaseLib.Xwt.Controls.DockPanel;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Xwt;
using Xwt.Drawing;

namespace DockExample.OpenTK
{
    public class opentkdoc : Canvas, IDockContent, IDockDocument, IDockNotify, IDockSerializable
    {
        const Int64 TimeBase = 10000000L;

        private readonly IXwtRender xwtrender;
        private readonly IXwt xwt;
        private readonly IRendererFactory factory;
        private OpenTK.IWxtDisplay xwtdisplay;

        // private Thread thread;

        Widget IDockContent.Widget => this;

        public string TabText => "testdoc";

        public IDockPane DockPane { get; set; }

        public opentkdoc(IRendererFactory factory, IXwtRender xwtrender, IXwt xwt)
        {
            this.xwtrender = xwtrender;
            this.xwt = xwt;
            this.factory = factory;

            base.BackgroundColor = Colors.Black;
            base.MinWidth = base.MinHeight = 100;
        }
        void IDockNotify.OnLoaded(IDockPane pane)
        {
            Debug.Assert(this.xwtdisplay == null);

            this.xwtdisplay = new OpenTK.XwtRender(this, this.xwtrender,this.xwt, TimeBase);
            this.xwtdisplay.FrameRenderer = new MovieRender(this);
            this.xwtdisplay.Initialize(this.factory, this.xwtrender, new Xwt.Size(1920, 1080));
            this.xwtdisplay.Play(0);
        }
        void IDockNotify.OnUnloading()
        {
            this.xwtdisplay?.Dispose();
            this.xwtdisplay = null;
        }

        string IDockSerializable.Serialize()
        {
            return "";
        }

        class MovieRender : OpenTK.MovieRender
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
            int test = 0;
            private vertices<vertex> vertices;
            private shader shader;

            public MovieRender(opentkdoc doc)
                : base(doc)
            {
            }
            protected override void Dispose(bool disposing)
            {
                using (var lck = this.Display.Renderer.GetDrawLock())
                {
                    this.shader?.Dispose();
                    this.shader = null;
                    this.vertices?.Dispose();
                    this.vertices = null;
                }
                base.Dispose(disposing);
            }
            public override void Initialize(size videosize, long timebase)
            {
                base.Initialize(videosize, timebase);

                using (var lck = this.Display.Renderer.GetDrawLock())
                {
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
            }
            public override void Stop()
            {
                base.Stop();
            }
            public override void Render(IRenderFrame frame, long videotime)
            {
                var c = (test++ % 25) / 25f;

                var cc = new Xwt.Drawing.Color(c, c, c, 255);

                // render
                var state = this.Display.Renderer.StartRender(frame, Rectangle.Zero);

                GL.ClearColor((float)cc.Red, (float)cc.Green, (float)cc.Blue, (float)cc.Alpha);
                GL.Clear(ClearBufferMask.ColorBufferBit/*ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit*/); // We're not using stencil buffer so why bother with clearing?            

                GL.Disable(EnableCap.DepthTest);
                //     GL.Disable(EnableCap.Lighting);

                //          ES30.GL.Enable(OpenTK.Graphics.ES30.EnableCap.DepthTest);
                //        ES30.GL.Enable(ES30.EnableCap.Blend);
                //        ES30.GL.BlendFunc(ES30.BlendingFactorSrc.SrcAlpha, ES30.BlendingFactorDest.OneMinusSrcAlpha);

                GL.Disable(EnableCap.StencilTest);

                /* GL.UseProgram(shaderProgram);
                 GL.BindBuffer(BufferTarget.ArrayBuffer, buf_vertices);

                 GL.DrawArrays(PrimitiveType.Triangles, 0, 3);*/

                this.vertices.Apply(this.shader);

                GL.DrawArrays(PrimitiveType.Triangles, 0, 3); // Starting from vertex 0; 3 vertices total -> 1 triangle
                GL.DisableVertexAttribArray(0);

                //  return destination;
                this.Display.Renderer.EndRender(state);
            }
        }

    }
  /*  internal static class Extensions
    {
        public static object InvokeStatic(this Type type, string method, params object[] arguments)
        {
            return type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, arguments);
        }

        public static object Invoke(this Type type, object instance, string method, params object[] arguments)
        {
            return type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, arguments);
        }
        public static object GetPropertyValue(this Type type, object instance, string propertyname)
        {
            return type.GetProperty(propertyname, BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty).GetValue(instance, new object[0]);
        }
        public static object GetPropertyValueStatic(this Type type, string propertyname)
        {
            return type.GetProperty(propertyname, BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty).GetValue(null, new object[0]);
        }
    }*/
}

