using BaseLib.Media.Display;
using BaseLib.Media.Video;
using SharpDX;
using SharpDX.Direct3D9;
using System;
using System.Diagnostics;

namespace BaseLib.Display.WPF
{
    public class VideoFrame : IVideoFrame, IDirectXFrame
    {
     //   internal int texture = 0;
        private Surface surface;
        private DataRectangle lockrect;

        public Texture[] Textures { get; }
        public int Levels => this.Textures.Length;
        private readonly DirectX9Renderer owner;

        public VideoFrame(DirectX9Renderer owner)
        {
            this.owner = owner;
            this.Textures = new Texture[1];
        }
        ~VideoFrame()
        {
            Debug.Assert(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Set(VideoFormat fmt)
        {
            switch (fmt)
            {
                case VideoFormat.RGBA:
                case VideoFormat.ARGB:
                    this.dxfmt = Format.A8R8G8B8;
                    this.PixelFormat = VideoFormat.ARGB;
                    break;
                default:
                    this.dxfmt = Format.X8R8G8B8;
                    this.PixelFormat = VideoFormat.RGB;
                    break;
            }
        }

        public bool Set(long time, int width, int height, long duration)
        {
          //  owner.movieslock.Lock();

            this.Time = time;
            this.Duration = duration;

            if (this.Textures[0]==null && this.owner.device != null)
            {
                this.Width = width;
                this.Height = height;

                /* if (this.systexture == null)
                 {
                     this.systexture = new Texture(this.owner.device, width, height, 1, Usage.None, Format.X8R8G8B8, Pool.SystemMemory);
                 }*/
                for (int nit = 0; nit < this.Textures.Length; nit++)
                {
                    this.Textures[nit] = new Texture(this.owner.device, width, height, 1, Usage.Dynamic, this.dxfmt, Pool.Default);
                }
                this.surface = null;
            }
        //   owner.movieslock.Unlock();

            return this.Textures[this.Textures.Length-1] != null;
        }

        private void Dispose(bool disposing)
        {
            Unlock();

            System.Array.ForEach(this.Textures, _t=>_t?.Dispose());

         //   this.split?.ForEach(_f => _f.Dispose());
         //   this.split = new RenderFrame[0];

            lock (this.owner.videoframes)
            {
                this.owner.videoframes.Remove(this);
            }
        }

        public void Lock()
        {
            if (this.surface == null)
            {
                // owner.ready.WaitOne(-1, false);
                // owner.movieslock.Lock();

                this.surface = this.Textures[0].GetSurfaceLevel(0);
                this.lockrect = surface.LockRectangle(LockFlags.Discard | LockFlags.NoSystemLock);// | LockFlags.NoSystemLock);

                //    FFMPEG.memset(this.lockrect.DataPointer, 0xff, this.lockrect.Pitch * this.height);
            }
        }

        public void Unlock()
        {
            if (this.surface != null)
            {
                this.surface?.UnlockRectangle();
                this.surface?.Dispose();
                this.surface = null;
                //         owner.movieslock.Unlock();
            }
        }

        public long Time { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public long Duration { get; private set; }

        private Format dxfmt;

        public VideoFormat PixelFormat { get; private set; }

        public IntPtr Data => this.lockrect.DataPointer;
        public int Stride => this.lockrect.Pitch;
        
        internal void OnLost()
        {
            // Unlock();
            this.Textures[0]?.Dispose();
            this.Textures[0] = null;
        }

        internal void OnReset()
        {
            this.Textures[0] = new Texture(this.owner.device, Width, Height, 1, Usage.Dynamic, this.dxfmt, Pool.Default);
            //    this.owner.UpdateTexture(this.systexture, this.vidtexture, height);
        }

        public void Deinterlace(IRenderFrame destination, DeinterlaceModes mode)
        {
            this.owner.Deinterlace(this, destination, mode);
        }

        public void CopyTo(IntPtr dataPointer, int pitch)
        {
            throw new NotImplementedException();
        }
    }
}
