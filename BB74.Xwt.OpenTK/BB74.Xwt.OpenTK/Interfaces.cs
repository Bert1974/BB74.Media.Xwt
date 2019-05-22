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
    public interface IRendererFactory : IDisposable
    {
        string Name { get; } // enum RendererNames
        void Initialize();
        IRenderer Open(IXwtRender wxt, Canvas ctl, OpenTK.IRenderOwner renderer, FPS fps, size videosize); 

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
        object StartRender(IRenderFrame destination, rectangle r);
        void EndRender(object state);
        /*    Object^ StartRender(params IVideoFrame^>^ destination);
            void EndRender(Object^renderdata);*/
  //      void Paint(IRenderFrame destination, IVideoFrame src, Rectangle dstrec);
   //     void Paint(IRenderFrame destination, IVideoFrame src, int index, Rectangle dstrec);
        /*   void Paint(IVideoFrame^ destination, IVideoFrame^ src, paintinfo^ paintinfo);
           void Paint(IVideoFrame^ destination, array<IVideoFrame^>^ src, effectinfo^ effectinfo);*/
        void Present(IVideoFrame frame, rectangle dstrec, IntPtr ctl);
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