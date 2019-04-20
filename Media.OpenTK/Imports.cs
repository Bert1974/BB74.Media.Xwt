using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace BaseLib.Audio.Interop
{
    using BaseLib.Media.Audio;

    public static class Imports
    {
        private const string _dll_name = "BaseLib.FFMPEG.dll";
        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern IntPtr openaudio(int bitrate, AudioFormat format, ChannelsLayout layout, int frames, int buffers, [MarshalAs(UnmanagedType.LPStr)] StringBuilder error);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void closeaudio(IntPtr audio);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void audio_write(IntPtr audio, IntPtr data, int leninsamples);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern int audio_bufsize(IntPtr audio);

        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void audio_start(IntPtr audio);
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void audio_stop(IntPtr audio);
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void audio_setcallback(IntPtr audio, IntPtr callback);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void RootMeanSquareFloat(IntPtr dst, IntPtr values, int length, int channels);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void RootMeanSquareShort(IntPtr dst, IntPtr values, int length, int channels);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void Add2BufferFloat(IntPtr src, IntPtr dst, int totsamples, int schannels, int dchannels, bool mono, float volume);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void Add2BufferShort(IntPtr src, IntPtr dst, int totsamples, int schannels, int dchannels, bool mono, float volume);
    }
}
namespace BaseLib.Video.Interop
{
    using BaseLib.Media.Audio;
    using BaseLib.Media.Video;

    namespace Recorder
    {
        public static class Imports
        {
            private const string _dll_name = "BaseLib.FFMPEG.dll";

            [SuppressUnmanagedCodeSecurity]
            [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr openrecorder([MarshalAs(UnmanagedType.LPStr)]string filename, [MarshalAs(UnmanagedType.LPStr)] StringBuilder error);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr openrecorder2(IntPtr writefunc, [MarshalAs(UnmanagedType.LPStr)] StringBuilder error);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern void destroyrecorder(IntPtr recorder);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr recorder_start(IntPtr recorder);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr recorder_stop(IntPtr recorder);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr recorder_addvideo(IntPtr recorder, int width, int height, ref BaseLib.Media.FPS fps, VideoFormat fmt, [MarshalAs(UnmanagedType.LPStr)] StringBuilder error);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr recorder_addaudio(IntPtr recorder, int bitrate, int samplerate, ChannelsLayout channels, AudioFormat audiofmt, [MarshalAs(UnmanagedType.LPStr)] StringBuilder error);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr recorder_video_push(IntPtr vidstream, IntPtr data, int stride, int width, int height, VideoFormat fmt, Int64 time);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr recorder_video_push2(IntPtr vidstream, IntPtr avframe);

            [SuppressUnmanagedCodeSecurity]
            [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
            public static extern IntPtr recorder_audio_push(IntPtr vidstream, Int64 time, IntPtr data, int totsamples, AudioFormat fmt);
        }
    }
    public static class Imports
    {
        private const string _dll_name = "BaseLib.FFMPEG.dll";
        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern IntPtr openplayer([MarshalAs(UnmanagedType.LPStr)]string filename, [MarshalAs(UnmanagedType.LPStr)] StringBuilder error);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void player_set_callbacks(IntPtr player, IntPtr eosdelegate, IntPtr flusheddegate);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void destroyplayer(IntPtr player);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern int _player_videostreamcount(IntPtr player);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void _player_get_videostream(IntPtr player, uint ind, ref videostreaminfo info);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern int _player_audiostreamcount(IntPtr player);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void _player_get_audiostream(IntPtr player, uint ind, ref audiostreaminfo info);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern long _player_duration(IntPtr player, long timebase);

        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern IntPtr _player_openvideo(IntPtr player, uint ind, /*VideoStream.FrameReadyFunction*/IntPtr frameready, [MarshalAs(UnmanagedType.LPStr)] StringBuilder error);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void _player_run(IntPtr player, long time, long timebase);
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void _player_seek(IntPtr player, long time, long timebase);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void _player_preparestop(IntPtr player);
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void _player_stop(IntPtr player);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern IntPtr _player_vid_allocframe(IntPtr stream, IntPtr allocfunc, IntPtr lockfunc, IntPtr unlockfunc);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern IntPtr _player_vid_fillframe(IntPtr stream, IntPtr frame, IntPtr avframe, ref Int64 time);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void _player_vidframe_freeframe(IntPtr frame);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern void _player_vidframe_freeavframe(IntPtr avframe);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(_dll_name, CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        public static extern IntPtr _player_openaudio(IntPtr player, uint ind, int samplerate, AudioFormat format, ChannelsLayout channels, IntPtr frameready, [MarshalAs(UnmanagedType.LPStr)] StringBuilder error);
    }
}