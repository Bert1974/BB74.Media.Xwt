using BaseLib.Media.Display;
using BaseLib.Media.Video;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Runtime.InteropServices;

namespace BaseLib.Media.OpenTK
{
    public class VideoFrame : IVideoFrame, IOpenGLFrame
    {
        internal int texture = 0;
        private IRenderer owner;
        private byte[] buffer;
        private GCHandle handle;

        public VideoFrame(IRenderer owner)
        {
            this.owner = owner;
        }
        ~VideoFrame()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Set(VideoFormat fmt)
        {
            this.PixelFormat = VideoFormat.RGBA;
          /*  switch (fmt)
            {
                case BaseLib.Video.VideoFormat.RGBA:
                default:
                    this.PixelFormat = fmt;
                    break;
                case BaseLib.Video.VideoFormat.ARGB:
                    throw new NotImplementedException();
            }*/
        }

        public bool Set(long time, int width, int height, long length)
        {
            this.Time = time;
            this.Duration = length;

            if (texture == 0)
            {
                this.Width = width;
                this.Height = height;
                this.Stride = this.Width * 4;

                using (var ll =  owner.GetDrawLock())
                {
                    // create a RGBA color texture
                    GL.GenTextures(1, out texture);
                    GL.BindTexture(TextureTarget.Texture2D, texture);

                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                                        width, height,
                                        0, global::OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte,
                                        IntPtr.Zero);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                }

                this.buffer = new byte[this.Stride * this.Height];
            }
            return texture != 0;
        }

        private void Dispose(bool disposing)
        {
            using (var ll =  owner.GetDrawLock())
            {
                GL.DeleteTexture(texture);
            }

        }

        void IVideoFrame.Lock()
        {
            this.handle = GCHandle.Alloc(this.buffer, GCHandleType.Pinned);
        }

        void IVideoFrame.Unlock()
        {
            /*  using (var bmp = new Bitmap(this.Width, this.Height, this.Stride, System.Drawing.Imaging.PixelFormat.Format32bppRgb, this.Data))
              {
                  bmp.Save("e:\\test.bmp");
              }*/
            using (var ll =  owner.GetDrawLock())
            {
                GL.BindTexture(TextureTarget.Texture2D, texture);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, this.Width, this.Height,
                                global::OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, this.Data);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
            this.handle.Free();
        }

        public long Time { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public long Duration { get; private set; }
        public VideoFormat PixelFormat { get; private set; }

        public IntPtr Data => this.handle.AddrOfPinnedObject();
        public int Stride { get; private set; }

        int[] IOpenGLFrame.Textures => new int[] { this.texture };
        public int Levels => 1;

        public void Deinterlace(IRenderFrame deinterlace, DeinterlaceModes mode)
        {
            //  owner.Deinterlace(this, deinterlace, mode);
        }
        public void Save(string filename)
        {
       /*     using (var bmp = new System.Drawing.Bitmap(this.Width, this.Height, this.Stride, System.Drawing.Imaging.PixelFormat.Format32bppArgb, this.Data))
            {
                try
                {
                    bmp.Save(filename);
                }
                catch { }
            }*/
        }

        public void CopyTo(IntPtr dataPointer, int pitch)
        {
            throw new NotImplementedException();
        }
    }
}