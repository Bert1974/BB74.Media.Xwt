using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Xwt;

namespace BaseLib.Platforms
{
    public class WPF : IXwtRender
    {
        class viewinfo
        {
            //     internal IWindowInfo wininf;
            //     internal GraphicsContext ctx;
            internal IRenderer renderer;
        }
        readonly  Dictionary<Widget, viewinfo> views = new Dictionary<Widget, viewinfo>();

        public WPF(out IRendererFactory render)
        {
            Initialize(out render);
        }
        ~WPF()
        {
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        public void Initialize(out IRendererFactory render)
        {
            render = new BaseLib.Display.WPF.FrameFactory();

            Application.Initialize(ToolkitType.Wpf);
        }
        /*      void IXwt.SetCapture(Widget widget)
               {
                   var wBackend = Xwt.Toolkit.CurrentEngine.GetSafeBackend(widget) as Xwt.WPFBackend.WidgetBackend;
                   wBackend.Widget.CaptureMouse();
               }
               void IXwt.ReleaseCapture(Widget widget)
               {
                   var wBackend = Xwt.Toolkit.CurrentEngine.GetSafeBackend(widget) as Xwt.WPFBackend.WidgetBackend;
                   wBackend.Widget.ReleaseMouseCapture();
               }*/
        void IXwtRender.CreateForWidgetContext(IRenderer renderer, IRenderOwner rendererimpl, Canvas widget)
        {
            //    views[widget].
            /*      var wBackend = Xwt.Toolkit.CurrentEngine.GetSafeBackend(win) as Xwt.WPFBackend.WidgetBackend;
                  //   var hwndSource = System.Windows.PresentationSource.FromVisual(wBackend.NativeWidget as Visual) as System.Windows.Interop.HwndSource;
                  //hwndSource.CompositionTarget.RenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
                  var window = Xwt.Toolkit.CurrentEngine.GetSafeBackend(win.ParentWindow) as Xwt.WPFBackend.WindowFrameBackend;
                  var h = new WindowInteropHelper(window.Window).Handle;
                  //   var hwndSource = System.Windows.PresentationSource.From(window.) as System.Windows.Interop.HwndSource;
                  //hwndSource.CompositionTarget.RenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

                  var wininf = Utilities.CreateWindowsWindowInfo(h);

                  var gm = new GraphicsMode(new ColorFormat(8, 8, 8, 0), 24, 8);
                  var ctx = new GraphicsContext(gm, wininf, 3, 3, GraphicsContextFlags.Default);

                  ctx.MakeCurrent(wininf);
                  ctx.LoadAll();

                  views[win] = new viewinfo() { wininf = wininf, ctx = ctx };*/

        //    widget.BoundsChanged += Widget_BoundsChanged;

            this.views[widget] = new viewinfo()
            {
                renderer = renderer
            };
        }

        private void Widget_BoundsChanged(object sender, EventArgs e)
        {
       //     (this.views[sender as Widget].renderer as Display.WPF.DirectX9Renderer).CheckPos();
        }

        void IXwtRender.FreeWindowInfo(Widget widget)
        {
       //     widget.BoundsChanged -= Widget_BoundsChanged;
            this.views.Remove(widget);
        }


        void IXwtRender.StartRender(IRenderer renderer, Widget widget)
        {
         //   var r = this.views[widget].renderer;
            renderer.Xwt.StartRender(renderer, widget);
        }
        void IXwtRender.EndRender(IRenderer renderer, Widget widget)
        {
        //    var r = this.views[widget].renderer;
            renderer.Xwt.EndRender(renderer, widget);
            /*     if (views.TryGetValue(widget, out viewinfo view))
             {
                 view.ctx.MakeCurrent(view.wininf);

                 int width = Convert.ToInt32(widget.Size.Width), height = Convert.ToInt32(widget.Size.Height);
                 GL.Viewport(0, 0, width, height);

                 GL.ClearColor((test++ % 10) / 10f, 0, 0, 1);
                 GL.Clear(ClearBufferMask.ColorBufferBit);

             //    view.ctx.SwapBuffers();
             }*/
        }

        void IXwtRender.SwapBuffers(Widget widget)
        {
            throw new NotImplementedException();
        }
    }
}