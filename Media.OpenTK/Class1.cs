using BaseLib.IO;
using BaseLib.Media.Audio;
using BaseLib.Media.OpenTK;
using BaseLib.Media.Video;
using System;
using System.Threading;
using Xwt;

namespace BaseLib.Media.Display
{
    using Xwt = global::Xwt;
    public struct size
    {
        public int width, height;

        public static implicit operator size(Size size)
        {
            return new size() { width = Convert.ToInt32(size.Width), height = Convert.ToInt32(size.Height) };
        }
        public static implicit operator Size(size size)
        {
            return new Xwt.Size(size.width, size.height);
        }
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

    public interface IRendererFactory : IDisposable
    {
        string Name { get; } // enum RendererNames
        void Initialize();
        IRenderer Open(IXwtRender wxt, Widget ctl, OpenTK.IRenderOwner renderer, Size videosize); 

    }
    /*    public interface Ipaintinfo
        {
        }*/
    public interface IRenderer : IDisposable
    {
        IXwtRender Xwt { get; }

        void Start();
        void PrepareRender();
        void StopRender();
        object StartRender(IRenderFrame destination, Rectangle r);
        void EndRender(object state);
        /*    Object^ StartRender(params IVideoFrame^>^ destination);
            void EndRender(Object^renderdata);*/
  //      void Paint(IRenderFrame destination, IVideoFrame src, Rectangle dstrec);
   //     void Paint(IRenderFrame destination, IVideoFrame src, int index, Rectangle dstrec);
        /*   void Paint(IVideoFrame^ destination, IVideoFrame^ src, paintinfo^ paintinfo);
           void Paint(IVideoFrame^ destination, array<IVideoFrame^>^ src, effectinfo^ effectinfo);*/
        void Present(IVideoFrame frame, Rectangle dstrec, IntPtr ctl);
        /*    void Prepare(IVideoFrame^ frame, DeinterlaceModes deinterlace);*/
        IVideoFrame GetFrame();
        IRenderFrame GetRenderFrame(int levels);

        void AllocFunc(int width, int height, VideoFormat fmt, ref IntPtr data, ref int pitch, ref VideoFormat framefmt);
        void Stop();

        bool ForceNoThreading { get; }
        bool UseNoThreading { get; }
        VideoFormat AlphaFormat { get; }

        IDisposable GetDrawLock();
    }
    public interface IFrameListener
    {
        void CheckVideo(long videotime1, long videotime2);
        void CheckAudio(long audiotime1, long audiotime2);
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