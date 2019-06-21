using BaseLib.Media.Display;
using BaseLib.Media.Video;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BaseLib.Media.OpenTK
{
    public class RenderFrame : IRenderFrame, IOpenGLFrame
    {
        internal int framebuffer = 0, depthBuffer = 0;
        private int[] textureColorBuffer = new int[0];
        private IRenderer owner;
        private byte[] _data;
        private GCHandle _gh;

        internal RenderFrame(IRenderer owner, int levels)
        {
            this.owner = owner;
            this.textureColorBuffer = new int[levels];
        }
        ~RenderFrame()
        {
            Debug.Assert(false);
            //Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            using (var ll = owner.GetDrawLock())
            {
                GL.DeleteFramebuffers(1, ref framebuffer);
                GL.DeleteTextures(this.textureColorBuffer.Length, this.textureColorBuffer);
                GL.DeleteTexture(this.depthBuffer);
            }
        }
        public long Time { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public long Duration { get; private set; }
        public VideoFormat PixelFormat { get; private set; }

        public IntPtr Data { get; private set; }
        public int Stride { get; private set; }

        void IVideoFrame.Set(VideoFormat pixfmt)
        {
            switch (pixfmt)
            {
                default:
                case VideoFormat.RGBA:
                    this.PixelFormat = VideoFormat.RGBA;
                    break;
                /*case BaseLib.Video.VideoFormat.ARGB:
                    throw new NotImplementedException();
                default:
                    this.PixelFormat = BaseLib.Video.VideoFormat.RGB;
                    break;*/
            }
        }
        bool IVideoFrame.Set(Int64 time, int width, int height, Int64 duration)
        {
            this.Time = time;
            this.Duration = duration;

            if (framebuffer == 0)
            {
                this.Width = width; this.Height = height;

                using (var ll =  owner.GetDrawLock())
                {
                    this.framebuffer = GL.GenFramebuffer();

                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.framebuffer);

                    GL.GenTextures(textureColorBuffer.Length, textureColorBuffer);
                    for (int nit = 0; nit < this.textureColorBuffer.Length; nit++)
                    {
                        // create a RGBA color texture
                        GL.BindTexture(TextureTarget.Texture2D, textureColorBuffer[nit]);

                        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                                            width, height,
                                            0, global::OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte,
                                            IntPtr.Zero);

                        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
                        GL.BindTexture(TextureTarget.Texture2D, 0);
                    }                    // create a RGBA color texture for normals
                    // create a depth texture
                    GL.GenTextures(1, out depthBuffer);
                    GL.BindTexture(TextureTarget.Texture2D, depthBuffer);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Depth24Stencil8,
                                        width, height,
                                        0, global::OpenTK.Graphics.OpenGL4.PixelFormat.DepthStencil, PixelType.UnsignedInt248,
                                        IntPtr.Zero);
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                  //gl.renderbufferStorage(gl.RENDERBUFFER, gl.DEPTH_COMPONENT16, targetTextureWidth, targetTextureHeight);
                   // gl.framebufferRenderbuffer(gl.FRAMEBUFFER, gl.DEPTH_ATTACHMENT, gl.RENDERBUFFER, depthBuffer);
                    var l = new List<DrawBuffersEnum>();

                    for (int nit = 0; nit < this.textureColorBuffer.Length; nit++)
                    {
                        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + nit, TextureTarget.Texture2D, textureColorBuffer[nit], 0);
                        l.Add(DrawBuffersEnum.ColorAttachment0 + nit);
                    }
//                    GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, textureColorBuffer[1], 0);
                    GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, depthBuffer, 0);

                  //  DrawBuffersEnum[] bufs = new DrawBuffersEnum[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 };
                    GL.DrawBuffers(l.Count, l.ToArray());

                   var err= GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

                    var error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

                }
            }
            return framebuffer != 0;
        }
        
        public void Lock()
        {
            Debug.Assert(this.textureColorBuffer.Length == 1);

            this._data = new byte[this.Width * 4 * this.Height];
            this._gh = GCHandle.Alloc(this._data, GCHandleType.Pinned);

            this.Stride = this.Width * 4;
            this.Data = this._gh.AddrOfPinnedObject();

            using (var ll =  owner.GetDrawLock())
            {
                uint pbo;
                GL.GenBuffers(1, out pbo);
                GL.BindBuffer(BufferTarget.CopyWriteBuffer, pbo);

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.framebuffer);

                GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
                GL.ReadPixels(0, 0, Width, Height, global::OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, this._gh.AddrOfPinnedObject()); 

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

                GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
                GL.DeleteBuffer(pbo);
            }
        }

        public void Unlock()
        {
            this._gh.Free();
            this._data = null;
        }
        public void Combine(IVideoFrame[] frames)
        {
        //    owner.Combine(frames, this);

            /*    this.owner.StartRender(this, (Color?)null);

                try
                {
                    var dstrec = new System.Drawing.Rectangle(0, 0, this.width, this.height);
                    var effect = this.owner.effect3;
                    var m = Matrix.Scaling(dstrec.Width, dstrec.Height, 1) * Matrix.Translation(dstrec.Left, dstrec.Top, 0);
                    var worldViewProj = m * this.owner.CreateViewMatrix(this.width, this.height);

                    //  var texturematrix = Matrix.Scaling(dstrec.Width-1, dstrec.Height-1, 1);

                    effect.SetValue("worldViewProj", worldViewProj);
                    //     effect.SetValue("texturematrix", texturematrix);
                    effect.SetTexture("texture0", (fields[0] as IDirectXFrame).Texture);
                    effect.SetTexture("texture1", (fields[1] as IDirectXFrame).Texture);
                    effect.SetValue("vpHeight", this.height);

                    this.owner.Paint(
                        new System.Drawing.Rectangle(0, 0, this.width, this.height),
                        effect, 0);
                }
                catch { }
                finally
                {
                    this.owner.EndRender(this);
                }*/
        }

        public void Deinterlace(IRenderFrame deinterlace, DeinterlaceModes mode)
        {
         //   owner.Deinterlace(this, deinterlace, mode);
        }

        public void Save(string filename)
        {
            Lock();

            using (var bmp = new System.Drawing.Bitmap(this.Width, this.Height, this.Stride, System.Drawing.Imaging.PixelFormat.Format32bppArgb, this.Data))
            {
                try
                {
                    bmp.Save(filename);
                }
                catch { }
            }
            
                Unlock();
        }

        public void CopyTo(IntPtr dataPointer, int pitch)
        {
            Debug.Assert(this.textureColorBuffer.Length == 1);
            
            using (var ll = owner.GetDrawLock())
            {
                uint pbo;
                GL.GenBuffers(1, out pbo);
                GL.BindBuffer(BufferTarget.CopyWriteBuffer, pbo);

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.framebuffer);

                GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
                GL.ReadPixels(0, 0, Width, Height, global::OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, dataPointer);

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

                GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
                GL.DeleteBuffer(pbo);
            }
        }

        public int[] Textures => this.textureColorBuffer;
        public int Levels => this.textureColorBuffer.Length;
    }
}