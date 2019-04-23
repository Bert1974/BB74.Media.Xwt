using BaseLib.IO;
using BaseLib.Media.Audio;
using BaseLib.Media.Video;
using System;
using System.Threading;

namespace BaseLib.Media.Display
{
    public struct size
    {
        public int width, height;

        public size(int width, int height) { this.width = width; this.height = height; }
    }
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
namespace BaseLib.Media.Recording
{
    using BaseLib.Media.Display;
    using BaseLib.Media.Video;
    public interface IRecorderAudioStream
    {
        void Push(long time, byte[] data);
        void Push(long time, IntPtr data, int datalength);
    }
    public interface IRecorderVideoStream
    {
        void Push(object refframe, IRenderFrame frame, Int64 time, Int64 number);
        void Push(IntPtr avframe);
    }
    public interface IRecorder : IDisposable
    {
        long EndTime { get; }
        long TimeBase { get; }
        IRecorderOwner Display { get; set; }
        IRecorderVideoStream[] VideoStreams { get; }
        IRecorderAudioStream[] AudioStreams { get; }

        int AddVideo(int width, int height, VideoFormat fmt, FPS fps);
        int AddAudio(int rate, ChannelsLayout layout, AudioFormat format);
        void Pause(bool wait);
        void Start(long time);
        void Stop();
        void Prepare();
    }
    public interface IRecorderOwner
    {
        ReaderWriterLock recorderlock { get; }
    //    IMixer Mixer { get; }

        void Release(object refframe);
    }
}
namespace BaseLib.Media.Audio
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
    }
}