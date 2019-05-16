using BaseLib.Media;
using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using BaseLib.Media.Video;
using System;

namespace DockExample.OpenTK
{
    public enum DisplayStates
    {
        Stopped,
        Paused,
        Running
    }

    public interface IWxtDisplay : IDisposable
    {
        DisplayStates State { get; }
        IWxtRenderer FrameRenderer { get; set; }
        IRenderer Renderer { get; set; }
        IRendererFactory RenderFactory { get; }

        long Time { get; }

        void Initialize(IRendererFactory factory, IXwtRender xwt, FPS fps, size videosize);
        
        void Pause();
        void Pause(long time);
        void Play(long time);
        /*
        void StartRender();
        void EndRender();

        IRenderFrame GetRenderFrame();
        object StartRender(IRenderFrame destination);
        void EndRender(object state);*/
    }
    public interface IWxtRenderer : IDisposable
    {
        IWxtDisplay Display { get; set; }

        void Initialize(size videosize, Int64 timebase);

        void Stop();
        void Pause(long time);
        void Play(long time);
        IRenderFrame GetFrame(long time, bool dowait);
        void FrameDone(IRenderFrame frame);
        //   void Lock();
        //    void Unlock();
    }

    public interface iview
    {
    }

}
