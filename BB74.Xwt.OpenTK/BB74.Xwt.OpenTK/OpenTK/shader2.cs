using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BaseLib.Media.OpenTK
{
    public class vertices<T> : vertices
        where T : struct
    {
        class i
        {
            internal int fi;
            internal int len;
        }
        Dictionary<string, i> p = new Dictionary<string, i>();
        
        public static implicit operator int(vertices<T> v) => v.buf_vertices;

        public vertices(T[] data)
        {
            GL.GenVertexArrays(1, out this.vao);
            GL.BindVertexArray(this.vao);

            GL.GenBuffers(1, out this.buf_vertices); // Generate 1 buffer
            GL.BindBuffer(BufferTarget.ArrayBuffer, buf_vertices);

            GL.BufferData(BufferTarget.ArrayBuffer, Marshal.SizeOf(typeof(T)) * data.Length, data, BufferUsageHint.StaticDraw);

            //GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        }
        public void define(string key, string fld)
        {
            var fi = typeof(T).GetField(fld);

            if (fi != null)
            {
                this.p[key] = new i()
                {
                    len = Marshal.SizeOf(fi.FieldType),
                    fi = (fi.GetCustomAttributes(typeof(FieldOffsetAttribute), true)[0] as FieldOffsetAttribute).Value
                };
            }
        }
        ~vertices()
        {
            Debug.Assert(false);
        }
        public override void Dispose()
        {
            base.Dispose();
        }

        public void Apply(shader shader)
        {
            GL.BindVertexArray(this.vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, (int)this);
            GL.UseProgram((int)shader);

            foreach (var kv in this.p)
            {
                if (kv.Value.fi >= 0)
                {
                    var pos1 = GL.GetAttribLocation((int)shader, kv.Key);

                    if (pos1 != -1)
                    {
                        GL.VertexAttribPointer(pos1, kv.Value.len / sizeof(float), VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(T)), kv.Value.fi);
                        GL.EnableVertexAttribArray(pos1);
                    }
                }
            }
        }
     /*   internal void Bind(shader shader)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, buf_vertices);
            GL.BindVertexArray(shader.vao);

            var pos = GL.GetAttribLocation(shader, "position");
            GL.VertexAttribPointer(pos, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            pos = GL.GetAttribLocation(shader, "texcoord");
            GL.VertexAttribPointer(pos, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);

            GL.UseProgram(shader);
        }*/
    }
    
}
