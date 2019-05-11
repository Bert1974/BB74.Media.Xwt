using BaseLib.IO;
using BaseLib.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace BaseLib.Media.Audio
{
    public class Mixer : IMixer
    {
        class streaminf
        {
            public int channels;
            internal float[] audiolevel;
            public bool muted = false;
            public float volume = 1.0f;
            internal bool running = true;
            internal long starttime = -1;
            public Action Done;

            public streaminf(int channels)
            {
                this.channels = channels;
                this.audiolevel = new float[this.channels];
            }

            internal void Set(bool muted, float volume)
            {
                this.muted = muted;
                this.volume = volume;
            }
        }

        private readonly List<FifoStream> streams = new List<FifoStream>();
        private readonly List<streaminf> streaminfo = new List<streaminf>();

        public ReaderWriterLock StreamsLock { get; } = new ReaderWriterLock();
        private byte[] buffer;

        public int TotalStreams { get { return this.streams.Count; } }

        public int Channels { get; }
        public ChannelsLayout ChannelLayout { get; }
        public AudioFormat Format { get; }
        public int SampleRate { get; }
        public int SampleSize { get; }

        private bool inaction = false;

        public Mixer(int samplerate, AudioFormat format, ChannelsLayout channels)
        {
            try
            {
                this.SampleRate = samplerate;
                this.Channels = channels == ChannelsLayout.Dolby ? 6 : 2;
                this.ChannelLayout = channels;
                this.Format = format;

                switch (format)
                {
                    case AudioFormat.Short16:
                        this.SampleSize = 2;
                        break;
                    case AudioFormat.Float32:
                        this.SampleSize = 4;
                        break;
                }
                this.buffer = new byte[this.SampleRate * this.SampleSize * this.Channels];
            }
            catch
            {
                GC.SuppressFinalize(this);
                throw;
            }
        }
        ~Mixer()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        void Dispose(bool disposing)
        {
            foreach (FifoStream audiostream in this.streams)
            {
                audiostream.Dispose();
            }
            this.streams.Clear();
        }

        public void Register(FifoStream audiostream, int channels, bool paused)
        {
            GetLock(out LockCookie cookie, out WriteLock wl);

            this.streams.Add(audiostream);
            this.streaminfo.Add(new streaminf(channels) { running = !paused });
            ReleaseLock(cookie, wl);
        }
        public void Unregister(FifoStream audiostream)
        {
            if (audiostream != null)
            {
                audiostream.Close();

                GetLock(out LockCookie cookie, out WriteLock wl);

                int ind = this.streams.IndexOf(audiostream);

                if (ind >= 0)
                {
                    this.streaminfo.RemoveAt(ind);
                    this.streams.RemoveAt(ind);
                }
                else
                {
                    Debug.Assert(false);
                }
                ReleaseLock(cookie, wl);
                audiostream.Clear();
            }
        }
        public void SetSetAudio(FifoStream audiostream, bool muted, float volume)
        {
            using (var rl = new ReadLock(this.StreamsLock))
            {
                int ind = streams.IndexOf(audiostream);
                if (ind != -1)
                {
                    this.streaminfo[ind].Set(muted, volume);
                }
            }
        }
        public void Peek(int totsamples) // shoud be locked by caller
        {
            for (int nit = 0; nit < this.streams.Count; nit++)
            {
                int len = totsamples * this.SampleSize * this.Channels;

                int total = this.streams[nit].Peek(buffer, 0, len);

                if (len == total)
                {
                    RootMeanSquare(this.streaminfo[nit].audiolevel, buffer, this.streaminfo[nit].channels);
                }
            }
        }

        private void RootMeanSquare(float[] audiolevel, byte[] buffer, int channels)
        {
            switch (this.Format)
            {
                case AudioFormat.Short16: AudioOut.RootMeanSquareShort(audiolevel, buffer, channels); break;
                case AudioFormat.Float32: AudioOut.RootMeanSquareFloat(audiolevel, buffer, channels); break;
                default: throw new NotImplementedException();
            }
        }
        private void Add2Buffer(byte[] buffer, byte[] result, int totsamples, int channels1, int channels2, bool v, float volume)
        {
            switch (this.Format)
            {
                case AudioFormat.Short16: AudioOut.Add2BufferShort(buffer, result, totsamples, channels1, channels2, v, volume); break;
                case AudioFormat.Float32: AudioOut.Add2BufferFloat(buffer, result, totsamples, channels1, channels2, v, volume); break;
                default: throw new NotImplementedException();
            }
        }


        public byte[] Read(Int64 time, int totsamples) // shoud be locked by caller
        {
            byte[] result = new byte[totsamples * this.SampleSize * this.Channels];

            for (int nit = 0; nit < this.streams.Count; nit++)
            {
                int len = totsamples * this.SampleSize * this.Channels;

                lock (this.streaminfo[nit])
                {
                    if (!this.streaminfo[nit].running)
                    {
                        if (this.streams[nit].EOS.WaitOne(0, false))
                        {
                            /*   var cookie = this.StreamsLock.UpgradeToWriterLock(-1);

                               this.streams[nit].Close();
                               this.streaminfo.RemoveAt(nit);
                               this.streams.RemoveAt(nit);

                               this.StreamsLock.DowngradeFromWriterLock(ref cookie);
                               nit--;*/
                        }
                        else if (this.streaminfo[nit].starttime != -1 && this.streaminfo[nit].starttime <= time)
                        {
                            this.streaminfo[nit].starttime = -1;
                            this.streaminfo[nit].running = true;

                            //       Debug.WriteLine("starting audio stream");
                        }
                        else
                        {
                            int total = this.streams[nit].Peek(buffer, 0, len);

                            if (len == total)
                            {
                                RootMeanSquare(this.streaminfo[nit].audiolevel, buffer, this.streaminfo[nit].channels);
                            }
                        }
                    }
                }
                if (this.streaminfo[nit].running)
                {
                    lock (this.streaminfo[nit])
                    {
                        int total = this.streams[nit].Read(buffer, 0, len);

                        if (len == total)
                        {
                            RootMeanSquare(this.streaminfo[nit].audiolevel, buffer, this.streaminfo[nit].channels);
                            //AudioConverter.Average(this.streaminfo[nit].audiolevel, this.buffer, totsamples, streaminfo[nit].channels);

                            if (!streaminfo[nit].muted)
                            {
                                if (this.streaminfo[nit].channels == 1)
                                {
                                    Add2Buffer(this.buffer, result, totsamples, streaminfo[nit].channels, this.Channels, true, this.streaminfo[nit].volume);
                                }
                                else
                                {
                                    Add2Buffer(this.buffer, result, totsamples, streaminfo[nit].channels, this.Channels, false, this.streaminfo[nit].volume);
                                }
                            }
                        }
                        else if (this.streams[nit].EOS.WaitOne(0, false))
                        {
                            if (total > 0)
                            {
                                if (!streaminfo[nit].muted)
                                {
                                    if (this.streaminfo[nit].channels == 1)
                                    {
                                        Add2Buffer(this.buffer, result, total / (this.SampleSize * this.Channels), streaminfo[nit].channels, this.Channels, true, this.streaminfo[nit].volume);
                                    }
                                    else
                                    {
                                        Add2Buffer(this.buffer, result, total / (this.SampleSize * this.Channels), streaminfo[nit].channels, this.Channels, false, this.streaminfo[nit].volume);
                                    }
                                }
                            }
                            //      Debug.Assert(total == 0);
                            this.streaminfo[nit].running = false;
                            this.streaminfo[nit].Done?.Invoke();
                            //     Debug.WriteLine("stopping audio stream");

                            /*        var cookie = this.StreamsLock.UpgradeToWriterLock(-1);

                                    this.streams[nit].Close();
                                    this.streaminfo.RemoveAt(nit);
                                    this.streams.RemoveAt(nit);

                                    this.StreamsLock.DowngradeFromWriterLock(ref cookie);

                                    nit--;*/
                            //this.streams[nit].EOS.Reset();
                        }
                        else
                        {
                            //      Debug.Assert(false);
                        }
                    }
                }
            }
            return result;
        }

        public float[] GetAudioLevels(FifoStream audiostream)
        {
            using (var wl = new ReadLock(this.StreamsLock))
            {
                int ind = streams.IndexOf(audiostream);
                if (ind == -1) { return new float[0]; }
                return this.streaminfo[ind].audiolevel;
            }
        }
        public void Flush(FifoStream audiostream)
        {
            audiostream.WaitAllReadDone();
        }
        public void Close()
        {
            using (var wl = new ReadLock(this.StreamsLock))
            {
                foreach (FifoStream audiostream in this.streams)
                {
                    audiostream.Close();
                }
            }
        }
        public void Open()
        {
            using (var wl = new ReadLock(this.StreamsLock))
            {
                foreach (FifoStream audiostream in this.streams)
                {
                    audiostream.Open();
                }
            }
        }
        public void CloseRead()
        {
            using (var wl = new ReadLock(this.StreamsLock))
            {
                for (int nit = 0; nit < this.streams.Count; nit++)
                {
                    if (this.streaminfo[nit].running)
                    {
                        this.streams[nit].CloseRead();
                    }
                }
            }
        }
        public void OpenRead()
        {
            using (var wl = new ReadLock(this.StreamsLock))
            {
                foreach (FifoStream audiostream in this.streams)
                {
                    audiostream.OpenRead();
                }
            }
        }
        public void CloseWrite()
        {
            using (var wl = new ReadLock(this.StreamsLock))
            {
                for (int nit = 0; nit < this.streams.Count; nit++)
                {
                    if (this.streaminfo[nit].running)
                    {
                        this.streams[nit].CloseWrite();
                    }
                }
            }
        }
        public void OpenWrite()
        {
            using (var wl = new ReadLock(this.StreamsLock))
            {
                foreach (FifoStream audiostream in this.streams)
                {
                    audiostream.OpenWrite();
                }
            }
        }

        public void Reset()
        {
            using (var wl = new ReadLock(this.StreamsLock))
            {
                foreach (FifoStream audiostream in this.streams)
                {
                    audiostream.Clear();
                }
            }
        }

       /* public void Action(long audiotime, FrameAction a)
        {
            this.inaction = true;
            a.AudioAction?.Invoke(audiotime, a);
            this.inaction = false;
        }*/

        public void Start(FifoStream audiostream)
        {
            GetLock(out LockCookie cookie, out WriteLock wl);

            int ind = streams.IndexOf(audiostream);
            if (ind != -1)
            {
                this.streaminfo[ind].running = true;
            }
            ReleaseLock(cookie, wl);
        }

        public void StartAt(FifoStream audiostream, long time)
        {
            GetLock(out LockCookie cookie, out WriteLock wl);

            int ind = streams.IndexOf(audiostream);
            if (ind != -1)
            {
                this.streaminfo[ind].starttime = time;
            }
            ReleaseLock(cookie, wl);
        }
        public void Pause(FifoStream audiostream)
        {
            GetLock(out LockCookie cookie, out WriteLock wl);
            int ind = streams.IndexOf(audiostream);
            if (ind != -1)
            {
                this.streaminfo[ind].running = false;
            }
            ReleaseLock(cookie, wl);
        }

        private void ReleaseLock(LockCookie cookie, WriteLock wl)
        {
            if (wl != null)
            {
                wl.Dispose();
            }
            else
            {
                this.StreamsLock.DowngradeFromWriterLock(ref cookie);
            }
        }

        private void GetLock(out LockCookie cookie, out WriteLock wl)
        {
            cookie = default(LockCookie);
            wl = null;
            if (this.inaction)
            {
                cookie = this.StreamsLock.UpgradeToWriterLock(-1);
            }
            else
            {
                wl = new WriteLock(this.StreamsLock);
            }
        }

        public void ListenEnd(FifoStream audiostream, Action function)
        {
            using (var rl = new ReadLock(this.StreamsLock))
            {
                //GetLock(out LockCookie cookie, out WriteLock wl);
                int ind = streams.IndexOf(audiostream);
                if (ind != -1)
                {
                    lock (this.streaminfo[ind])
                    {
                        if (!this.streaminfo[ind].running)
                        {
                            function();
                        }
                        else
                        {
                            this.streaminfo[ind].Done = function;
                        }
                    }
                }
                //   ReleaseLock(cookie, wl);
            }
        }
    }
}
