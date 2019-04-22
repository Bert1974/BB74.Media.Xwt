using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using Xwt;

namespace BaseLib.Platforms
{
    using Xwt = global::Xwt;

    public class GTK : IXwtRender
    {
        IXwtRender impl;

        /*     const string linux_libgdk_x11_name = "libgdk";

             [DllImport(linux_libgdk_x11_name)]
             internal static extern int gdk_pointer_grab(IntPtr gdkwindow, bool owner_events, IntPtr confine_to_gdkwin, IntPtr cursor, int time);

             [DllImport(linux_libgdk_x11_name)]
             internal static extern void gdk_pointer_ungrab(int time);*/

        class Windows : IXwtRender
        {
            const string linux_libgdk_win_name = "libgdk-win32-2.0-0.dll";

            [DllImport(linux_libgdk_win_name, CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr gdk_win32_drawable_get_handle(IntPtr raw);
            [DllImport(linux_libgdk_win_name, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr gdk_drawable_get_display(IntPtr drawable);

            class viewinfo
            {
                public IWindowInfo windowInfo;
                public IGraphicsContext gfxcontext;

                public viewinfo(IWindowInfo windowInfo, IGraphicsContext gfxcontext)
                {
                    this.windowInfo = windowInfo;
                    this.gfxcontext = gfxcontext;
                }
                public void Dispose()
                {
                    this.gfxcontext.Dispose();
                    this.windowInfo.Dispose();
                }
            }
            static readonly Dictionary<Widget, viewinfo> views = new Dictionary<Widget, viewinfo>();
            private IRendererFactory render;

            public Windows(out IRendererFactory render)
            {
                this.render = render = new FrameFactory(null);
            }

            void IXwtRender.FreeWindowInfo(Widget win)
            {
                if (views.TryGetValue(win, out viewinfo view))
                {
                    view.Dispose();
                    views.Remove(win);
                }
            }
            //     [DllImport("libgdk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
            //    internal static extern IntPtr gdk_win32_drawable_get_handle(IntPtr raw);

            private IntPtr GetHwnd(WindowFrame r)
            {
                var wh = Activator.CreateInstance(Extensions.GetType("System.Windows.Interop.WindowInteropHelper"),
                    new object[] { (Xwt.Toolkit.CurrentEngine.GetSafeBackend(r) as Xwt.Backends.IWindowFrameBackend).Window });
                return (IntPtr)wh.GetType().GetPropertyValue(wh, "Handle");
            }
            void IXwtRender.CreateForWidgetContext(IRenderer renderer, IRenderOwner rendererimpl, Widget widget)
            {
                IntPtr hwnd;
                if (Xwt.Toolkit.CurrentEngine.Type == ToolkitType.Wpf)
                {
                    hwnd = GetHwnd(widget.ParentWindow);
                }
                else
                {
                    var wBackend = Xwt.Toolkit.CurrentEngine.GetSafeBackend(widget) as Xwt.GtkBackend.CanvasBackend;

                    var widget2 = wBackend.GetType().GetPropertyValue(wBackend, "Widget");
                    widget2.GetType().SetPropertyValue(widget2, "DoubleBuffered", false);

                    var gdkwin = widget2.GetType().GetPropertyValue(widget2, "GdkWindow");
                    var gdkwinhandle = (IntPtr)gdkwin.GetType().GetPropertyValue(gdkwin, "Handle");

                    hwnd = gdk_win32_drawable_get_handle(gdkwinhandle);
                }
                IWindowInfo WindowInfo = null;
                IGraphicsContext gfxcontext = null;

                WindowInfo = Utilities.CreateWindowsWindowInfo(hwnd);
                gfxcontext = new global::OpenTK.Graphics.GraphicsContext(new GraphicsMode(new ColorFormat(8, 8, 8, 8), 24, 8), WindowInfo, null, 3, 2, GraphicsContextFlags.Default);

                views[widget] = new viewinfo(WindowInfo, gfxcontext);

                gfxcontext.MakeCurrent(WindowInfo);
                gfxcontext.LoadAll();

                int major, minor;
                GL.GetInteger(GetPName.MajorVersion, out major);
                GL.GetInteger(GetPName.MinorVersion, out minor);

                Console.WriteLine($"OpenGL {major}.{minor}");

                gfxcontext.MakeCurrent(null);
            }

            /*    void IXwt.ReleaseCapture(Widget widget)
                {
                    Gdk.Pointer.Ungrab(0);
                }
                void IXwt.SetCapture(Widget widget)
                {
                    var wBackend = Xwt.Toolkit.CurrentEngine.GetSafeBackend(widget) as Xwt.GtkBackend.WidgetBackend;
                    Gdk.Pointer.Grab(wBackend.Widget.GdkWindow, true, Gdk.EventMask.AllEventsMask, null, null, 0);
                }*/

            void IXwtRender.StartRender(IRenderer renderer, Widget widget)
            {
                if (views.TryGetValue(widget, out viewinfo view))
                {
                    view.gfxcontext.MakeCurrent(view.windowInfo);

                    //         Console.WriteLine($"active context={(view.gfxcontext as IGraphicsContextInternal).Context.Handle}");
                }
            }
            void IXwtRender.EndRender(IRenderer renderer, Widget widget)
            {
                if (views.TryGetValue(widget, out viewinfo view))
                {
                    view.gfxcontext.MakeCurrent(null);
                    //             Console.WriteLine($"active context=null");
                }
            }
            void IXwtRender.SwapBuffers(Widget widget)
            {
                if (views.TryGetValue(widget, out viewinfo view))
                {
                    view.gfxcontext.SwapBuffers();
                }
            }
        }
        class X11 : IXwtRender
        {
            #region DllImports'
            const string linux_libx11_name = "libX11.so.6";
            const string linux_libgdk_x11_name = "libgdk-win32-2.0-0.dll";

            [DllImport(linux_libgdk_x11_name)]
            private static extern int gdk_pointer_grab(IntPtr gdkwindow, bool owner_events, IntPtr confine_to_gdkwin, IntPtr cursor, int time);

            [DllImport(linux_libgdk_x11_name)]
            private static extern void gdk_pointer_ungrab(int time);

            [SuppressUnmanagedCodeSecurity, DllImport(linux_libx11_name)]
            static extern void XFree(IntPtr handle);

            /// <summary> Returns the X resource (window or pixmap) belonging to a GdkDrawable. </summary>
            /// <remarks> XID gdk_x11_drawable_get_xid(GdkDrawable *drawable); </remarks>
            /// <param name="gdkDisplay"> The GdkDrawable. </param>
            /// <returns> The ID of drawable's X resource. </returns>
            [SuppressUnmanagedCodeSecurity, DllImport(linux_libgdk_x11_name)]
            static extern IntPtr gdk_x11_drawable_get_xid(IntPtr gdkDisplay);

            /// <summary> Returns the X display of a GdkDisplay. </summary>
            /// <remarks> Display* gdk_x11_display_get_xdisplay(GdkDisplay *display); </remarks>
            /// <param name="gdkDisplay"> The GdkDrawable. </param>
            /// <returns> The X Display of the GdkDisplay. </returns>
            [SuppressUnmanagedCodeSecurity, DllImport(linux_libgdk_x11_name)]
            static extern IntPtr gdk_x11_display_get_xdisplay(IntPtr gdkDisplay);

            [DllImport(linux_libgdk_x11_name)]
            internal static extern IntPtr gdk_x11_drawable_get_xdisplay(IntPtr raw);

            [DllImport(linux_libgdk_x11_name)]
            internal static extern IntPtr gdk_screen_get_root_window(IntPtr window);

            [DllImport(linux_libgdk_x11_name)]
            internal static extern IntPtr gdk_display_get_default_screen(IntPtr display);

            [DllImport(linux_libgdk_x11_name)]
            internal static extern int gdk_screen_get_number(IntPtr screen);

            [DllImport(linux_libgdk_x11_name)]
            internal static extern IntPtr gdk_drawable_get_display(IntPtr drawable);

            [Flags]
            internal enum XVisualInfoMask
            {
                No = 0x0,
                ID = 0x1,
                Screen = 0x2,
                Depth = 0x4,
                Class = 0x8,
                Red = 0x10,
                Green = 0x20,
                Blue = 0x40,
                ColormapSize = 0x80,
                BitsPerRGB = 0x100,
                All = 0x1FF,
            }
            public enum XVisualClass : int
            {
                StaticGray = 0,
                GrayScale = 1,
                StaticColor = 2,
                PseudoColor = 3,
                TrueColor = 4,
                DirectColor = 5,
            }
            [StructLayout(LayoutKind.Sequential)]
            struct XVisualInfo
            {
                public IntPtr Visual;
                public IntPtr VisualID;
                public int Screen;
                public int Depth;
                public XVisualClass Class;
                public long RedMask;
                public long GreenMask;
                public long blueMask;
                public int ColormapSize;
                public int BitsPerRgb;

                public override string ToString()
                {
                    return String.Format("id ({0}), screen ({1}), depth ({2}), class ({3})",
                        VisualID, Screen, Depth, Class);
                }
            }
            [DllImport("libX11", EntryPoint = "XGetVisualInfo")]
            static extern IntPtr XGetVisualInfoInternal(IntPtr display, IntPtr vinfo_mask, ref XVisualInfo template, out int nitems);

            static IntPtr XGetVisualInfo(IntPtr display, XVisualInfoMask vinfo_mask, ref XVisualInfo template, out int nitems)
            {
                return XGetVisualInfoInternal(display, (IntPtr)(int)vinfo_mask, ref template, out nitems);
            }
            #endregion

            class viewinfo
            {
                public IWindowInfo windowInfo;
                public IGraphicsContext gfxcontext;
                public ContextHandle handle;

                public viewinfo(IWindowInfo windowInfo, IGraphicsContext gfxcontext, ContextHandle handle)
                {
                    this.windowInfo = windowInfo;
                    this.gfxcontext = gfxcontext;
                    this.handle = handle;
                }
            }
            static readonly Dictionary<Widget, viewinfo> views = new Dictionary<Widget, viewinfo>();
            private IRendererFactory render;

            public X11(out IRendererFactory render)
            {
                this.render = render = new FrameFactory(null);
            }

            void IXwtRender.FreeWindowInfo(Widget win)
            {
            }

            void IXwtRender.CreateForWidgetContext(IRenderer renderer, IRenderOwner rendererimpl, Widget win)
            {
                var wBackend = Xwt.Toolkit.CurrentEngine.GetSafeBackend(win) as Xwt.GtkBackend.CanvasBackend;
                var widget = wBackend.GetType().GetPropertyValue(wBackend, "Widget");
                widget.GetType().SetPropertyValue(widget, "DoubleBuffered", false);
                var gdkwin = widget.GetType().GetPropertyValue(widget, "GdkWindow");
                var h = (IntPtr)gdkwin.GetType().GetPropertyValue(gdkwin, "Handle");

                IntPtr windowHandle = gdk_x11_drawable_get_xid(h);// wBackend.Widget.Handle
                IntPtr display2 = gdk_drawable_get_display(h);
                IntPtr display = gdk_x11_drawable_get_xdisplay(h);
                IntPtr screen = gdk_display_get_default_screen(display2);
                int screenn = gdk_screen_get_number(screen);
                IntPtr rootWindow = gdk_screen_get_root_window(screen);
                IntPtr visualInfo = IntPtr.Zero;

                XVisualInfo info = new XVisualInfo();
                info.VisualID = IntPtr.Zero;
                int dummy;
                visualInfo = XGetVisualInfo(display, XVisualInfoMask.ID, ref info, out dummy);
                //     }
                /*    else
                    {
                        visualInfo = GetVisualInfo(display);
                    }*/

                var WindowInfo = Utilities.CreateX11WindowInfo(display, screenn, windowHandle, rootWindow, visualInfo);

                XFree(visualInfo);

                var gfxcontext = new OpenTK.Graphics.GraphicsContext(new GraphicsMode(new ColorFormat(8, 8, 8, 8), 24, 8), WindowInfo, 3, 3, GraphicsContextFlags.Default);

                views[win] = new viewinfo(WindowInfo, gfxcontext, (gfxcontext as IGraphicsContextInternal).Context);

                gfxcontext.MakeCurrent(WindowInfo);
                gfxcontext.LoadAll();

                int major, minor;
                GL.GetInteger(GetPName.MajorVersion, out major);
                GL.GetInteger(GetPName.MinorVersion, out minor);

                Console.WriteLine("OpenGL {0}.{1}", major, minor);

                gfxcontext.MakeCurrent(null);
            }

            /*  void IXwt.ReleaseCapture(Widget widget)
              {
                  gdk_pointer_ungrab(0);
              }
              void IXwt.SetCapture(Widget widget)
              {
                  var wBackend = Xwt.Toolkit.CurrentEngine.GetSafeBackend(widget) as Xwt.GtkBackend.WidgetBackend;

                  gdk_pointer_grab(wBackend.Widget.GdkWindow.Handle, true, IntPtr.Zero, IntPtr.Zero, 0);
              }*/
            void IXwtRender.StartRender(IRenderer renderer, Widget widget)
            {
                views[widget].gfxcontext.MakeCurrent(views[widget].windowInfo);
            }
            void IXwtRender.EndRender(IRenderer renderer, Widget widget)
            {
                views[widget].gfxcontext.MakeCurrent(null);
            }
            void IXwtRender.SwapBuffers(Widget widget)
            {
                views[widget].gfxcontext.SwapBuffers();
            }
        }

        public GTK(out IRendererFactory render)
            : this(out render, true)
        {
        }
        public GTK(out IRendererFactory render, bool initialize)
        {
            if (System.Environment.OSVersion.Platform == PlatformID.Unix ||
                System.Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                impl = new X11(out render);
            }
            else
            {
                impl = new Windows(out render);
            }
        }
        void IXwtRender.FreeWindowInfo(Widget widget)
        {
            impl.FreeWindowInfo(widget);
        }
        void IXwtRender.CreateForWidgetContext(IRenderer renderer, IRenderOwner rendererimpl, Widget widget)
        {
            impl.CreateForWidgetContext(renderer,rendererimpl,widget);
        }
        void IXwtRender.StartRender(IRenderer renderer, Widget widget)
        {
            impl.StartRender(renderer, widget);
        }
        void IXwtRender.EndRender(IRenderer renderer, Widget widget)
        {
            impl.EndRender(renderer, widget);
        }
        void IXwtRender.SwapBuffers(Widget widget)
        {
            impl.SwapBuffers(widget);
        }
    }
}
