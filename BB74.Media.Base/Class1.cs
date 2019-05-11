using BaseLib.IO;
using BaseLib.Media.Audio;
using BaseLib.Media.Video;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

namespace BaseLib.Media.Display
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
            this.Number = fps; this.Interlaced = interlaced;
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

    public struct size
    {
        public int width, height;

        public size(int width, int height) { this.width = width; this.height = height; }
    }
    public enum DeinterlaceModes
    {
        Auto,
        None,
        Blend,
        Split
    }
    public interface IRenderFrame : IVideoFrame
    {
   //     void Combine(IVideoFrame[] fieldframes);
    }
    public interface IVideoFrame : IDisposable
    {
        Int64 Time { get; set; }
        IntPtr Data { get; }
        int Width { get; }
        int Height { get; }
        int Stride { get; }
        Int64 Duration { get; }
        VideoFormat PixelFormat { get; }
        int Levels { get; }

        void Set(VideoFormat fmt);
        bool Set(long time, int width, int height, long duration);

        void Lock();
        void Unlock();
        void CopyTo(IntPtr dataPointer, int pitch);
        //      void Deinterlace(IRenderFrame destination, DeinterlaceModes mode);
    }
}
namespace BaseLib.Media.Audio
{
    public interface IAudioOut : IDisposable
    {
        AudioFormat Format { get; }
        int Channels { get; }
        ChannelsLayout ChannelLayout { get; }
        int SampleRate { get; }
        int SampleSize { get; }

        void Write(byte[] data, int leninsamples);
        int BufSize { get; }

        void Start();
        void Stop();

        ManualResetEvent Buffered { get; }
    }
    public interface IMixer : IDisposable
    {
        AudioFormat Format { get; }
        int Channels { get; }
        ChannelsLayout ChannelLayout { get; }
        int SampleRate { get; }
        int SampleSize { get; }

        ReaderWriterLock StreamsLock { get; }
        int TotalStreams { get; }

        void OpenRead();
        void CloseRead();

        byte[] Read(long otime, int totsamples);
        void Peek(int totsamples);

        void Pause(FifoStream audiostream);
        void Start(FifoStream audiostream);

        void Register(FifoStream audiostream, int channels, bool paused);
        void Unregister(FifoStream audiostream);
    }
}