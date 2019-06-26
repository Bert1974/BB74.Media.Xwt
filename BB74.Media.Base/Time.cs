using BaseLib.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace BaseLib
{
    public static class Time
    {
        public static long FromTicks(long ticks, long timeBase)
        {
            return Convert.ToInt64((double)ticks * timeBase / 10000000.0);
        }

        public static long ToTick(long time, long timeBase)
        {
            return Convert.ToInt64(time * 10000000.0 / timeBase);
        }
        public static long GetTime(long frame, FPS fps, long timebase)
        {
            return (long)((double)fps.Number.num * frame * timebase / fps.Number.den);
        }
        public static long GetFrame(long time, FPS fps, long timebase)
        {
            return (long)(double)((time * fps.Number.den) / (timebase * fps.Number.num));
        }
    }
}
namespace BaseLib
{
    public class ExpandTypeConverter : TypeConverter
    {
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            if (value != null)
            {
                return GetProperties(value, attributes);
            }
            return base.GetProperties(context, value, attributes);
        }
        public virtual PropertyDescriptorCollection GetProperties(object value, Attribute[] attributes)
        {
            List<PropertyDescriptor> props = new List<PropertyDescriptor>();

            foreach (MemberInfo m in value.GetType().GetMembers(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                bool ok = false;
                Type t = null;
                object v = null;

                if (m is PropertyInfo)
                {
                    if ((m as PropertyInfo).GetIndexParameters().Length == 0)
                    {
                        t = (m as PropertyInfo).PropertyType;
                        ok = true;
                    }
                }
                else if (m is FieldInfo)
                {
                    if ((m as FieldInfo).FieldType == typeof(EventHandler))
                        ok = false;
                    else
                    {
                        t = (m as FieldInfo).FieldType;
                        ok = true;
                    }
                }
                if (ok)
                {
                    foreach (Attribute a in attributes)
                    {
                        Attribute aa = m.GetCustomAttributes(a.GetType(), true).OfType<Attribute>().FirstOrDefault();
                        if (aa != null)
                        {
                            string name = aa.GetType().Name;
                            if (name.EndsWith("Attribute"))
                            {
                                PropertyInfo prop = aa.GetType().GetProperty(
                                                                    name.Substring(0, name.Length - 9),
                                                                    BindingFlags.Public | BindingFlags.Instance);

                                ok = (bool)prop.GetValue(a, null) == (bool)prop.GetValue(aa, null);
                            }
                        }
                        if (!ok)
                        {
                            break;
                        }
                    }
                    if (ok)
                    {
                        List<Attribute> aa = new List<Attribute>();

                        if (m is PropertyInfo)
                        {
                            if ((m as PropertyInfo).GetIndexParameters().Length == 0)
                            {
                                v = (m as PropertyInfo).GetValue(value, null);
                            }
                        }
                        else if (m is FieldInfo)
                        {
                            v = (m as FieldInfo).GetValue(value);
                        }
                        if (v != null)
                        {
                            foreach (Attribute a in TypeDescriptor.GetAttributes(v.GetType(), false))
                            {
                                aa.Add(a);
                            }
                            foreach (Attribute a in v.GetType().GetCustomAttributes(true))
                            {
                                aa.Add(a);
                            }
                        }
                        foreach (Attribute a in TypeDescriptor.GetAttributes(t, false))
                        {
                            aa.Add(a);
                        }
                        foreach (Attribute a in t.GetCustomAttributes(true))
                        {
                            aa.Add(a);
                        }
                        foreach (Attribute a in m.GetCustomAttributes(true))
                        {
                            aa.Add(a);
                        }
                        // Attribute[] a = new Attribute[o.Length];
                        //  o.CopyTo(a, 0);
                        props.Add(new ExpandPropertyDescriptor(m, aa.ToArray()));
                    }
                }
            }
            return new PropertyDescriptorCollection(props.ToArray());
        }
        public class ExpandPropertyDescriptor : PropertyDescriptor
        {
            MemberInfo info;
            bool isreadonly;

            public ExpandPropertyDescriptor(MemberInfo m, Attribute[] a)
                : base(m.Name, a)
            {
                this.info = m;

                this.isreadonly = ((ReadOnlyAttribute)new List<Attribute>(a).Find(_a => _a is ReadOnlyAttribute) ?? new ReadOnlyAttribute(false)).IsReadOnly;
            }

            public override bool CanResetValue(object component)
            {
                return false;
            }

            public override Type ComponentType
            {
                get { return info.ReflectedType; }
            }

            public override object GetValue(object component)
            {
                if (info is PropertyInfo)
                {
                    return (info as PropertyInfo).GetValue(component, null);
                }
                else if (info is FieldInfo)
                {
                    return (info as FieldInfo).GetValue(component);
                }
                throw new NotImplementedException();
            }

            public override bool IsReadOnly
            {
                get { return this.isreadonly; }
            }

            public override Type PropertyType
            {
                get
                {
                    if (info is PropertyInfo)
                    {
                        return (info as PropertyInfo).PropertyType;
                    }
                    else if (info is FieldInfo)
                    {
                        return (info as FieldInfo).FieldType;
                    };
                    throw new NotImplementedException();
                }
            }

            public override void ResetValue(object component)
            {
            }

            private Type FieldPropertyType
            {
                get
                {
                    if (info is FieldInfo) { return (info as FieldInfo).FieldType; }
                    if (info is PropertyInfo) { return (info as PropertyInfo).PropertyType; }
                    throw new NotImplementedException();
                }
            }

            public override void SetValue(object component, object value)
            {
                if (value != null)
                {
                    if (value.GetType() != FieldPropertyType)
                    {
                        TypeConverter converter = TypeDescriptor.GetConverter(FieldPropertyType);

                        if (converter != null)
                        {
                            if (converter.CanConvertFrom(value.GetType()))
                            {
                                value = converter.ConvertFrom(value);
                            }
                        }
                    }
                }
                if (info is PropertyInfo)
                {
                    (info as PropertyInfo).SetValue(component, value, null);
                }
                else if (info is FieldInfo)
                {
                    (info as FieldInfo).SetValue(component, value);
                }
            }

            public override bool ShouldSerializeValue(object component)
            {
                return false;
            }
        }
    }
    public class EnumDescriptionConverter<T> : EnumConverter
        where T : Enum
    {
        public EnumDescriptionConverter() : base(typeof(T))
        {
        }
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                var v = Enum.GetValues(typeof(T)).Cast<T>().FirstOrDefault(_v => GetDescription(_v) == (string)value);

                if (GetDescription(v) == (string)value)
                {
                    return v;
                }
            }
            return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return GetDescription((T)value);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
        public static string GetDescription(Enum value)
        {
            return value.GetType().GetField(value.ToString()).
                        GetCustomAttributes(typeof(DescriptionAttribute), false).
                        OfType<DescriptionAttribute>().
                        Select(_d => _d.Description).
                        FirstOrDefault() ?? value.ToString();
        }
    }

}