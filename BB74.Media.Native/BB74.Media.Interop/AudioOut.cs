using BaseLib.Audio.Interop;
using BaseLib.Media.Audio;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace BaseLib.Media.Audio
{
    public class AudioOut : Audio.IAudioOut
    {
        private static bool IsRunningOnMono => Type.GetType("Mono.Runtime") != null;
        private IntPtr audio;

        public int SampleRate { get; }
        public int Channels { get; }
        public AudioFormat Format { get; }
        public ChannelsLayout ChannelLayout { get; }
        public int SampleSize { get; }

        int IAudioOut.BufSize => Imports.audio_bufsize(this.audio);

        public ManualResetEvent Buffered { get; } = new ManualResetEvent(false);

        Action m_callback;

        public AudioOut(int samplerate, AudioFormat format, ChannelsLayout channels, int buffers)
        {
            BaseLib.Media.Interop.staticinit.Initialize();

            try
            {
                this.SampleRate = samplerate;
                this.Channels = channels == ChannelsLayout.Dolby ? 6 : 2; ;
                this.ChannelLayout = channels;
                this.Format = format;

                switch (this.Format)
                {
                    case AudioFormat.Short16: this.SampleSize = 2 * this.Channels; break;
                    default: this.SampleSize = 4 * this.Channels; break;
                }
                Intialize(buffers);
            }
            catch(Exception e)
            {
                GC.SuppressFinalize(this);
                throw;
            }
        }
        private void Intialize(int buffers)
        {
            Interop.staticinit.Initialize2();
            var error = new StringBuilder(1024);
            this.audio = Imports.openaudio(this.SampleRate, this.Format, this.ChannelLayout, 25, 3, error);

            if (this.audio == IntPtr.Zero)
            {
                throw new Exception(error.ToString());
            }
            m_callback = () => this.Buffered.Set();

            Imports.audio_setcallback(this.audio, Marshal.GetFunctionPointerForDelegate(m_callback));
        }
        ~AudioOut()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool dispsing)
        {
            Imports.closeaudio(this.audio);
            this.audio = IntPtr.Zero;
            //Invoke("closeaudio", this.audio);
        }
        public void Write(byte[] data, int leninsamples)
        {
            var h = GCHandle.Alloc(data, GCHandleType.Pinned);
            Imports.audio_write(this.audio, h.AddrOfPinnedObject(), leninsamples);
            h.Free();
        }

        public static void RootMeanSquareFloat(float[] dst, byte[] values, int channels)
        {
            var h1 = GCHandle.Alloc(dst, GCHandleType.Pinned);
            var h2 = GCHandle.Alloc(values, GCHandleType.Pinned);

            Imports.RootMeanSquareFloat(h1.AddrOfPinnedObject(), h2.AddrOfPinnedObject(), values.Length, channels);

            h1.Free();
            h2.Free();
        }
        public static void RootMeanSquareShort(float[] dst, byte[] values, int channels)
        {
            var h1 = GCHandle.Alloc(dst, GCHandleType.Pinned);
            var h2 = GCHandle.Alloc(values, GCHandleType.Pinned);

            Imports.RootMeanSquareShort(h1.AddrOfPinnedObject(), h2.AddrOfPinnedObject(), values.Length, channels);

            h1.Free();
            h2.Free();
        }
        public static void Add2BufferFloat(byte[] src, byte[] dst, int totsamples, int schannels, int dchannels, bool mono, float volume)
        {
            var h1 = GCHandle.Alloc(src, GCHandleType.Pinned);
            var h2 = GCHandle.Alloc(dst, GCHandleType.Pinned);

            Imports.Add2BufferFloat(h1.AddrOfPinnedObject(), h2.AddrOfPinnedObject(), totsamples, schannels, dchannels, mono, volume);

            h1.Free();
            h2.Free();
        }
        public static void Add2BufferShort(byte[] src, byte[] dst, int totsamples, int schannels, int dchannels, bool mono, float volume)
        {
            var h1 = GCHandle.Alloc(src, GCHandleType.Pinned);
            var h2 = GCHandle.Alloc(dst, GCHandleType.Pinned);

            Imports.Add2BufferShort(h1.AddrOfPinnedObject(), h2.AddrOfPinnedObject(), totsamples, schannels, dchannels, mono, volume);

            h1.Free();
            h2.Free();
        }

        public void Start()
        {
            Imports.audio_start(this.audio);
        }

        public void Stop()
        {
            Imports.audio_stop(this.audio);

            this.Buffered.Reset();
        }
    }
}