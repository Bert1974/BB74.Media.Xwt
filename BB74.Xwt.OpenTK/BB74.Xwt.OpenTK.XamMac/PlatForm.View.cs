using AppKit;
using BaseLib;
using BaseLib.Media;
using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using CoreAnimation;
using CoreGraphics;
using CoreVideo;
using OpenGL;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Xwt;

namespace BaseLib.Platforms
{
    using Xwt = global::Xwt;

    public partial class XamMac
    {
        class viewwindow : NSView//, iview
        {
            internal class layer : NSOpenGLLayer
            {
                [DllImport("/System/Library/Frameworks/OpenGL.framework/OpenGL")]
                internal static extern int CGLEnable(IntPtr handle, int flag);
                [DllImport("/System/Library/Frameworks/OpenGL.framework/OpenGL")]
                internal static extern int CGLSetCurrentContext(IntPtr handle);

                private readonly viewwindow owner;
                internal _GraphicsContext _ctx2;
              //  private Thread thread;
                private ManualResetEvent stop = new ManualResetEvent(false), stopped = new ManualResetEvent(false);
                private long timebase;
                private long lastupdate = -1;

                public layer(viewwindow owner)
                    : base()
                {
                    this.owner = owner;
                    base.NeedsDisplayOnBoundsChange = true;
                    base.Asynchronous = true;

                    lastupdate = -1;
                    timebase = 0;

#if (false)
                    this.thread = new Thread(() =>
                      {
                          /*   var wBackend = Xwt.Toolkit.CurrentEngine.GetSafeBackend(owner.window.ParentWindow) as Xwt.Backends.IWindowFrameBackend;
                             var win = wBackend.Window as NSWindow;

                             while (win.ParentWindow != null)
                             {
                                 win = win.ParentWindow;
                             }*/

                          long lu = 0, t = 0;

                          while (!stop.WaitOne(0, false))
                          {
                              while (!stop.WaitOne(20, false))
                              {
                                  t = Interlocked.Add(ref this.lastupdate, 0);

                                  if (t == -1) { continue; }

                                  if (DateTime.Now.Ticks - t > 500000)
                                  {
                                      break;
                                  }
                              }
                              Thread.Sleep(30);
                              long t2 = Interlocked.Add(ref this.lastupdate, 0);

                              if (t == t2)
                              {
                                  var tt = Math.Max(lu, t);
                                  //       this.owner.renderer.SkipRender(DateTime.Now.Ticks - tt);
                                  lu = tt;
                              }
                          }
                          this.stopped.Set();
                      })
                    { Name = "wincheck" };

                    this.thread.Start();
#endif
                }
                protected override void Dispose(bool disposing)
                {
                  //  this.stop.Set();
                  // this.stopped.WaitOne(-1, false);

                    this._ctx2?.Dispose();
                    base.Dispose(disposing);
                }
                public override NSOpenGLPixelFormat GetOpenGLPixelFormat(uint mask)
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
                public override NSOpenGLContext GetOpenGLContext(NSOpenGLPixelFormat pixelFormat)
                {
                    this._ctx2?.Dispose();
                    this._ctx2 = null;

                    var r = new NSOpenGLContext(pixelFormat/*GetOpenGLPixelFormat(0)*/, this.owner.openglctx);

                    if (r != null)
                    {
                        var error = CGLEnable(r.CGLContext.Handle, 313); // 
                        Trace.Assert(r.CGLContext.Handle != this.owner.openglctx.CGLContext.Handle);
                        {
                            r.CGLContext.Lock();
                            r.MakeCurrentContext();
                            this._ctx2 = new _GraphicsContext(); // register active context handle
                            NSOpenGLContext.ClearCurrentContext();
                            r.CGLContext.Unlock();
                        }
                        return r;
                    }
                    return null;
                }
                public override void Release(CGLContext glContext)
                {
                    this._ctx2?.Dispose();
                    this._ctx2 = null;
                    base.Release(glContext);
                }
                /*  class openglonscreen : NSOpenGLContext
                  {
                      public openglonscreen(NSOpenGLPixelFormat format, NSOpenGLContext shareContext)
                          : base(format,shareContext)
                      {

                      }
                      protected override void Dispose(bool disposing)
                      {
                          base.Dispose(disposing);
                      }
                  }*/
                public override bool CanDrawInCGLContext(CGLContext glContext, CGLPixelFormat pixelFormat, double timeInterval, ref CVTimeStamp timeStamp)
                {
                    //   lastupdate = DateTime.Now.Ticks;
                    return (this.View as viewwindow).initdone.WaitOne(0, false);//.CanDrawInCGLContext(glContext, pixelFormat, timeInterval, ref timeStamp);
                }
                public override void DrawInCGLContext(CGLContext glContext, CGLPixelFormat pixelFormat, double timeInterval, ref CVTimeStamp timeStamp)
                {
                    try
                    {
                        long now = DateTime.Now.Ticks;

                        /*     if (lastupdate != -1 && lastupdate - now > 1000000)
                             {
                                 // this.owner.renderer.SkipRender(lastupdate - now-400000);
                             }

                             Interlocked.Exchange(ref lastupdate, DateTime.Now.Ticks);
                             var now = DateTime.Now.Ticks;*/

                        long time;

                        if (lastupdate == -1)
                        {
                            time = lastupdate = this.timebase;
                            this.timebase = now - this.timebase;
                      //      Log.Verbose($"viewtime={0} first");
                        }
                        else
                        {
                            time = now - this.timebase;

                              if ((time - lastupdate) > 25000000)
                              {
                                  this.timebase = now - lastupdate;
                                  time = lastupdate;
                                  Log.Verbose($"viewtime={time} reset");
                              }
                        }
                 //       Log.Verbose($"viewtime={time}");

                        lastupdate = time;
                        if (this.owner.renderer.preparerender(null, time, false))
                        {
                            //  lock (this.owner)
                            {
                                using (this.owner.render.GetDrawLock(true))
                                {
                                    this.OpenGLContext.CGLContext.Lock();
                                    this.OpenGLContext.MakeCurrentContext();

                                    try
                                    {
                                        var r = new rectangle(0, 0, Convert.ToInt32(this.owner.Bounds.Width), Convert.ToInt32(this.owner.Bounds.Height));

                                        GL.Viewport(r.x,r.y,r.width,r.height);

                                        this.owner.renderer.render(null, time, r);
                                    }
                                    catch (Exception e)
                                    {
                                        //    Log.LogException(e);
                                    }

                                    this.OpenGLContext.FlushBuffer();
                                    NSOpenGLContext.ClearCurrentContext();
                                    this.OpenGLContext.CGLContext.Unlock();
                                }
                            }
                        }
               //         Log.Verbose($"viewtime-done={time}");
                    }
                    catch(Exception e)
                    {
                   
}
                }

            }


            public override bool IsOpaque => true;

            public override CALayer MakeBackingLayer()
            {
                var l = new layer(this);
                return l;
            }
            public override void ViewDidChangeEffectiveAppearance()
            {
                base.ViewDidChangeEffectiveAppearance();

                // Need to propagate information about retina resolution
                this.Layer.ContentsScale = this.Window.BackingScaleFactor;
            }

            private readonly Thread mainthread;
            private readonly IRenderer render;

            //     private CVDisplayLink dl;
            private readonly IRenderOwner renderer;
            internal readonly Widget window;
            internal _GraphicsContext _ctx;
            public NSOpenGLContext openglctx { get; }
            internal ManualResetEvent initdone = new ManualResetEvent(false);

            public viewwindow(IRenderer render, IRenderOwner renderer, Widget widget, CGRect frame, NSOpenGLContext ctx)
                : base(frame)
            {
                this.mainthread = Thread.CurrentThread;

                this.render = render;
                this.renderer = renderer;
                this.window = widget;

                base.WantsBestResolutionOpenGLSurface = true;
                this.WantsLayer = true;

                this.openglctx = ctx;

                Trace.Assert(this.openglctx != null, "no NSOpenGLContext context");

                //                    this.openglctx.CGLContext.Handle

                this.openglctx.CGLContext.Lock();
                try
                {
                    this.openglctx.MakeCurrentContext();

                    this._ctx = new _GraphicsContext(); // created from active content
                }
                catch { throw; }
                finally
                {
                    NSOpenGLContext.ClearCurrentContext();
                    this.openglctx.CGLContext.Unlock();
                }

                /*      var wBackend = Xwt.Toolkit.CurrentEngine.GetSafeBackend(widget.ParentWindow) as Xwt.Backends.IWindowFrameBackend;
                      var win = wBackend.Window as NSWindow;

                      while (win.ParentWindow != null)
                      {
                          win = win.ParentWindow;
                      }
                 //     win.DidChangeValue+= Win_DidChangeValue;
                      win.DidChangeBackingProperties+= Win_DidChangeBackingProperties;
                      win.DidChangeScreenProfile+= Win_DidChangeScreenProfile;
                      win.DidExpose+= Win_DidExpose;
                      win.DidMiniaturize+= Win_DidMiniaturize;
                      win.DidChangeScreen+= Win_DidChangeScreen;*/
            }
            protected override void Dispose(bool disposing)
            {
                this._ctx?.Dispose();
                base.Dispose(disposing);
        }
            /*    void Win_DidChangeScreen(object sender, EventArgs e)
                {
                }


                void Win_DidMiniaturize(object sender, EventArgs e)
                {
                }


                void Win_DidExpose(object sender, EventArgs e)
                {
                }


                void Win_DidChangeScreenProfile(object sender, EventArgs e)
                {
                }


                void Win_DidChangeBackingProperties(object sender, EventArgs e)
                {
                }


                void Win_DidChangeValue(string obj)
                {
                }
                public override void ViewDidHide()
                {
                    base.ViewDidHide();
                }
                public override void ViewDidUnhide()
                {
                    base.ViewDidUnhide();
                }*/

            internal void EndRender()
            {
                /*  if (object.ReferenceEquals(Thread.CurrentThread, this.mainthread))
                  {*/
                Debug.Assert(FrameFactory.getcurrentfunc().Handle == this.openglctx.CGLContext.Handle);
                NSOpenGLContext.ClearCurrentContext();
                if (Thread.CurrentThread == typeof(Application).GetProperty("UIThread", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, new object[0]) as Thread)
                {
                    this.openglctx.CGLContext.Unlock();
                }
                /*   }
                   else
                   {
                       layer.CGLSetCurrentContext(IntPtr.Zero);
                   }*/
            }
            internal void StartRender()
            {
                /*  if (object.ReferenceEquals(Thread.CurrentThread, this.mainthread))
                  {*/
                  
                if (Thread.CurrentThread == typeof(Application).GetProperty("UIThread", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, new object[0]) as Thread)
                {
                    this.openglctx.CGLContext.Lock();
                }
                this.openglctx.MakeCurrentContext();
                /*   }
                   else
                   {
                       layer.CGLSetCurrentContext((IntPtr)(this.Layer as layer)._ctx);
                   }*/
                //     (this.Layer as layer)._ctx.MakeCurrent();
                //     (this.Layer as layer).openglctx.CGLContext.Lock();
                //    (this.Layer as layer).openglctx.MakeCurrentContext();

                //  (this.Layer as layer).CheckContext();
            }
        }
    }
}
