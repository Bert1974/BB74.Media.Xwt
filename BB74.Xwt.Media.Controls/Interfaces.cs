using BaseLib.Media;
using BaseLib.Media.Audio;
using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using BaseLib.Media.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xwt;

namespace BaseLib.Xwt.Controls.Media
{
    public interface ICanvas3DControl
    {
        long TimeBase { get; }
        IRenderer Renderer { get; }
        IAudioOut Audio { get; }
        IMixer Mixer { get; }

        void Initialize(IVideoAudioInformation info, ICanvas3DImplmentation impl);
        void OnLoaded();
        void Unloading();

        long Frame(long time);
        long Time(long frame);
    }
    public interface ICanvas3DImplmentation
    {
        long Timebase { get; }
        size VideoSize { get; }
        FPS FPS { get; }

        void OnLoaded(bool renderlocked);
        void Unloading(bool renderlocked);
        void Stop();
        bool StartRender(long time, bool dowait);
        void Render(long time, rectangle viewport);

        long Frame(long time);
        long Time(long frame);
    }
    public interface IVideoAudioInformation
    {
        IRendererFactory RenderFactory { get; }
        IXwtRender XwtRender { get; }
        IXwt XwtHelper { get; }
    }



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

}
