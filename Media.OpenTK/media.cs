using BaseLib.Media;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace BaseLib.Media
{
    [Serializable()]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct Rational
    {
        public int num, den;
    }

    public class FPSConverter : StringConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) { return true; }
            return base.CanConvertFrom(context, sourceType);
        }
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string)) { return true; }
            return base.CanConvertTo(context, destinationType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                FPS.TryParse(value as string, out FPS fps);
                return fps;
            }
            return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return ((FPS)value).ToString();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [Serializable()]
    [TypeConverter(typeof(FPSConverter))]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct FPS : ICloneable, IEquatable<FPS>
    {
        public Rational Number;
        [MarshalAs(UnmanagedType.I1)]
        public bool Interlaced;

        public FPS(int num, int den, bool interlaced)
        {
            this.Number = new Rational() { num = num, den = den };
            this.Interlaced = interlaced;
        }

        public FPS(Rational fps, bool interlaced)
        {
            this.Number = fps;this.Interlaced = interlaced;
        }

        public object Clone()
        {
            return base.MemberwiseClone();
        }
        public bool Equals(FPS other)
        {
            return Number.num == other.Number.num && Number.den == other.Number.den && Interlaced == other.Interlaced;
        }
        public override string ToString()
        {
            return $"{Number.den}/{Number.num}/{(Interlaced ? 2 : 1)}";
        }
        public static bool TryParse(string fps, out FPS result)
        {
            var split = fps.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (split.Length == 1)
            {
                double n;
                if (double.TryParse(split[0], out n))
                {
                    if ((int)n == n)
                    {
                        result = new FPS(1, (int)n, false);
                        return true;
                    }
                    else
                    {
                        result = new FPS((int)(1 / (n * 1000.0)), 1000, false);
                        return true;
                    }
                }
            }
            else if (split.Length == 2)
            {
                if (int.TryParse(split[0], out int n1) && int.TryParse(split[1], out int n2))
                {
                    result = new FPS(n1, n2, false);
                    return true;
                }
            }
            else if (split.Length == 3)
            {
                if (int.TryParse(split[0], out int n1) && int.TryParse(split[1], out int n2))
                {
                    result = new FPS(n1, n2, split[2] == "2");
                    return true;
                }
            }
            result = new FPS(1, 25, true);
            return false;
        }

        public static implicit operator FPS(string value)
        {
            FPS.TryParse(value, out FPS fps);
            return fps;
        }
    }

}
namespace BaseLib.Media.Audio
{
    [Flags]
    public enum ChannelsLayout : Int64
    {
        [Description("stereo")]
        Stereo = 3,
        [Description("dolby")]
        Dolby = 0x60f
    }
    public enum AudioFormat : int
    {
        [Description("signed 16bit")]
        Short16,
        [Description("float")]
        Float32,
        [Description("singed 32 bit")]
        Int32
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct audiostreaminfo
    {
        public uint ind;
        public int samplerate, channels;
        public AudioFormat format;
        public Int64 channellayout;
        public Rational fps, timebase;
    }
}
namespace BaseLib.Media.Video
{
    public enum VideoFormat
    {
        RGB,
        RGBA,
        ARGB,
        YUV420,
        YUV422
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct videostreaminfo
    {
        public uint ind;
        public int width, height, ticks;
        public Rational fps, timebase;
    }
}