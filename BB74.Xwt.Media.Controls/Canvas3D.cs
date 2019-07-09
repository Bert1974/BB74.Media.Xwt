using BaseLib.Media;
using BaseLib.Media.Audio;
using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using BaseLib.Media.Video;
using BaseLib.Xwt;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Xwt;
using Xwt.Drawing;

namespace BaseLib.Xwt.Controls.Media
{
    public partial class Canvas3D : Canvas, ICanvas3DControl, IRenderOwner
    {
        long ICanvas3DControl.TimeBase => this.impl.Timebase;
        IAudioOut ICanvas3DControl.Audio => this.Audio;
        IMixer ICanvas3DControl.Mixer => this.Mixer;
        IRenderer ICanvas3DControl.Renderer => this.Renderer;

        protected IVideoAudioInformation info { get; private set; }
        protected ICanvas3DImplmentation impl { get; private set; }
        protected IRenderer Renderer;
        protected IAudioOut Audio;
        protected IMixer Mixer;
        private Thread audiothread;
        protected ManualResetEvent audiostop = new ManualResetEvent(false);
        private object renderdata;

        public virtual void Start(long time)
        {
         //   this.videotime = time;
         //   this.audio.Run(time);
           this.audiostop.Reset();
            this.audiothread = new Thread(AudioRun) { Name = "audiopush", Priority = ThreadPriority.Highest };
            this.audiothread.Start();
            
            this.Audio.Buffered.WaitOne(-1, false);

        //    this.IsPlaying = true;
        //    this.audio.StartForReal();
            this.Renderer.Start();
            this.Audio?.Start();


        //    this.Buffered.Reset();
      //      this.audio.StartForReal(); // starts and buffers audio

         /*   if (!this.Renderer.UseNoThreading)
            {
                this.renderrun.Run(time); //start render frames
            }
            else
            {
                this.renderrun.Set(time); //
            }*/
        }

        protected virtual void AudioRun()
        {
            try
            {
                while (!audiostop.WaitOne(0, false))
                {
                    try
                    {
                        var data = this.Mixer.Read(0, 48000 / 25);
                        Audio.Write(data, data.Length / 8);
                    }
                    catch { }
                }
            }
            catch { }
        }

        public virtual void Stop()
        {
            this.Renderer.Stop();
            this.Audio?.Stop();

            this.impl.Stop();

        }

        public void /*ICanvas3DControl.*/Initialize (IVideoAudioInformation info, ICanvas3DImplmentation impl)
        {
            this.info = info;
            this.impl = impl;

            base.MinWidth = base.MinHeight = 0;

        }
        void ICanvas3DControl.OnLoaded()
        {
            try
            {
                this.Renderer = this.info.RenderFactory.Open(this.info.XwtRender, this, this, this.impl.FPS, this.impl.VideoSize);

                this.Audio = new AudioOut(48000, AudioFormat.Float32, ChannelsLayout.Stereo, 2);
                this.Mixer = new Mixer(this.Audio.SampleRate, this.Audio.Format, this.Audio.ChannelLayout);

                this.impl.OnLoaded(false);

                using (var lck = this.Renderer.GetDrawLock())
                {
                    this.impl.OnLoaded(true);
                }

                //this.Display.WaitBuffered();
                //    this.Audio?.Buffered.WaitOne(-1, false);



                //this.movie.p
            }
            catch (Exception e)
            {
                throw;
            }
        }

        void ICanvas3DControl.Unloading()
        {
            if (this.Renderer != null)
            {
                this.Renderer.Stop();
                this.Audio?.Stop();

                this.impl.Stop();


                this.impl.Unloading(false);

                this.Mixer?.Dispose();
                this.Mixer = null;
                this.Audio?.Dispose();
                this.Audio = null;

                using (var lck = this.Renderer.GetDrawLock())
                {
                    this.impl.Unloading(true);
                }
                this.Renderer.Dispose();
                this.Renderer = null;
            }
        }
        public virtual bool preparerender(IRenderFrame destination, long time, bool dowait)
        {
            return this.impl.StartRender(time, dowait, out this.renderdata);
        }
        public virtual void render(IRenderFrame destination, long time, rectangle r)
        {
            this.impl.Render(destination, time, r, this.renderdata);
            this.Renderer.Present(destination, r, IntPtr.Zero);
        }
        void IRenderOwner.StartRender(IRenderer renderer)
        {
            this.info.XwtRender.StartRender(renderer, this);
        }
        void IRenderOwner.EndRender(IRenderer renderer)
        {
            this.info.XwtRender.EndRender(renderer, this);
        }
        void IRenderOwner.DoEvents(Func<bool> cancelfunc)
        {
            this.info.XwtHelper.DoEvents(cancelfunc);
        }
        long ICanvas3DControl.Frame(long time)
        {
            return BaseLib.Time.GetFrame(time, this.impl.FPS, this.impl.Timebase);
        }
        long ICanvas3DControl.Time(long frame)
        {
            return BaseLib.Time.GetTime(frame, this.impl.FPS, this.impl.Timebase);
        }
    }

}
