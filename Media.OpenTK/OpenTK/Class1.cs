using System;
using System.Reflection;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;

namespace BaseLib.Media.OpenTK
{
    using Xwt = global::Xwt;

    public interface IOpenGLFrame
    {
        int[] Textures { get; }
        void Save(string filename);
    }
    public static class Extensions
    {
        public static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }
        public static void CheckShaderLog(this int shader)
        {
            var log = GL.GetShaderInfoLog(shader);
            if (log.Length > 0)
            {
                throw new Exception(log);
            }
        }
        public static void GetProgramInfoLog(this int shader)
        {
            var log = GL.GetProgramInfoLog(shader);
            if (log.Length > 0 && !log.StartsWith("WARNING:"))
            {
                throw new Exception(log);
            }
        }
        public static Xwt.Backends.IWidgetBackend GetBackend(this Xwt.Widget o)
        {
            return (Xwt.Backends.IWidgetBackend)Xwt.Toolkit.CurrentEngine.GetSafeBackend(o);
        }
        public static Xwt.Backends.IWindowBackend GetBackend(this Xwt.WindowFrame o)
        {
            return (Xwt.Backends.IWindowBackend)Xwt.Toolkit.CurrentEngine.GetSafeBackend(o);
        }
        public static object InvokeStatic(this Type type, string method, params object[] arguments)
        {
            return type.GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, arguments);
        }
        public static object InvokeStaticPrivate(this Type type, string method, params object[] arguments)
        {
            return type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, arguments);
        }

        public static object Invoke(this Type type, object instance, string method, params object[] arguments)
        {
            return type.GetMethod(method, BindingFlags.Public | BindingFlags.Instance).Invoke(instance, arguments);
        }
        public static object InvokePrivateStatic(this Type type, string method, params object[] arguments)
        {
            return type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, arguments);
        }
        public static object GetPropertyValue(this Type type, object instance, string propertyname)
        {
            return type.GetProperty(propertyname, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty).GetValue(instance, new object[0]);
        }
        public static void SetPropertyValue(this Type type, object instance, string propertyname, object value)
        {
            type.GetProperty(propertyname, BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty).SetValue(instance, value, new object[0]);
        }
        public static object GetPropertyValuePrivate(this Type type, object instance, string propertyname)
        {
            return type.GetProperty(propertyname, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty).GetValue(instance, new object[0]);
        }
        public static object GetPropertyValueStatic(this Type type, string propertyname)
        {
            return type.GetProperty(propertyname, BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty).GetValue(null, new object[0]);
        }
        public static void SetFieldValuePrivate(this Type type, object instance, string fieldname, object value)
        {
            type.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetField).SetValue(instance, value);
        }
        public static void SetFieldValuePrivateStatic(this Type type, string fieldname, object value)
        {
            type.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.SetField).SetValue(null, value);
        }

        public static bool IsDerived(this Type b, Type t)
        {
            while (b != null && !object.ReferenceEquals(b, t))
            {
                b = b.BaseType;
            }
            return b != null;
        }
    }
    /*    public static class OpenTKHandler
        {
            internal static GraphicsContext.GetCurrentContextDelegate getcurrentfunc;

            public static void Initialize(Func<IntPtr> getcurrentfunc)
            {
                Toolkit.Init(new ToolkitOptions() { Backend = PlatformBackend.PreferNative });

                if (getcurrentfunc != null)
                {
                    OpenTKHandler.getcurrentfunc = new GraphicsContext.GetCurrentContextDelegate(() => new ContextHandle(getcurrentfunc()));

                    typeof(GraphicsContext).SetFieldValuePrivate("GetCurrentContext", OpenTKHandler.getcurrentfunc);
                }
            }
        }*/
    public class _GraphicsContext : IDisposable
    {
        public global::OpenTK.Graphics.GraphicsContext ctx { get; }

        private static GraphicsContext.GetAddressDelegate addaddrfunc;

        public _GraphicsContext()
        {
            addaddrfunc = addaddrfunc ?? (GraphicsContext.GetAddressDelegate)typeof(Utilities).InvokePrivateStatic("CreateGetAddress");

            this.ctx = new GraphicsContext(new ContextHandle(), addaddrfunc, FrameFactory.getcurrentfunc);

            this.ctx.LoadAll();

            int major, minor;
            GL.GetInteger(GetPName.MajorVersion, out major);
            GL.GetInteger(GetPName.MinorVersion, out minor);

            Console.WriteLine("Major {0}\nMinor {1}", major, minor);

            Console.WriteLine($"Context-Handle {FrameFactory.getcurrentfunc()}");
        }

        public void Dispose()
        {
            this.ctx.Dispose();
            GC.SuppressFinalize(this);
        }

        public static implicit operator GraphicsContext(_GraphicsContext ctx) => ctx.ctx;
        public static implicit operator IntPtr(_GraphicsContext ctx) => (ctx.ctx as IGraphicsContextInternal).Context.Handle;

        public static void Initialize()
        {
            Toolkit.Init(new ToolkitOptions() { });
        }

        public void MakeCurrent()
        {
            this.ctx.MakeCurrent(null);
        }
    }
}