using BaseLib.Media;
using BaseLib.Media.Audio;
using BaseLib.Media.Video;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace BaseLib.Media
{
    /*    [Serializable()]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        internal struct rational
        {
            public int num, den;
        }
        [Serializable()]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        internal struct fps
        {
            public rational Number;
            [MarshalAs(UnmanagedType.I1)]
            public bool Interlaced;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        internal struct _audiostreaminfo
        {
            public uint ind;
            public int samplerate, channels;
            public int format;
            public Int64 channellayout;
            public rational fps, timebase;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        internal struct _videostreaminfo
        {
            public uint ind;
            public int width, height, ticks;
            public rational fps, timebase;
        }*/
}
namespace BaseLib.Interop
{
    internal delegate void messagefunction(int level, [MarshalAs(UnmanagedType.LPStr)]string group, [MarshalAs(UnmanagedType.LPStr)]string text);

    internal static class Imports
    {
#if X86_BUILD
     public   const string _dll_name = "x86\\BB74.Media.Native.dll";
#elif X64_BUILD
        public const string _dll_name = "x64\\BB74.Media.Native.dll";
#else
        public const string _dll_name = "BB74.Media.Native.dll";
#endif
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern void __setprintf(IntPtr callback);
    }
}
namespace BaseLib.Audio.Interop
{
    internal static class Imports
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern IntPtr openaudio(int bitrate, int format, long layout, int frames, int buffers, [MarshalAs(UnmanagedType.LPStr)] StringBuilder error);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void closeaudio(IntPtr audio);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void audio_write(IntPtr audio, IntPtr data, int leninsamples);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern int audio_bufsize(IntPtr audio);

        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void audio_start(IntPtr audio);
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void audio_stop(IntPtr audio);
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void audio_setcallback(IntPtr audio, IntPtr callback);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void RootMeanSquareFloat(IntPtr dst, IntPtr values, int length, int channels);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void RootMeanSquareShort(IntPtr dst, IntPtr values, int length, int channels);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void Add2BufferFloat(IntPtr src, IntPtr dst, int totsamples, int schannels, int dchannels, bool mono, float volume);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void Add2BufferShort(IntPtr src, IntPtr dst, int totsamples, int schannels, int dchannels, bool mono, float volume);
    }
}
namespace BaseLib.Video.Interop
{
    namespace Recorder
    {
        internal static class Imports
        {
            [SuppressUnmanagedCodeSecurity]
            [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr openrecorder([MarshalAs(UnmanagedType.LPStr)]string filename, [MarshalAs(UnmanagedType.LPStr)] StringBuilder error);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr openrecorder2(IntPtr writefunc, [MarshalAs(UnmanagedType.LPStr)] StringBuilder error);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern void destroyrecorder(IntPtr recorder);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr recorder_start(IntPtr recorder);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr recorder_stop(IntPtr recorder);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr recorder_addvideo(IntPtr recorder, int width, int height, ref BaseLib.Media.FPS fps, VideoFormat fmt, [MarshalAs(UnmanagedType.LPStr)] StringBuilder error);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr recorder_addaudio(IntPtr recorder, int bitrate, int samplerate, ChannelsLayout channels, AudioFormat audiofmt, [MarshalAs(UnmanagedType.LPStr)] StringBuilder error);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr recorder_video_push(IntPtr vidstream, IntPtr data, int stride, int width, int height, VideoFormat fmt, Int64 time);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr recorder_video_push2(IntPtr vidstream, IntPtr avframe);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr recorder_audio_push(IntPtr vidstream, Int64 time, IntPtr data, int totsamples, AudioFormat fmt);
        }
    }
    internal static class Imports
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern IntPtr openplayer([MarshalAs(UnmanagedType.LPStr)]string filename, [MarshalAs(UnmanagedType.LPStr)] StringBuilder error);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void player_set_callbacks(IntPtr player, IntPtr eosdelegate, IntPtr flusheddegate);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void destroyplayer(IntPtr player);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern int _player_videostreamcount(IntPtr player);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void _player_get_videostream(IntPtr player, uint ind, ref videostreaminfo info);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern int _player_audiostreamcount(IntPtr player);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void _player_get_audiostream(IntPtr player, uint ind, ref audiostreaminfo info);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern long _player_duration(IntPtr player, long timebase);

        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern IntPtr _player_openvideo(IntPtr player, uint ind, /*VideoStream.FrameReadyFunction*/IntPtr frameready, [MarshalAs(UnmanagedType.LPStr)] StringBuilder error);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void _player_run(IntPtr player, long time, long timebase);
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void _player_seek(IntPtr player, long time, long timebase);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void _player_preparestop(IntPtr player);
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void _player_stop(IntPtr player);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern IntPtr _player_vid_allocframe(IntPtr stream, IntPtr allocfunc, IntPtr lockfunc, IntPtr unlockfunc);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern IntPtr _player_vid_fillframe(IntPtr stream, IntPtr frame, IntPtr avframe, ref Int64 time);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void _player_vidframe_freeframe(IntPtr frame);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void _player_vidframe_freeavframe(IntPtr avframe);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(BaseLib.Interop.Imports._dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern IntPtr _player_openaudio(IntPtr player, uint ind, int samplerate, AudioFormat format, ChannelsLayout channels, IntPtr frameready, [MarshalAs(UnmanagedType.LPStr)] StringBuilder error);
    }
}