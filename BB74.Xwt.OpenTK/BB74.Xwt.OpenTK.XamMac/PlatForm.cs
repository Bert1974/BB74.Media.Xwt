using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using AppKit;
using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using BaseLib.Xwt;
using CoreGraphics;
using Xwt;
using Xwt.Backends;

namespace BaseLib.Platforms
{
    using Xwt = global::Xwt;

    public partial class XamMac : XwtImpl, IXwtRender
    {
        class viewinfo
        {
            internal NSView orgview;
            //      internal NSWindow backend;
            internal viewwindow myview;
            internal Widget widget;
            //   internal _GraphicsContext ctx;
        }
        static readonly Dictionary<Widget, viewinfo> views = new Dictionary<Widget, viewinfo>();

        [DllImport("/System/Library/Frameworks/OpenGL.framework/OpenGL")]
        private static extern IntPtr CGLGetCurrentContext();

        public XamMac(out IRendererFactory rendererFactory)
            : base()
        {
            rendererFactory = new FrameFactory(() => /*CGLGetCurrentContext()*/ NSOpenGLContext.CurrentContext?.CGLContext.Handle ?? IntPtr.Zero);

            Application.Initialize(ToolkitType.XamMac);

            // NSApplication.Initialize(); // Problem: This does not allow creating a separate app and using CocoaNativeWindow.
        }
        ~XamMac()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
        }
        public NSOpenGLPixelFormat GetOpenGLPixelFormat(uint mask)
        {
            object[] attribs = new object[] {

               NSOpenGLPixelFormatAttribute.OpenGLProfile, NSOpenGLProfile.Version3_2Core,
              /*      NSOpenGLPixelFormatAttribute.Accelerated,
                    NSOpenGLPixelFormatAttribute.NoRecovery,*/
                    NSOpenGLPixelFormatAttribute.DoubleBuffer,
                    NSOpenGLPixelFormatAttribute.ColorSize, 32,
                    NSOpenGLPixelFormatAttribute.AlphaSize,8,
                    NSOpenGLPixelFormatAttribute.DepthSize, 16}
                    ;
            return new NSOpenGLPixelFormat(attribs);
            //   return base.GetOpenGLPixelFormat(mask);
        }

        void IXwtRender.CreateForWidgetContext(IRenderer renderer, IRenderOwner rendererimpl, Canvas widget)
        {
            var o = Xwt.Toolkit.CurrentEngine.GetSafeBackend(widget);
            var wBackend = Xwt.Toolkit.CurrentEngine.GetSafeBackend(widget) as Xwt.Backends.IWidgetBackend;
            //  var nsWindow = wBackend.NativeHandle;l;
            var nsView = (wBackend.NativeWidget as NSView);

            var ctx = new NSOpenGLContext(GetOpenGLPixelFormat(0), null);

            var error = viewwindow.layer.CGLEnable(ctx.CGLContext.Handle, 313); // enable multithread

            viewwindow view = null;

            //    ctx.CGLContext.Lock();
            //   ctx.MakeCurrentContext();

            Application.InvokeAsync(() =>
            {
                view = new viewwindow(rendererimpl, widget, ViewPostion(widget, wBackend), ctx);

                nsView.AddSubview(view);

                Debug.Assert(!views.ContainsKey(widget));

                views[widget] = new viewinfo() { orgview = nsView,/* backend = wBackend,*/ myview = view, widget = widget };

                widget.BoundsChanged += sizechanged;

            }).Wait();

        /*    (this as IXwtRender).StartRender(renderer, widget);
            view._ctx = new _GraphicsContext();
            (this as IXwtRender).EndRender(renderer, widget);*/

            this.QueueOnUI(() => view.initdone.Set());
        }
        private void sizechanged(object sender, System.EventArgs e)
        {
            var w = (Widget)sender;
            if (views.TryGetValue(w, out viewinfo view))
            {
                var wBackend = Xwt.Toolkit.CurrentEngine.GetSafeBackend(w) as Xwt.Backends.IWidgetBackend;
                view.myview.Frame = ViewPostion(w, wBackend);
            }
        }

        private CGRect ViewPostion(Widget w, IWidgetBackend wBackend)
        {
            var be = Extensions.GetBackend(w);
            var r = new Rectangle(be.ConvertToWindowCoordinates(Point.Zero), be.Size);
            return new CGRect(0, 0, r.Width, r.Height);
        }

        void IXwtRender.FreeWindowInfo(Widget widget)
        {
            if (views.TryGetValue(widget, out viewinfo view))
            {
                view.myview.RemoveFromSuperviewWithoutNeedingDisplay();
              //  view.myview._ctx?.Dispose();
                view.myview.Dispose();
                widget.BoundsChanged -= sizechanged;
                views.Remove(widget);
            }
        }
    /*    void IXwt.ReleaseCapture(Widget widget)
        {
        }

        void IXwt.SetCapture(Widget widget)
        {
        }*/

        void IXwtRender.StartRender(IRenderer renderer, Widget widget)
        {
            if (views.TryGetValue(widget, out viewinfo view))
            {
                view.myview.StartRender();
            }
        }
        void IXwtRender.EndRender(IRenderer renderer, Widget widget)
        {
            if (views.TryGetValue(widget, out viewinfo view))
            {
                view.myview.EndRender();
            }
        }
        void IXwtRender.SwapBuffers(Widget widget)
        {

        }
    }
}
