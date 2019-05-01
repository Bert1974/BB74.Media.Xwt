#include "stdafx.h"
#include "media.h"

#ifdef IS_LINUX_BUILD
#define ALSA_PCM_NEW_HW_PARAMS_API
#include <alsa/asoundlib.h>

class linux_sound
{
public:
	typedef void(__stdcall *BufferedFucnction)();
public:
	snd_pcm_t *handle;
	int m_wpos, m_writelen;
	std::unique_ptr<char[]> _buffer;
	int dir;
	snd_pcm_uframes_t frames;

	std::unique_ptr<std::thread> m_mainloop;
	std::mutex m_mainmutex;
	std::condition_variable m_maincond;

	bool m_empty, m_full, m_quiting, m_quitted, m_playing, m_buffered;

	AudioFormat m_format;
	int m_samplesize,m_channels, m_frames;
	ChannelsLayout m_channelslayout;

	BufferedFucnction m_callback;

	linux_sound(int samplerate, AudioFormat format, ChannelsLayout channels, int __frames, int buffers)
		: handle(0), m_empty(true), m_full(false), m_quiting(false), m_quitted(false)
		, m_format(format), m_channelslayout(channels), m_frames(buffers)
		, m_playing(false), m_buffered(false)
	{
		int rc;
		unsigned int val, val2;
		snd_pcm_hw_params_t *params=0;
	//	snd_pcm_sw_params_t *swparams=0 ;

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
		try
		{
			/* Open PCM device for playback. */
			rc = snd_pcm_open(&handle, "default", SND_PCM_STREAM_PLAYBACK, SND_PCM_NONBLOCK);
			if (rc < 0) {
				fprintf(stderr,
					"unable to open pcm device: %s\n",
					snd_strerror(rc));

				throw new _err("error opening (snd_pcm_open)");
			}

			/* Allocate a hardware parameters object. */
			snd_pcm_hw_params_alloca(&params);
		//	snd_pcm_sw_params_alloca(&swparams);

			/* Fill it in with default values. */
			snd_pcm_hw_params_any(handle, params);
		//	snd_pcm_sw_params_current(handle, swparams);

			/* Set the desired hardware parameters. */

			/* Interleaved mode */
			snd_pcm_hw_params_set_access(handle, params, SND_PCM_ACCESS_RW_INTERLEAVED);

			switch (m_format)
			{
			case AudioFormat::Float32:
				/* Signed 16-bit little-endian format */
				snd_pcm_hw_params_set_format(handle, params, SND_PCM_FORMAT_FLOAT_LE);
				m_samplesize = 4;
				break;
			case AudioFormat::Short16:
				/* Signed 16-bit little-endian format */
				snd_pcm_hw_params_set_format(handle, params, SND_PCM_FORMAT_S16_LE);
				m_samplesize = 2;
				break;
			default:
				throw new _err("unimplemented");
			}


			/* Two channels (stereo) */
			snd_pcm_hw_params_set_channels(handle, params, m_channels);

			/* 44100 bits/second sampling rate (CD quality) */
			val = samplerate;
			snd_pcm_hw_params_set_rate_near(handle, params, &val, &dir);

			uint bufsize = m_frames * (samplerate / __frames)* m_samplesize*m_channels;

			/* Use a buffer large enough to hold one period */
			snd_pcm_hw_params_set_buffer_time_near(handle, params, &bufsize, &dir);

			/* Write the parameters to the driver */
			rc = snd_pcm_hw_params(handle, params);

			if (rc < 0) {
				fprintf(stderr,
					"unable to set hw parameters: %s\n",
					snd_strerror(rc));

				throw new _err("error initializing(snd_pcm_hw_params)");
			}

			snd_pcm_hw_params_get_period_size(params, &frames, &dir);

		/*	rc = snd_pcm_sw_params_set_start_threshold(handle, swparams, ((samplerate / __frames+ frames -1) / frames) * frames);

			if (rc < 0) {
				fprintf(stderr,
					"unable to set sw parameters: %s\n",
					snd_strerror(rc));

				throw new _err("error initializing (snd_pcm_sw_params_set_start_threshold)");
			}

			rc = snd_pcm_sw_params(handle, swparams);
			if (rc < 0) {
				fprintf(stderr,
					"unable to set sw parameters: %s\n",
					snd_strerror(rc));

				throw new _err("error initializing (snd_pcm_sw_params)");
			}*/

			m_writelen = m_frames * (samplerate/ __frames) * m_samplesize*m_channels; /* 4 bytes/sample, 2 channels */

			_buffer.reset(new char[m_writelen]);
			m_wpos = 0;

		//	snd_pcm_hw_params_free(params);
		//	snd_pcm_sw_params_free(swparams);
			     
			m_mainloop.reset(new std::thread(&linux_sound::threadfunc, this));

			SETTHREADNAME(m_mainloop.get(), "sound main");
		}
		catch(_err*)
		{
		//	snd_pcm_hw_params_free(params);
		//	snd_pcm_sw_params_free(swparams);

			snd_pcm_close(handle);
			handle = 0;

			throw;
		}
	}
	~linux_sound()
	{
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
		snd_pcm_close(handle);
		handle= 0;

		delete[] _buffer.get();
		_buffer.release();

	}
private:
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
public:
	void Write(uint8_t *data, int samples)
	{
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
					tl = std::min(m_writelen - m_wpos, samples * m_samplesize*m_channels);

					if (tl == 0)
					{
						if (!m_buffered)
						{
							m_buffered = true;
							m_callback();
						}
						m_full = true;
					}
					else
					{
						memcpy(&_buffer.get()[m_wpos], data, tl);
						m_wpos += tl; data += tl; samples -= tl / (m_samplesize*m_channels);
						m_empty = false;
						m_maincond.notify_all();
					}
				} while (!m_full && samples > 0);
			}
		}
	}
	void Start()
	{
		std::unique_lock<std::mutex> lk(m_mainmutex);

		m_playing = true;
		m_maincond.notify_all();
	}
	void Stop()
	{
		std::unique_lock<std::mutex> lk(m_mainmutex);

		m_playing = false;
		m_buffered = false;
		m_maincond.notify_all();
	}
	void SetBufferedCallback(linux_sound::BufferedFucnction callback)
	{
		m_callback = callback;
	}
};


extern "C" {

	linux_sound *openaudio(int bitrate, AudioFormat format, ChannelsLayout channels, int frames, int buffers, char *error)
	{
		try
		{
			auto result = new linux_sound(bitrate, format, channels, frames, buffers);

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
	void closeaudio(linux_sound* p)
	{
		if (p) {
			delete p;
		}
	}
	void audio_write(linux_sound * audio, void *data, int leninsamples)
	{
		audio->Write((uint8_t*)data, leninsamples);
	}
	int audio_bufsize(linux_sound * audio)
	{
		return audio->m_writelen;
	}
	void audio_start(linux_sound * audio)
	{
		audio->Start();
	}
	void audio_stop(linux_sound * audio)
	{
		audio->Stop();
	}
	void audio_setcallback(linux_sound *audio, linux_sound::BufferedFucnction callback)
	{
		audio->SetBufferedCallback(callback);
	}
}

#endif