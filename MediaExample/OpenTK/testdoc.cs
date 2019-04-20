using BaseLib.Media.Display;
using BaseLib.Media.OpenTK;
using BaseLib.Xwt.Controls.DockPanel;
using System;
using System.Diagnostics;
using Xwt;
using Xwt.Drawing;

namespace DockExample.OpenTK
{
    public class opentkdoc : Canvas, IDockContent, IDockDocument, IDockNotify, IDockSerializable
    {
        const Int64 TimeBase = 10000000L;

        private readonly IXwtRender xwt;
        private readonly IRendererFactory factory;
        private OpenTK.IWxtDisplay xwtrender;

        // private Thread thread;

        Widget IDockContent.Widget => this;

        public string TabText => "testdoc";

        public IDockPane DockPane { get; set; }

        public opentkdoc(IRendererFactory factory, IXwtRender xwt)
        {
            this.xwt = xwt;
            this.factory = factory;

            base.BackgroundColor = Colors.Black;
            base.MinWidth = base.MinHeight = 100;
        }
        void IDockNotify.OnLoaded(IDockPane pane)
        {
            Debug.Assert(this.xwtrender == null);

            this.xwtrender = new OpenTK.XwtRender(this, this.xwt, TimeBase);
            this.xwtrender.FrameRenderer = new OpenTK.MovieRender(this);
            this.xwtrender.Initialize(this.factory, this.xwt, new Xwt.Size(1920,1080));
            this.xwtrender.Play(0);
        }
        void IDockNotify.OnUnloading()
        {
            this.xwtrender?.Dispose();
            this.xwtrender = null;
        }

        string IDockSerializable.Serialize()
        {
            return "";
        }
    }
  /*  internal static class Extensions
    {
        public static object InvokeStatic(this Type type, string method, params object[] arguments)
        {
            return type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, arguments);
        }

        public static object Invoke(this Type type, object instance, string method, params object[] arguments)
        {
            return type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, arguments);
        }
        public static object GetPropertyValue(this Type type, object instance, string propertyname)
        {
            return type.GetProperty(propertyname, BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty).GetValue(instance, new object[0]);
        }
        public static object GetPropertyValueStatic(this Type type, string propertyname)
        {
            return type.GetProperty(propertyname, BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty).GetValue(null, new object[0]);
        }
    }*/
}

