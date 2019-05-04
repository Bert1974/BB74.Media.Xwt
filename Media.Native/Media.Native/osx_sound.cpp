#include "stdafx.h"
#include "media.h"

#include <algorithm>
#include <assert.h>

#if (1)
#define IS_OSX_BUILD
#endif

#ifdef IS_OSX_BUILD
#define ALSA_PCM_NEW_HW_PARAMS_API
#include "AudioToolbox/AudioToolbox.h"

class osx_sound
{
public:
	typedef void(__stdcall *BufferedFucnction)();
public:
	int m_wpos, m_writelen;
	std::unique_ptr<char[]> _buffer;
	int frames, m_bufferlen, m_bufferpos;

	std::unique_ptr<std::thread> m_mainloop;
	std::mutex m_mainmutex;
	std::condition_variable m_maincond;

	bool m_empty, m_full, m_quiting, m_quitted, m_buffered, m_stopped;

	AudioFormat m_format;
	int m_samplesize,m_channels, m_frames;
	ChannelsLayout m_channelslayout;

	BufferedFucnction m_callback;

	AudioQueueRef queue;
	std::vector<AudioQueueBufferRef> m_buffers;

private:
	void Free()
	{
		OSStatus status = -1;
		if (queue)
		{
			while (!m_buffers.empty())
			{
				AudioQueueFreeBuffer(queue, *m_buffers.begin());
				m_buffers.erase(m_buffers.begin());
			}
			for (auto it = m_buffers.begin(); it != m_buffers.end(); ++it) {
			}
			status = AudioQueueDispose(queue, true);
			queue = 0;
		}
	}
private:
	static void listener(void *data, AudioQueueRef queue, AudioQueuePropertyID id)
	{
		((osx_sound*)data)->_listener(id);
	}
	void _listener(AudioQueuePropertyID id)
	{
		// Here, we're only listening for start/stop, so don't need to check
		// the id; it's always kAudioQueueProperty_IsRunning in our case.

		UInt32 running = 0;
		UInt32 size = sizeof running;
		/*   OggVorbis_File *vf = (OggVorbis_File *) vorbis; */
		OSStatus status = -1;

		status = AudioQueueGetProperty(queue, id, &running, &size);

		if (!running) {
			std::unique_lock<std::mutex> lk(m_mainmutex);
			m_stopped = true;
			m_maincond.notify_all();

			// In a "real example" we'd clean up the vf pointer with ov_clear() and
			// the audio queue with AudioQueueDispose(); however, the latter is 
			// better not called from within the listener function, so we just
			// exit normally.
		//	exit(0);
			// In a "real" application, we might signal the termination with
			// a pthread condition variable, or something similar, instead;
			// where the waiting thread would call AudioQueueDispose().  It is 
			// "safe" to call ov_clear() here, but there's no point.
		}
	}
	// The audio queue callback...
	static void callback(void *data, AudioQueueRef queue, AudioQueueBufferRef buffer)
	{
		((osx_sound*)data)->_callback(buffer);
	}
	void _callback(AudioQueueBufferRef buffer)
	{
		std::unique_lock<std::mutex> lk(m_mainmutex);

		assert(m_bufferpos==-1);

		int wpos = 0;

		buffer->mAudioDataByteSize = m_bufferlen;

		while (wpos < buffer->mAudioDataByteSize)
		{
			if (m_quiting)
			{
				//m_quitted = true;
				//m_maincond.notify_all();
				lk.unlock();
				return;
			}
			if (m_empty)
			{
				m_maincond.wait(lk);
			}
			else
			{
				int rpos =0;
				int wp = m_wpos;

				//lk.unlock();

				while (wpos < buffer->mAudioDataByteSize)
				{
					int tot = buffer->mAudioDataByteSize - wpos;
					if (tot > wp - rpos) { tot = wp - rpos; }

					if (tot > 0)
					{
						memcpy(&((char*)buffer->mAudioData)[wpos], &_buffer.get()[rpos], tot);

						wpos += tot; rpos += tot;
					}
					else
					{
						break;// buffer empty
					}
				}

				//lk.lock();
				if (rpos > 0)
				{
					m_empty = (wp == m_wpos);
					memmove(_buffer.get(), &_buffer.get()[rpos], m_wpos - rpos);
					m_wpos -= rpos;
					m_full = false;
					m_maincond.notify_all();
				}
			}
		}
		OSStatus status = -1;
		if ((status = AudioQueueEnqueueBuffer(queue, buffer, 0, 0))) {
			printf("AudioQueueEnqueueBuffer status = %d\n", status);
		}
	}
public:
	osx_sound(int samplerate, AudioFormat format, ChannelsLayout channels, int __frames, int buffers)
		: m_empty(true), m_full(false), m_quiting(false), m_quitted(false)
		, m_format(format), m_channelslayout(channels), m_frames(buffers)
		, m_buffered(false)
		, queue (0), frames(samplerate/buffers)
	{
		printf("sound open hello\n");

		OSStatus status = -1;
		try
		{
			int tot = 0;
			for (uint64_t tmp = channels, nit = pow(2, 63); nit != 0; nit /= 2)
			{
				if (tmp >= nit)
				{
					tmp -= nit;
					tot++;
				}
			}
			m_channels = tot;

			AudioStreamBasicDescription fmt = { 0 };

			switch (m_format)
			{
			case AudioFormat::SampleFloat32:
				/* Signed 16-bit little-endian format */
				fmt.mFormatFlags = kAudioFormatFlagIsFloat | kAudioFormatFlagIsPacked;
				fmt.mBitsPerChannel = 32;
				m_samplesize = 4;
				break;
			case AudioFormat::SampleShort16:
				/* Signed 16-bit little-endian format */
				fmt.mFormatID = kAudioFormatFlagIsSignedInteger | kAudioFormatFlagIsPacked;
				fmt.mBitsPerChannel = 16;
				m_samplesize = 2;
				break;
			default:
				throw new _err("unimplemented");
			}

			fmt.mSampleRate = samplerate;
			fmt.mFormatID = kAudioFormatLinearPCM;
			fmt.mFramesPerPacket = 1;
			fmt.mBytesPerFrame = channels * m_samplesize;
			fmt.mChannelsPerFrame = m_channels; // 2 for stereo
			fmt.mBytesPerPacket = m_channels* m_samplesize; // x2 for stereo

			// Create the audio queue with the desired format.
			AudioQueueNewOutput(&fmt, callback, this, NULL, NULL, 0, &queue);
			
			if (status)
			{
				throw new _err("AudioQueueNewOutput status = %d\n", status);
			}

			m_bufferlen = frames * m_samplesize*m_channels;

			for (int nit = 0; nit < 5; nit++)
			{
				AudioQueueBufferRef b = 0;
				status = AudioQueueAllocateBuffer(queue, m_bufferlen, &b);

				if (status)
				{
					throw new _err("AudioQueueAllocateBuffer status = %d\n", status);
				}
				m_buffers.push_back(b);
			}
			m_bufferpos = 0;
			
			status = AudioQueueAddPropertyListener(queue, kAudioQueueProperty_IsRunning, listener, this);
			
			if (status)
			{
				throw new _err("AudioQueueAddPropertyListener status = %d\n", status);
			}
			printf("sound open\n");
		}
		catch(_err*)
		{
		//	snd_pcm_hw_params_free(params);
		//	snd_pcm_sw_params_free(swparams);

			Free();
			throw;
		}
	}
public:
	~osx_sound()
	{
		if (queue)
		{
			OSStatus status = -1;
			{
				std::unique_lock<std::mutex> lk(m_mainmutex);
				m_quiting = true;
				m_maincond.notify_all();

				while (!m_quitted)
				{
					m_maincond.wait(lk);
				}
				m_mainloop->join();
			}
			Free();

			delete[] _buffer.get();
			_buffer.release();
		}
	}
private:
#if (false)
	void threadfunc()
	{
		std::unique_lock<std::mutex> lk(m_mainmutex);

		auto this_thread = pthread_self();// struct sched_param is used to store the scheduling priority
		struct sched_param params;

		// We'll set the priority to the maximum.
		params.sched_priority = sched_get_priority_max(SCHED_FIFO);
		pthread_setschedparam(this_thread, SCHED_FIFO, &params);

		while (true)
		{
			if (m_quiting)
			{
				m_quitted =true;
				m_maincond.notify_all();
				lk.unlock();
				return;
			}
			if (m_empty || !m_playing)
			{
				m_maincond.wait(lk);
			}
			else
			{
				int pos = 0;
				int wp = m_wpos;

				lk.unlock();
				while (pos + frames * m_samplesize*m_channels <= wp)
				{
					int avail;

				/*	do
					{
						avail = snd_pcm_avail(handle);
					} while (avail >  3);*/

					auto rc = snd_pcm_writei(handle, &_buffer.get()[pos], frames);

					if (rc == -EPIPE) {
						/* EPIPE means underrun */
						fprintf(stderr, "underrun occurred\n");
						snd_pcm_prepare(handle);
					}
					else if (rc == -11)
					{
						std::this_thread::sleep_for(std::chrono::milliseconds(10));
					}
					else if (rc < 0) {
						rc = snd_pcm_recover(handle, frames, 0);

						if (rc < 0)
						{
							fprintf(stderr,
								"error from writei: (%ld) %s\n",
								rc, snd_strerror(rc));
						}
						break;
					}
					else
					{
						pos += rc * m_samplesize*m_channels;
					}
				}
				lk.lock();
				if (pos > 0)
				{
					m_empty = (wp == m_wpos);
					memmove(_buffer.get(), &_buffer.get()[pos], m_wpos - pos);
					m_wpos -= pos;
					m_full = false;
					m_maincond.notify_all();
				}
			}
		}
	}
#endif
public:
	void Write(uint8_t *data, int samples)
	{
		OSStatus status = -1;
		std::unique_lock<std::mutex> lk(m_mainmutex);

		while (samples > 0)
		{
			if (m_quiting)
			{
				lk.unlock();
				return;
			}
			if (m_full)
			{
				m_maincond.wait(lk);
			}
			else
			{
				int tl;
				do
				{
					tl = std::min(m_writelen - m_wpos, m_bufferlen);

					if (tl == 0 && m_bufferpos<m_buffers.size())
					{
						if (!m_buffered)
						{
							printf("sound bufferd\n");
							m_buffered = true;
							m_callback();
						}
						m_full = true;
						m_maincond.notify_all();
					}
					else
					{
						memcpy(&_buffer.get()[m_wpos], data, tl);
						m_wpos += tl; data += tl; samples -= tl / (m_samplesize*m_channels);

						if (m_bufferpos!=-1 && m_wpos >= m_bufferlen) // preroll?
						{
							AudioQueueBufferRef b = m_buffers[m_bufferpos++];

							b->mAudioDataByteSize = m_bufferlen;
							memcpy(b->mAudioData, &_buffer.get()[m_wpos], m_bufferlen);

							if ((status = AudioQueueEnqueueBuffer(queue, b, 0, 0))) {
								printf("AudioQueueEnqueueBuffer status = %d\n", status);
								exit(1);
							}
							m_empty = (m_bufferlen == m_wpos);
							memmove(_buffer.get(), &_buffer.get()[m_bufferlen], m_bufferlen);
							m_wpos -= m_bufferlen;
							m_full = false;
					//		m_maincond.notify_all();

							if (m_bufferpos==m_buffers.size()) // preroll done?
							{
								printf("sound queued\n");
								m_bufferpos =-1;
							//	m_maincond.notify_all();
							}
						}
						else if (m_bufferpos ==-1)
						{
							m_empty = false;
							m_maincond.notify_all();
						}
					}
				} while (!m_full && samples > 0);
			}
		}
	}
	void Start()
	{
		OSStatus status = -1;
		std::unique_lock<std::mutex> lk(m_mainmutex);

		assert(m_bufferpos == -1);

		m_stopped = false;
		
		status=AudioQueueStart(queue, 0);
	}
	void Stop()
	{
		OSStatus status = -1;
		std::unique_lock<std::mutex> lk(m_mainmutex);

		status = AudioQueueStop(queue, 0);

		while (!m_stopped)
		{
			m_maincond.wait(lk);
		}
	}
	void SetBufferedCallback(osx_sound::BufferedFucnction callback)
	{
		m_callback = callback;
	}
};


extern "C" {

	osx_sound *openaudio(int bitrate, AudioFormat format, ChannelsLayout channels, int frames, int buffers, char *error)
	{
		try
		{
			auto result = new osx_sound(bitrate, format, channels, frames, buffers);

			return result;
		}
		catch (_err *e)
		{
#pragma warning (suppress:4996)
			strcpy(error, e->m_text.c_str());
			delete e;
			return 0;
		}
	}
	void closeaudio(osx_sound* p)
	{
		if (p) {
			delete p;
		}
	}
	void audio_write(osx_sound * audio, void *data, int leninsamples)
	{
		audio->Write((uint8_t*)data, leninsamples);
	}
	int audio_bufsize(osx_sound * audio)
	{
		return audio->m_writelen;
	}
	void audio_start(osx_sound * audio)
	{
		audio->Start();
	}
	void audio_stop(osx_sound * audio)
	{
		audio->Stop();
	}
	void audio_setcallback(osx_sound *audio, osx_sound::BufferedFucnction callback)
	{
		audio->SetBufferedCallback(callback);
	}
}

#endif