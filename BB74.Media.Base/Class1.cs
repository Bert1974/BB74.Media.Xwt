using BaseLib.IO;
using BaseLib.Media.Audio;
using BaseLib.Media.Video;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

namespace BaseLib.Media
{
    public struct point
    {
        public int x, y;

        public point(int x, int y) { this.x = x; this.y = y; }
    }
    public struct size
    {
        public int width, height;

        public size(int width, int height) { this.width = width; this.height = height; }
    }
    public struct rectangle
    {
        public int x, y, width, height;

        public rectangle(int x, int y, int width, int height) { this.x = x; this.y = y; this.width = width; this.height = height; }
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