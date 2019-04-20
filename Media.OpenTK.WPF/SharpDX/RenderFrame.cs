using BaseLib.Media.Display;
using BaseLib.Media.Video;
using SharpDX;
using SharpDX.Direct3D9;
using System;
using System.Diagnostics;

namespace BaseLib.Display.WPF
{
    public class RenderFrame : IRenderFrame, IDirectXFrame
    {
        private DirectX9Renderer owner;
        private Format dxfmt;
        private Texture systexture;
        private Surface syssurface;
        private DataRectangle lockrect;

        public Texture[] Textures { get; }
        public int Levels => this.Textures.Length;

        internal Surface[] rendertarget;

        public RenderFrame(DirectX9Renderer owner, int levels)
        {
            this.owner = owner;
            this.Textures = new Texture[levels];
            this.rendertarget = new Surface[levels];
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
            lock (this.owner.renderframes)
            {
                Unlock();
                this.systexture?.Dispose();
                System.Array.ForEach(this.rendertarget, _rt => _rt?.Dispose());
                System.Array.ForEach(this.Textures, _t => _t?.Dispose());
                this.owner.renderframes.Remove(this);
            }
        }
        public long Time { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public long Duration { get; private set; }

        public VideoFormat PixelFormat { get; private set; }

        public IntPtr Data => this.lockrect.DataPointer;
        public int Stride => this.lockrect.Pitch;

        void IVideoFrame.Set(VideoFormat pixfmt)
        {
            switch (pixfmt)
            {
                case VideoFormat.ARGB:
                    this.dxfmt = Format.A8R8G8B8;
                    this.PixelFormat = pixfmt;
                    break;
                case VideoFormat.RGBA:
                    throw new NotImplementedException();
                default:
                    this.dxfmt = Format.X8R8G8B8;
                    this.PixelFormat = VideoFormat.RGB;
                    break;
            }
        }
        bool IVideoFrame.Set(Int64 time, int width, int height, Int64 duration)
        {
            this.Time = time;
            this.Duration = duration;
            if (this.rendertarget[0] == null && this.owner.device != null)
            {
                this.Width = width;
                this.Height = height;

                for (int nit = 0; nit < this.Textures.Length; nit++)
                {
                    this.Textures[nit] = new Texture(this.owner.device, width, height, 1, Usage.RenderTarget, this.dxfmt, Pool.Default);
                    this.rendertarget[nit] = this.Textures[nit].GetSurfaceLevel(0);
                }
            }
            return this.rendertarget != null;
        }

        public void Lock()
        {
            if (this.syssurface == null)
            {
                var rendertargetsurface = this.Textures[0].GetSurfaceLevel(0);

                if (this.systexture == null)
                {
                    this.systexture = new Texture(this.owner.device, Width, Height, 1, Usage.None, this.dxfmt, Pool.SystemMemory);
                }
                this.syssurface = this.systexture.GetSurfaceLevel(0);

                this.owner.device.GetRenderTargetData(rendertargetsurface, this.syssurface);

                rendertargetsurface.Dispose();

                this.lockrect = this.syssurface.LockRectangle(LockFlags.ReadOnly);
            }
        }

        public void Unlock()
        {
            this.syssurface?.UnlockRectangle();
            this.syssurface?.Dispose();
            this.syssurface = null;
        }
        public void Combine(IVideoFrame[] frames)
        {
            owner.Combine(frames, this);

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
        internal void OnLost()
        {
            throw new NotImplementedException();
        }

        internal void OnReset()
        {
            throw new NotImplementedException();
        }
        public void Deinterlace(IRenderFrame deinterlace, DeinterlaceModes mode)
        {
            owner.Deinterlace(this, deinterlace, mode);
        }

        public void CopyTo(IntPtr dataPointer, int pitch)
        {
            throw new NotImplementedException();
        }
    }
}