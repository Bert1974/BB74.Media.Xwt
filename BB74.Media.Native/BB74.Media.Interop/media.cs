using BaseLib.Media;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;

#if (false)
namespace BaseLib.Media
{
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
    public enum VideoFormat : int 
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
#endif