using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BaseLib.Media
{
    using BaseLib.Media.Audio;
    using BaseLib.Media.Video;
    using BaseLib.Video.Interop;
    using global::Media.Interop;
    using System.Collections.Generic;
    using System.IO;

    public class BBRException : Exception
    {
        public BBRException(string txt)
            : base(txt)
        {
        }
        public BBRException(string txt, Exception innerexception)
            : base(txt, innerexception)
        {
        }

        internal static void CheckError(StringBuilder error)
        {
            if (error.Length > 0)
            {
                throw new BBRException(error.ToString());
            }
        }
        internal static void CheckError(StringBuilder error, string message)
        {
            if (error.Length > 0)
            {
                throw new BBRException($"{message}: {error.ToString()}");
            }
        }
    }


    public class BaseStream : IDisposable
    {
        ~BaseStream()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
    public class VideoFrame : IDisposable
    {
        internal IntPtr _vidframe;
        internal VideoStream _stream;
        internal VideoStream.FrameAllocateFunction _allocfunc;
        internal VideoStream.FrameLockFunction _lockfunc;
        internal VideoStream.FrameUnlockFunction _unlockfunc;
        public VideoFrame(VideoStream stream, IntPtr frame)
        {
            this._vidframe = frame;
            this._stream = stream;
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
        void Dispose(bool disposing)
        {
            Imports._player_vidframe_freeframe(this._vidframe);
            //_stream.Player.Invoke("_player_vidframe_freeframe", this._vidframe);
            this._vidframe = IntPtr.Zero;
        }

        public int Width => _stream.info.width;
        public int Height => _stream.info.height;

        public Int64 Time { get; set; }

        public static void FreeAVFrame(IntPtr avframe)
        {
            Imports._player_vidframe_freeavframe(avframe);
        }
    }
    public class VideoStream : BaseStream
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="time"></param>
        /// <returns>true if frame is taken over</returns>
        public delegate bool FrameReadyFunction(IntPtr avframe, long time, long duration);
        public delegate void FrameAllocateFunction(IntPtr stream, long time, long duration, int width, int height, VideoFormat fmt, ref IntPtr data, ref int pitch, ref VideoFormat framefmt);
        public delegate void FrameLockFunction(IntPtr stream, ref IntPtr data, ref int pitch);
        public delegate void FrameUnlockFunction();

        public readonly videostreaminfo info;
        public readonly IntPtr stream;
        private FrameReadyFunction _framedelegate;

        public MoviePlayer Player { get; }

        public VideoStream(MoviePlayer player, int n)
        {
            this.Player = player;
            this.info = new videostreaminfo();
            player.get_videostream((uint)n, ref info);
        }
        private VideoStream(VideoStream def, IntPtr stream)
        {
            this.Player = def.Player;
            this.info = def.info;
            this.stream = stream;
        }
        protected override void Dispose(bool disposing)
        {
            Debug.Assert(stream == IntPtr.Zero || disposing);

            base.Dispose(disposing);
        }
        internal VideoStream open(FrameReadyFunction frameready)
        {
            this._framedelegate = new VideoStream.FrameReadyFunction(frameready);
            var error = new StringBuilder(1024);

            var vidstream = Imports._player_openvideo(Player._player, info.ind, Marshal.GetFunctionPointerForDelegate(this._framedelegate), error);

            BBRException.CheckError(error, $"open_video");

            return new VideoStream(this, vidstream);
        }

        public VideoFrame AllocateFrame()
        {
            return AllocateFrame(null, null, null);
        }

        public VideoFrame AllocateFrame(FrameAllocateFunction allocfunc, FrameLockFunction lockfunc, FrameUnlockFunction unlockfunc)
        {
            var error = new StringBuilder(1024);

            var _allocfunc = allocfunc != null ? new FrameAllocateFunction(allocfunc) : null;
            var _lockfunc = lockfunc != null ? new FrameLockFunction(lockfunc) : null;
            var _unlockfunc = allocfunc != null ? new FrameUnlockFunction(unlockfunc) : null;

            var func1 = _allocfunc != null ? Marshal.GetFunctionPointerForDelegate(_allocfunc) : IntPtr.Zero;
            var func2 = _lockfunc != null ? Marshal.GetFunctionPointerForDelegate(_lockfunc) : IntPtr.Zero;
            var func3 = _unlockfunc != null ? Marshal.GetFunctionPointerForDelegate(_unlockfunc) : IntPtr.Zero;

            var frame = Imports._player_vid_allocframe(stream, func1, func2, func3);

            BBRException.CheckError(error, $"open_video");
            var result = new VideoFrame(this, frame) { _allocfunc = _allocfunc, _lockfunc = _lockfunc, _unlockfunc = _unlockfunc };

            return result;
        }

        public void FillFrame(VideoFrame frame, IntPtr avframe)
        {
            Int64 time = 0;
            Imports._player_vid_fillframe(stream, frame._vidframe, avframe, ref time);
            frame.Time = time;
        }
        public void FillFrame(IntPtr frame, IntPtr avframe, out long time)
        {
            time = 0;
            Imports._player_vid_fillframe(stream, frame, avframe, ref time);
        }

        public long Time(long frame, long timebase)
        {
            return (long)((double)frame /** this.info.ticks*/ * this.info.timebase.num * timebase / this.info.timebase.den);
        }
        public long Frame(long time, long timebase)
        {
            return (long)((double)time * this.info.timebase.den / (this.info.timebase.num * timebase));
        }
    }
    public class AudioStream : BaseStream
    {
        public delegate void FrameReadyFunction(Int64 time, IntPtr data, int samplecount);

        public readonly audiostreaminfo info;
        private readonly IntPtr stream;
        private FrameReadyFunction _framedelegate;

        public MoviePlayer Player { get; }

        public AudioStream(MoviePlayer player, int n)
        {
            this.Player = player;
            this.info = new audiostreaminfo();
            player.get_audiostream((uint)n, ref info);
        }
        private AudioStream(AudioStream def, IntPtr stream)
        {
            this.Player = def.Player;
            this.info = def.info;
            this.stream = stream;
        }
        protected override void Dispose(bool disposing)
        {
            Debug.Assert(stream == IntPtr.Zero || disposing);

            base.Dispose(disposing);
        }
        internal AudioStream open(int samplerate, AudioFormat format, ChannelsLayout channelslayout, AudioStream.FrameReadyFunction frameready)
        {
            this._framedelegate = new AudioStream.FrameReadyFunction(frameready);
            var error = new StringBuilder(1024);

            var audstream = Imports._player_openaudio(Player._player, info.ind, samplerate, (int)format, (Int64)channelslayout, Marshal.GetFunctionPointerForDelegate(frameready), error);

            BBRException.CheckError(error, $"open_audio");

            return new AudioStream(this, audstream);
        }

        public long Time(long time, long timebase)
        {
            return (long)((double)time /** this.info.ticks*/ * this.info.timebase.num * timebase / this.info.timebase.den);
        }
    }
    public class MoviePlayer : IDisposable
    {
        internal static bool IsRunningOnMono => Type.GetType("Mono.Runtime") != null;

        internal IntPtr _player;

        public string Filename { get; }

        private Action _eosdelegate, _flusheddelegate;
        public VideoStream[] VideoStreams { get; private set; }
        public AudioStream[] AudioStreams { get; private set; }
        private readonly List<VideoStream> videostreams = new List<VideoStream>();
        private readonly List<AudioStream> audiostreams = new List<AudioStream>();

        internal MoviePlayer(string filename, Action eos = null, Action flushed = null)
        {
            staticinit.Initialize();

            this.Filename = filename;

            this._eosdelegate = eos != null ? new Action(eos) : null;
            this._flusheddelegate = flushed != null ? new Action(flushed) : null;

            Initialize(filename);
        }
        private void Initialize(string filename)
        { 
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException(filename);
            }
            var error = new StringBuilder(1024);
            _player = Imports.openplayer(filename, error);

            BBRException.CheckError(error, $"error opening {filename}");

            Imports.player_set_callbacks(this._player,
                                this._eosdelegate != null ? Marshal.GetFunctionPointerForDelegate(this._eosdelegate) : IntPtr.Zero,
                                this._flusheddelegate != null ? Marshal.GetFunctionPointerForDelegate(this._flusheddelegate) : IntPtr.Zero);

            this.VideoStreams = Enumerable.Range(0, get_videostreamcount()).Select(_n => new VideoStream(this, _n)).ToArray();
            this.AudioStreams = Enumerable.Range(0, get_audiostreamcount()).Select(_n => new AudioStream(this, _n)).ToArray();
        }
        ~MoviePlayer()
        {
            Trace.Assert(_player==IntPtr.Zero);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public void Dispose(bool disposing)
        {
            Imports.destroyplayer(this._player);
            this._player = IntPtr.Zero;

            this.audiostreams.ForEach(_stream => _stream.Dispose());
            this.audiostreams.Clear();

            this.videostreams.ForEach(_stream => _stream.Dispose());
            this.videostreams.Clear();
        }
        internal int get_videostreamcount()
        {
            return Imports._player_videostreamcount(this._player);
        }
        internal void get_videostream(uint ind, ref videostreaminfo info)
        {
            _videostreaminfo _info = new _videostreaminfo();
            Imports._player_get_videostream(this._player, ind, ref _info);
            info = new videostreaminfo()
            {
                width = _info.width,
                height = _info.height,
                fps = new Rational() { num = _info.fps.num, den = _info.fps.den },
                ind = _info.ind,
                ticks = _info.ticks,
                timebase = new Rational() { num = _info.timebase.num, den = _info.timebase.den }
            };
        }
        internal int get_audiostreamcount()
        {
            return Imports._player_audiostreamcount(this._player);
        }
        internal void get_audiostream(uint ind, ref audiostreaminfo info)
        {
            var _info = new _audiostreaminfo();
            Imports._player_get_audiostream(this._player, ind, ref _info);
            info = new audiostreaminfo()
            {
                samplerate = _info.samplerate,
                channellayout = _info.channellayout,
                fps = new Rational() { num = _info.fps.num, den = _info.fps.den },
                timebase = new Rational() { num = _info.timebase.num, den = _info.timebase.den },
                channels = _info.channels,
                format = (AudioFormat)_info.format,
                ind = _info.ind
            };
        }
        public VideoStream open_video(uint ind, VideoStream.FrameReadyFunction frameready)
        {
            Trace.Assert(!this.videostreams.Any(_stream => _stream.info.ind == this.VideoStreams[ind].info.ind));

            var result = this.VideoStreams[ind].open(frameready);
            this.videostreams.Add(result);
            return result;
        }
        public AudioStream open_audio(uint ind, int samplerate, AudioFormat fmt, ChannelsLayout channellayout, AudioStream.FrameReadyFunction frameready)
        {
            Trace.Assert(!this.audiostreams.Any(_stream => _stream.info.ind == this.AudioStreams[ind].info.ind));

            var result = this.AudioStreams[ind].open(samplerate, fmt, channellayout, frameready);
            this.audiostreams.Add(result);
            return result;
        }
        public AudioStream open_audio(uint ind, IMixer mixer, AudioStream.FrameReadyFunction frameready)
        {
            Trace.Assert(!this.audiostreams.Any(_stream => _stream.info.ind == this.AudioStreams[ind].info.ind));

            var result = this.AudioStreams[ind].open(mixer.SampleRate, mixer.Format, mixer.ChannelLayout, frameready);
            this.audiostreams.Add(result);
            return result;
        }
        public void start(long time, long timebase) // call while stopped
        {
            Imports._player_run(this._player, time, timebase);
        }
        public void seek(long time, long timebase) // call while running
        {
            Imports._player_seek(this._player, time, timebase);
        }
        public void stop()
        {
            Imports._player_stop(this._player);
        }
        public void preparestop()
        {
            Imports._player_preparestop(this._player);
        }
        public long Duration(long timebase) {
            return Imports._player_duration(this._player, timebase);
        }

        public static MoviePlayer Open(Action eos, string filename)
        {
            return new MoviePlayer(filename, eos);
        }
    }
}