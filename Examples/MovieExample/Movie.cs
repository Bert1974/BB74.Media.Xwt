using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using BaseLib.IO;
using BaseLib.Media;
using BaseLib.Media.Audio;
using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using BaseLib.Media.Video;
using BaseLib.Xwt;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Xwt;

namespace MovieExample
{
    public class frameinfo : IDisposable
    {
        private Player _player;
        private IRenderer _renderer;
        private IntPtr _avframe;
        internal long _time, _duration;
        private VideoStream _video;

        public BaseLib.Media.VideoFrame Frame { get; private set; } // movieplayer
        public IVideoFrame Buffer { get; private set; } //opentk,directx

        private uint usagecnt = 1;

        /// <summary>
        /// saves values and allocates directx-buffer
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="video"></param>
        /// <param name="player"></param>
        public frameinfo(IRenderer renderer, VideoStream video, Player player)
        {
            this._player = player;
            this._renderer = renderer;
            this._video = video;
            this._avframe = IntPtr.Zero;
            this.Frame = video.AllocateFrame(this.allocfunc, this.lockfunc, this.unlockfunc); // wrapper to lock Buffer for interop
            this.Buffer = this._renderer.GetFrame();
        }
        /// <summary>
        /// saves values, but doesn't create render-buffer for frame
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="video"></param>
        /// <param name="player"></param>
        /// <param name="avframe"></param>
        /// <param name="time"></param>
        public frameinfo(IRenderer renderer, VideoStream video, Player player, IntPtr avframe, long time, long duration)
        {
            this._player = player;
            this._renderer = renderer;
            this._video = video;
            this._avframe = avframe;
            this._time = time;
            this._duration = duration;
        }
        /// <summary>
        /// saves values, but doesn't create render-buffer for frame
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="video"></param>
        /// <param name="player"></param>
        /// <param name="avframe"></param>
        /// <param name="time"></param>
        public frameinfo(IRenderer renderer, VideoStream video, IntPtr avframe, long time, long duration)
        {
            this._renderer = renderer;
            this._video = video;
            this._avframe = avframe;
            this._time = time;
            this._duration = duration;
        }
        /// <summary>
        /// fill from avframe to renderbuffer
        /// </summary>
        /// <param name="renderbuffer"></param>
        public void Update(ref frameinfo renderbuffer)
        {
            Update(ref renderbuffer, this._avframe);
        }
        /// <summary>
        /// fill from avframe to renderbuffer
        /// </summary>
        /// <param name="renderbuffer"></param>
        public void Update(ref frameinfo renderbuffer, IntPtr avframe)
        {
            if (renderbuffer == null)
            {
                renderbuffer = new frameinfo(_renderer, _video, _player);
                //this.Frame = this._video.AllocateFrame(this.allocfunc, this.lockfunc, this.unlockfunc);
                //this.Buffer = this._renderer.GetFrame();
            }
            this._video.FillFrame(renderbuffer.Frame, avframe);

            // this._lasttime = frame.Frame.Time;

            renderbuffer.Frame.Time += _video.Frame(this._player.basetime, this._player.timebase);
        }

        /*   internal IRenderFrame Deinterlace(IDocumentTracks doc, DeinterlaceModes deinterlace)
           {
               if (deinterlace == DeinterlaceModes.Auto)
               {
                   deinterlace = DeinterlaceModes.Split; // todo abc bert
               }
               if (deinterlace == DeinterlaceModes.None)
               {
                   return null;
               }
               switch (deinterlace)
               {
                   case DeinterlaceModes.Blend:
                       {
                           var _deinterlace = doc.GetRenderBuffer(this.Buffer.Width, this.Buffer.Height / 2, 1);

                           _deinterlace.Set(doc.display.Renderer.AlphaFormat);
                           _deinterlace.Set(this._time, this.Buffer.Width, this.Buffer.Height / 2, this.Buffer.Duration);

                           this.Buffer.Deinterlace(_deinterlace, deinterlace);
                           return _deinterlace;
                       }
                   case DeinterlaceModes.Split:
                       {
                           var _deinterlace = doc.GetRenderBuffer(this.Buffer.Width, this.Buffer.Height / 2, 2);
                           _deinterlace.Set(doc.display.Renderer.AlphaFormat);
                           _deinterlace.Set(this._time, this.Buffer.Width, this.Buffer.Height / 2, this.Buffer.Duration);
                           this.Buffer.Deinterlace(_deinterlace, deinterlace);
                           return _deinterlace;
                       }
               }
               throw new NotImplementedException();
           }*/

        ~frameinfo()
        {
            Debug.Assert(false);
        }
        public void Dispose()
        {
            if (--usagecnt == 0)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
        void Dispose(bool disposing)
        {
            if (this._avframe != IntPtr.Zero)
            {
                BaseLib.Media.VideoFrame.FreeAVFrame(this._avframe);
                this._avframe = IntPtr.Zero;
            }
            this.Buffer?.Dispose();
            this.Frame?.Dispose();
        }

        internal void allocfunc(IntPtr stream, long time, long duration, int width, int height, VideoFormat fmt, ref IntPtr data, ref int pitch, ref VideoFormat framefmt)
        {
            /*   switch (framefmt)
               {
                   case VideoFormat.ARGB:
                   case VideoFormat.RGBA:
                   default:
                       framefmt = _renderer.AlphaFormat;
                       break;
                   case VideoFormat.RGB:
                       break;
               }*/
            this.Buffer.Set(framefmt);
            framefmt = this.Buffer.PixelFormat;
            this.Buffer.Set(this.Frame.Time, width, height, 400000);
        }

        private void lockfunc(IntPtr stream, ref IntPtr data, ref int pitch)
        {
            this.Buffer.Lock();

            data = this.Buffer.Data;
            pitch = this.Buffer.Stride;
        }
        private void unlockfunc()
        {
            this.Buffer.Unlock();
        }
        public void Inc()
        {
            this.usagecnt++;
        }

    }
    public class Player : IDisposable
    {
        const int buffertotal = 4;

        internal long timebase, basetime;
        private IRenderer renderer;
        private IAudioOut audioout;
        private readonly IMixer mixer;
        private readonly FifoStream audiobuffer;
        private MoviePlayer player;
        private readonly AudioStream audio;
        private VideoStream video;
        private long starttime = 0;

        private ManualResetEvent stopevent = new ManualResetEvent(false), emptyevent = new ManualResetEvent(true), readyevent = new ManualResetEvent(false), running = new ManualResetEvent(true);

        List<frameinfo> frames = new List<frameinfo>();
        private frameinfo framebuffer;
        private byte[] audiobuffer2 = new byte[0];

        public Player(MainWindow mainwindow, string filename, long timebase)
        {
            try
            {
                this.timebase = timebase;
                this.renderer = mainwindow.Renderer;
                this.audioout = mainwindow.Audio;
                this.mixer = mainwindow.Mixer;
                this.player = BaseLib.Media.MoviePlayer.Open(() => { }, filename);

                try
                {
                    if (player.VideoStreams.Length > 0)
                    {
                        this.video = player.open_video(0, frameready);
                    }
                      if (player.AudioStreams.Length > 0)
                      {
                          this.audio = player.open_audio(0, mainwindow.Mixer, audioready);
                          this.audiobuffer = new FifoStream(mainwindow.Audio.SampleSize * mainwindow.Audio.SampleRate * 3);
                          this.mixer.Register(this.audiobuffer, this.audioout.Channels, false);
                      }
                    this.player.start(0, timebase);
                }
                catch
                {
                    Dispose(true);
                    GC.SuppressFinalize(this);
                    throw;
                }
            }
            catch
            {
                GC.SuppressFinalize(this);
                throw;
            }
        }
        ~Player()
        {
            Debug.Assert(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            using (var ll = this.renderer.GetDrawLock())
            {
                foreach (var f in this.frames)
                {
                    f.Dispose();
                }
                this.frames.Clear();
                this.framebuffer?.Dispose();
                this.framebuffer = null;
            }
            if (this.audiobuffer != null)
            {
                this.mixer.Unregister(this.audiobuffer);
            }
            this.player?.Dispose();
        }
        private void audioready(long time, IntPtr data, int samplecount)
        {
            /*   if (audio.Time(time, this._timebase) < this._audiostart)
               {
                   return;
               }*/
            int len = samplecount * this.audioout.SampleSize;
            if (audiobuffer2.Length < len)
            {
                audiobuffer2 = new byte[len];
            }
            Marshal.Copy(data, audiobuffer2, 0, len);
            try
            {
                this.audiobuffer.Write(audiobuffer2, 0, len);
            }
            catch { }
        }
        private bool frameready(IntPtr avframe, long time, long duration)
        {
            if (video.Time(time, this.timebase) < this.starttime)
            {
                return false;
            }
            if (WaitHandle.WaitAny(new WaitHandle[] { this.stopevent, this.emptyevent }, -1, false) == 1)
            {
                frameinfo frame;
                frame = new frameinfo(this.renderer, video, this, avframe, time, duration);
                lock (frames)
                {
                    frames.Add(frame);
                    this.readyevent.Set();

                    if (frames.Count >= buffertotal)
                    {
                        this.emptyevent.Reset();
                    }
                }
                return true;
            }
            return false;
        }
        public frameinfo GetFrame(long frametime, long duration)
        {
            if (WaitHandle.WaitAny(new WaitHandle[] { this.stopevent, this.running }, 0, false) == 1)
            {
                while (WaitHandle.WaitAny(new WaitHandle[] { this.stopevent, this.readyevent }, -1, false) == 1)
                {
                    lock (frames)
                    {
                        while (frames.Count > 1 && video.Time(frames[0]._time + 1, this.timebase) - 1 <= frametime)
                        {
                            this.frames[0].Dispose();
                            this.frames.RemoveAt(0);
                            this.emptyevent.Set();
                        }
                        var frame = this.frames.First();
                        if (video.Time(frame._time + 1, this.timebase) < frametime)
                        {
                            this.readyevent.Reset();
                            continue;
                        }
                        frame.Update(ref this.framebuffer); // avframe->texture, set time
                        this.framebuffer.Inc();
                        return this.framebuffer;
                    }
                }
            }
            return null;
        }
        internal void Stop()
        {
            this.player.preparestop();
            //  this._audiobuffer?.Close();
            this.stopevent.Set();
            this.player.stop();
            foreach (var f in this.frames)
            {
                f.Dispose();
            }
            this.frames.Clear();
        }
    }
}