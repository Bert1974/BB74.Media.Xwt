using BaseLib.IO;
using BaseLib.Media.Audio;
using BaseLib.Media.Video;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

namespace BaseLib.Media
{
    [TypeConverter(typeof(BaseLib.ExpandTypeConverter))]
    [DebuggerDisplay("{x},{y}")]
    public struct point
    {
        [Browsable(false)]
        public static readonly point Zero = new point(0, 0);
        public int x, y;

        [DebuggerStepThroughAttribute()]
        public point(int x, int y) { this.x = x; this.y = y; }

        [DebuggerStepThroughAttribute()]
        public bool Equals(point obj)
        {
            return this.x == obj.x && this.y == obj.y;
        }
        public override string ToString()
        {
            return $"{x}x{y}";
        }
    }
    [TypeConverter(typeof(BaseLib.ExpandTypeConverter))]
    [DebuggerDisplay("{width},{height}")]
    public struct size
    {
        [Browsable(false)]
        public static readonly size Zero = new size(0, 0);
        public int width, height;

        [DebuggerStepThroughAttribute()]
        public size(int width, int height) { this.width = width; this.height = height; }

        [DebuggerStepThroughAttribute()]
        public bool Equals(size obj)
        {
            return this.width == obj.width && this.height == obj.height;
        }
        public override string ToString()
        {
            return $"{width}x{height}";
        }
        /*    public static bool operator ==(size c1, size c2)
            {
                return c1.width == c2.width && c1.height == c2.height;
            }
            public static bool operator !=(size c1, size c2)
            {
                return c1.width != c2.width || c1.height != c2.height;
            }*/
    }
    [TypeConverter(typeof(BaseLib.ExpandTypeConverter))]
    [DebuggerDisplay("{x},{y},{width},{height}")]
    public struct rectangle
    {
        [Browsable(false)]
        public static readonly rectangle Zero = new rectangle(point.Zero, size.Zero);
        public int x, y, width, height;

        public size size => new size(width, height);
        public size Location => new size(x, y);

        [DebuggerStepThroughAttribute()]
        public rectangle(point pt, size size) { this.x = pt.x; this.y = pt.y; this.width = size.width; this.height = size.height; }
        [DebuggerStepThroughAttribute()]
        public rectangle(int x, int y, int width, int height) { this.x = x; this.y = y; this.width = width; this.height = height; }

        [DebuggerStepThroughAttribute()]
        public bool Equals(rectangle obj)
        {
            return this.x == obj.x && this.y == obj.y && this.width == obj.width && this.height == obj.height;
        }
        public override string ToString()
        {
            return $"{x}x{y}-{width}x{height}";
        }
    }
    namespace Video
    {
        public enum DeinterlaceModes
        {
            Auto,
            None,
            Blend,
            Split
        }
        public interface IRenderFrame : IVideoFrame
        {
            //     void Combine(IVideoFrame[] fieldframes);
        }
        public interface IVideoFrame : IDisposable
        {
            Int64 Time { get; set; }
            IntPtr Data { get; }
            int Width { get; }
            int Height { get; }
            int Stride { get; }
            Int64 Duration { get; }
            VideoFormat PixelFormat { get; }
            int Levels { get; }

            void Set(VideoFormat fmt);
            bool Set(long time, int width, int height, long duration);

            void Lock();
            void Unlock();
            void CopyTo(IntPtr dataPointer, int pitch);
            //      void Deinterlace(IRenderFrame destination, DeinterlaceModes mode);
        }
    }
    namespace Audio
    {
        public interface IAudioOut : IDisposable
        {
            AudioFormat Format { get; }
            int Channels { get; }
            ChannelsLayout ChannelLayout { get; }
            int SampleRate { get; }
            int SampleSize { get; }

            void Write(byte[] data, int leninsamples);
            int BufSize { get; }

            void Start();
            void Stop();

            ManualResetEvent Buffered { get; }
        }
        public interface IMixer : IDisposable
        {
            AudioFormat Format { get; }
            int Channels { get; }
            ChannelsLayout ChannelLayout { get; }
            int SampleRate { get; }
            int SampleSize { get; }

            ReaderWriterLock StreamsLock { get; }
            int TotalStreams { get; }

            void OpenRead();
            void CloseRead();

            byte[] Read(long otime, int totsamples);
            void Peek(int totsamples);

            void Pause(FifoStream audiostream);
            void Start(FifoStream audiostream);

            void Register(FifoStream audiostream, int channels, bool paused);
            void Unregister(FifoStream audiostream);
        }
    }
}