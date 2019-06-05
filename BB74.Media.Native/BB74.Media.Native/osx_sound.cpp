#include "stdafx.h"
#include "media.h"

#include <algorithm>
#include <assert.h>

#include <iostream>

#if (0)
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
	int m_wpos;
	std::unique_ptr<char[]> m_buffer;
	int frames, m_bufferlen, m_writelen, m_bufferpos;
	std::mutex m_mainmutex;
	std::condition_variable m_maincond;

	bool m_empty, m_full, m_quiting, m_quitted, m_buffered, m_stopped;

	AudioFormat m_format;
	int m_samplesize,m_channels, m_frames;
	ChannelsLayout m_channelslayout;

	BufferedFucnction m_callback;

	AudioQueueRef m_queue;
	std::vector<AudioQueueBufferRef> m_buffers;

private:
	void Free()
	{
		OSStatus status = -1;
		if (m_queue)
		{
			while (!m_buffers.empty())
			{
				AudioQueueFreeBuffer(m_queue, *m_buffers.begin());
				m_buffers.erase(m_buffers.begin());
			}
			for (auto it = m_buffers.begin(); it != m_buffers.end(); ++it) {
			}
			status = AudioQueueDispose(m_queue, true);
			m_queue = 0;
		}
	}
	std::string error(OSStatus error)
	{
		switch (error)
		{
		case kAudioQueueErr_InvalidBuffer:return "The specified audio queue buffer does not belong to the specified audio queue.";
		case kAudioQueueErr_BufferEmpty:return "The audio queue buffer is empty(that is, the mAudioDataByteSize field = 0).";
		case kAudioQueueErr_DisposalPending:return "The function cannot act on the audio queue because it is being asynchronously disposed of.";
		case kAudioQueueErr_InvalidProperty:return "The specified property ID is invalid.";
		case kAudioQueueErr_InvalidPropertySize: return "The size of the specified property is invalid.";
		case kAudioQueueErr_InvalidParameter: return "The specified parameter ID is invalid.";
		case kAudioQueueErr_CannotStart:return "The audio queue has encountered a problem and cannot start.";
		case kAudioQueueErr_InvalidDevice:return "The specified audio hardware device could not be located.";
		case kAudioQueueErr_BufferInQueue:return "The audio queue buffer cannot be disposed of when it is enqueued.";
		case kAudioQueueErr_InvalidRunState: return "The queue is running but the function can only operate on the queue when it is stopped, or vice versa.";
		case kAudioQueueErr_InvalidQueueType:return "The queue is an input queue but the function can only operate on an output queue, or vice versa.";
		case kAudioQueueErr_Permissions:return "You do not have the required permissions to call the function.";
		case kAudioQueueErr_InvalidPropertyValue:return "The property value used is not valid.";
		case kAudioQueueErr_PrimeTimedOut:return "During a call to the AudioQueuePrime function, the audio queue's audio converter failed to convert the requested number of sample frames.";
		case kAudioQueueErr_CodecNotFound:return "The requested codec was not found.";
		case kAudioQueueErr_InvalidCodecAccess:return "The codec could not be accessed.";
		case kAudioQueueErr_QueueInvalidated:return "In iOS, the audio server has exited, causing the audio queue to become invalid.";
		case kAudioQueueErr_RecordUnderrun:return "During recording, data was lost because there was no enqueued buffer to store it in.";
		case kAudioQueueErr_EnqueueDuringReset:return "During a call to the AudioQueueReset, AudioQueueStop, or AudioQueueDispose functions, the system does not allow you to enqueue buffers.";
		case kAudioQueueErr_InvalidOfflineMode:return "The operation requires the audio queue to be in offline mode but it isn't, or vice versa.";
		case kAudioFormatUnsupportedDataFormatError:return "The playback data format is unsupported";
		}
		return string_format("error=%d", error);
	}
private:
	static void listener(void *data, AudioQueueRef queue, AudioQueuePropertyID id)
	{
		((osx_sound*)data)->_listener(id);
	}
	void _listener(AudioQueuePropertyID id)
	{
		if (id == kAudioQueueProperty_IsRunning) {
			// Here, we're only listening for start/stop, so don't need to check
			// the id; it's always kAudioQueueProperty_IsRunning in our case.

			UInt32 running = 0;
			UInt32 size = sizeof running;
			/*   OggVorbis_File *vf = (OggVorbis_File *) vorbis; */
			OSStatus status = -1;

			status = AudioQueueGetProperty(m_queue, id, &running, &size);

			if (!running) {
				std::unique_lock<std::mutex> lk(m_mainmutex);
				m_stopped = true;
				m_maincond.notify_all();

				// In a "real example" we'd clean up the vf pointer with ov_clear() and
				// the audio m_queue with AudioQueueDispose(); however, the latter is 
				// better not called from within the listener function, so we just
				// exit normally.
			//	exit(0);
				// In a "real" application, we might signal the termination with
				// a pthread condition variable, or something similar, instead;
				// where the waiting thread would call AudioQueueDispose().  It is 
				// "safe" to call ov_clear() here, but there's no point.
			}else{
			}
		}
	}
	// The audio m_queue callback...
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
						memcpy(&((char*)buffer->mAudioData)[wpos], &m_buffer.get()[rpos], tot);

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
					memmove(m_buffer.get(), &m_buffer.get()[rpos], m_wpos - rpos);
					m_wpos -= rpos;
					m_full = false;
					m_maincond.notify_all();
				}
			}
		}
		OSStatus status = -1;
		if ((status = AudioQueueEnqueueBuffer(m_queue, buffer, 0, 0))) {
			__printf("AudioQueueEnqueueBuffer status = %d", status);
		}
	}
public:
	osx_sound(int samplerate, AudioFormat format, ChannelsLayout channels, int __frames, int buffers)
		: m_empty(true), m_full(false), m_quiting(false), m_quitted(false), m_stopped(true)
		, m_format(format), m_channelslayout(channels), m_frames(buffers)
		, m_buffered(false)
		, m_queue (0), frames(samplerate/ __frames)
	{
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
				fmt.mFormatFlags = kAudioFormatFlagIsSignedInteger | kAudioFormatFlagIsPacked;
				fmt.mBitsPerChannel = 16;
				m_samplesize = 2;
				break;
			default:
				throw new _err("unimplemented");
			}
			fmt.mSampleRate = samplerate;
			fmt.mFormatID = kAudioFormatLinearPCM;
			fmt.mFramesPerPacket = 1;
			fmt.mBytesPerFrame = m_channels * m_samplesize;
			fmt.mChannelsPerFrame = m_channels; // 2 for stereo
			fmt.mBytesPerPacket = m_channels* m_samplesize; // x2 for stereo

			// Create the audio queue with the desired format.
			status=AudioQueueNewOutput(&fmt, callback, this, NULL, NULL, 0, &m_queue);
			
			if (status)
			{
				throw new _err("AudioQueueNewOutput status = %s\n", error(status).c_str());
			}

			m_bufferlen = frames * m_samplesize*m_channels;

			m_buffer.reset(new char[(m_writelen=(m_bufferlen * 3))]);

			for (int nit = 0; nit < 5; nit++)
			{
				AudioQueueBufferRef b = 0;
				status = AudioQueueAllocateBuffer(m_queue, m_bufferlen, &b);

				if (status)
				{
					throw new _err("AudioQueueAllocateBuffer status = %d\n", status);
				}
				m_buffers.push_back(b);
			}
			m_bufferpos = 0;
			
			status = AudioQueueAddPropertyListener(m_queue, kAudioQueueProperty_IsRunning, listener, this);
			
			if (status)
			{
				throw new _err("AudioQueueAddPropertyListener status = %d\n", status);
			}
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
		if (m_queue)
		{
			OSStatus status = -1;
			{
		/*		std::unique_lock<std::mutex> lk(m_mainmutex);
				m_quiting = true;
				m_maincond.notify_all();

				while (!m_quitted)
				{
					m_maincond.wait(lk);
				}
				m_mainloop->join();*/
			}
			Free();

			delete[] m_buffer.get();
			m_buffer.release();
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

					auto rc = snd_pcm_writei(handle, &m_buffer.get()[pos], frames);

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
					memmove(m_buffer.get(), &m_buffer.get()[pos], m_wpos - pos);
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
			__printf("write %d", samples);

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

					if (tl == 0 && m_bufferpos==-1)
					{
						if (!m_buffered)
						{
							__printf("sound bufferd");
							m_buffered = true;
							m_callback();
						}
						m_full = true;
						m_maincond.notify_all();
					}
					else
					{
						__printf("write copy to buffer len=%d", tl);
						memcpy(&m_buffer.get()[m_wpos], data, tl);
						m_wpos += tl; data += tl; samples -= tl / (m_samplesize*m_channels);

						while (m_wpos >= m_bufferlen)
						{
							__printf("write got full buffer");
							if (m_bufferpos != -1) // preroll?
							{
								AudioQueueBufferRef b = m_buffers[m_bufferpos++];

								b->mAudioDataByteSize = m_bufferlen;
								memcpy(b->mAudioData, &m_buffer.get()[m_wpos], m_bufferlen);

								if ((status = AudioQueueEnqueueBuffer(m_queue, b, 0, 0))) {
									__printf("AudioQueueEnqueueBuffer status = %d", status);
									//exit(1);
								}
								memmove(m_buffer.get(), &m_buffer.get()[m_bufferlen], m_writelen-m_bufferlen);
								m_wpos -= m_bufferlen;
								m_empty = (0 == m_wpos);
								//		m_maincond.notify_all();

								__printf("write prrerolled");
								if (m_bufferpos == m_buffers.size()) // preroll done?
								{
									__printf("sound queued");
									m_bufferpos = -1;
									//	m_maincond.notify_all();
								}
							}
							else if (m_bufferpos == -1)
							{
								m_empty = false;
								m_maincond.notify_all();
								break;
							}
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

		
		status=AudioQueueStart(m_queue, 0);
	}
	void Stop()
	{
		OSStatus status = -1;


		status = AudioQueueStop(m_queue, 0);

		__printf("audio stopped?");
		std::unique_lock<std::mutex> lk(m_mainmutex);
		m_quiting = true;
		m_maincond.notify_all();
		while (!m_stopped)
		{
			m_maincond.wait(lk);
		}
		m_full = false;
		m_empty = true;
		m_bufferpos = 0;
		m_wpos = 0;
		m_quiting = false;
		m_stopped = false;
		m_buffered = false;
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

			__printf("audioptr=%lx", (long)result);

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
