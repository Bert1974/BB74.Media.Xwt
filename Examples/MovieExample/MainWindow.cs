using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using BaseLib.Media;
using BaseLib.Media.Audio;
using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using BaseLib.Media.Video;
using BaseLib.Xwt;
using BaseLib.Xwt.Controls.Media;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Xwt;
using Xwt.Drawing;

namespace MovieExample
{
    public class MainWindow : Window
    {
        private ICanvas3DControl Canvas => this.Content as ICanvas3DControl;

        public IRendererFactory RenderFactory { get; }
        public IXwtRender XwtRender { get; }
        public IXwt XwtHelper { get; }

        public IRenderer Renderer => Canvas.Renderer;
        public IAudioOut Audio => Canvas.Audio;
        public IMixer Mixer => Canvas.Mixer;

        public MainWindow(IRendererFactory renderfactory, IXwtRender xwtrender, IXwt xwt)
        {
            this.RenderFactory = renderfactory;
            this.XwtRender = xwtrender;
            this.XwtHelper = xwt;

            this.Content = new Canvas3D(this)
            {
                MinWidth = 100,
                MinHeight = 100,
                HorizontalPlacement = WidgetPlacement.Fill,
                VerticalPlacement = WidgetPlacement.Fill,
                ExpandHorizontal = true,
                ExpandVertical = true
            };
        }
        protected override void OnShown()
        {
            base.OnShown();

        //    this.Xwt.SetCapture(this.Content);

            this.Canvas.OnLoaded();
        }
        protected override bool OnCloseRequested()
        {
           this.Canvas.Unloading();
            return true;// base.OnCloseRequested();
        }
        protected override void OnClosed()
        {
            base.OnClosed();
            Application.Exit();
        }
    }
}