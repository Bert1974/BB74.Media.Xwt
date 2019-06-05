using OpenTK.Graphics.OpenGL4;
using System;
using System.Diagnostics;

namespace BaseLib.Media.OpenTK
{
    /* public static class Extensions
     {
         public static void CheckShaderLog(this int shader)
         {
             var log = GL.GetShaderInfoLog(shader);
             if (log.Length > 0)
             {
                 throw new Exception(log);
             }
         }
     }*/

    public class vertices : IDisposable
    {
        public int vao, buf_vertices;
        public static implicit operator int(vertices v) => v.buf_vertices;

        protected vertices()
        {
        }
        public vertices(float[] data)
        {
            GL.GenVertexArrays(1, out this.vao);
            GL.BindVertexArray(this.vao);

            GL.GenBuffers(1, out this.buf_vertices); // Generate 1 buffer
            GL.BindBuffer(BufferTarget.ArrayBuffer, buf_vertices);

            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * data.Length, data, BufferUsageHint.StaticDraw);

            //GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        }
        ~vertices()
        {
            Debug.Assert(false);
        }
        public virtual void Dispose()
        {
            GL.DeleteVertexArray(this.vao);
            GL.DeleteBuffers(1, ref buf_vertices);
            GC.SuppressFinalize(this);
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

    public class shader : IDisposable
    {
        private readonly vertices vertices;
        public readonly int vertexShader, fragmentShader, shaderProgram;

        private int pos1, pos2;

        public static implicit operator int (shader s)=>s.shaderProgram;

        public shader(string vertexshader, string fragmentshader, vertices vertices= null)
        {
            this.vertices = vertices;

            vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexshader);
            GL.CompileShader(vertexShader);
            vertexShader.CheckShaderLog();

            fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentshader);
            GL.CompileShader(fragmentShader);
            fragmentShader.CheckShaderLog();

            shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);

            GL.LinkProgram(shaderProgram);

            shaderProgram.GetProgramInfoLog();

          /*  GL.GetProgramiv(ProgramID, GL_LINK_STATUS, &Result);
          //  GL.(ProgramID, GL_INFO_LOG_LENGTH, &InfoLogLength);
            if (InfoLogLength > 0)
            {
                std::vector<char> ProgramErrorMessage(InfoLogLength+1);
                glGetProgramInfoLog(ProgramID, InfoLogLength, NULL, &ProgramErrorMessage[0]);
                printf("%s\n", &ProgramErrorMessage[0]);
            }
            */
            int outcolor = GL.GetFragDataLocation(shaderProgram, "outColor");

         //   Debug.Assert(outcolor == 0);

            //       int outcolor = GL.GetFragDataLocation(shaderProgram, "outColor");

         //   GL.GenVertexArrays(1, out this.vao);
     //       GL.BindVertexArray(vertices.vao);

      //      GL.UseProgram(this.shaderProgram);
          //  GL.BindBuffer(BufferTarget.ArrayBuffer, (int)vertices);

            this.pos1 = GL.GetAttribLocation(shaderProgram, "position");
            this.pos2 = GL.GetAttribLocation(shaderProgram, "texcoord");
            // GL.BindVertexArray(vao);

        //    GL.UseProgram(0);
        }
        public void Bind()
        {
            if (this.vertices != null)
            {
                Bind(this.vertices);
            }
        }
        public void Bind(vertices vertices)
        {
            GL.BindVertexArray(vertices.vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, (int)vertices);
                
            if (pos1 != -1)
            {
                GL.VertexAttribPointer(pos1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
                GL.EnableVertexAttribArray(pos1);
            }
            if (pos2 != -1)
            {
                GL.VertexAttribPointer(pos2, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
                GL.EnableVertexAttribArray(pos2);
            }
            GL.UseProgram((int)this);
        }
        ~shader()
        {
            Debug.Assert(false);
        }
        public void Dispose()
        {
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteProgram(shaderProgram);

            GC.SuppressFinalize(this);
        }
    }
}
