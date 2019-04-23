#pragma once

#include "media.h"
#include "audioconvert.h"

class movieplayer;
class videostream;

extern "C" {
	typedef bool(__stdcall *FrameReadyFunction)(AVFrame *frame, int64_t time, int64_t duration);
	typedef void(__stdcall *FrameAllocateFunction)(videostream *stream, int64_t time, int64_t duration, int w, int h, VideoFormat fmt, uint8_t **data, int *pitch, VideoFormat *fmtframe);
	typedef void(__stdcall *FrameLockFunction)(videostream *stream, uint8_t **data, int *pitch);
	typedef void(__stdcall *FrameUnLockFunction)();
	typedef void(__stdcall *AudioFrameReadyFunction)(int64_t time, void *data, int samplecnt);
	typedef void(__stdcall *EOSFunction)();
	typedef void(__stdcall *FlushedFunction)();
}

class basestream
{
public:
	int m_streamid;
	movieplayer *m_owner;
	AVCodecContext *m_codec;
	AVStream *m_stream;
	AVCodec *_codec;
	AVFrame * m_avFrame;
	uint64_t m_duration;
	uint64_t m_last_pts;
	uint64_t m_last_time;
public:
	basestream(movieplayer *owner, int streamnumber, AVCodec *codec);
	virtual ~basestream();
	virtual void EOS() = 0;
	virtual void Flushed() = 0;

	std::unique_ptr<std::thread> m_thread;
	std::vector<AVPacket*> m_packets;

	std::mutex m_mutex;
	std::condition_variable m_waitcondition, m_quitedcondition;

	bool m_full, m_empty, m_quit, m_quited;

public:
	virtual	void preparestop();
	virtual	void run();
	virtual	void stop();
	virtual void waitend();
	virtual void flush();
	virtual void flushnow();
	virtual void newpacket(AVPacket **packet);
	void decodenextpacket(AVPacket *packet);
	virtual void FrameReady() = 0;
private:
	void _run();
};

class videoframe
{
public:
	VideoFormat _fmt;
	uint8_t *m_data;
	bool m_allocated;
	int m_pitch;
	FrameAllocateFunction m_allocfunc;
	FrameLockFunction m_lockfunc;
	FrameUnLockFunction m_unlockfunc;
public:
	videoframe(FrameAllocateFunction allocfunc, FrameLockFunction lockfunc, FrameUnLockFunction unlockfunc);
	virtual ~videoframe();
public:
	void alloc(videostream *stream);
	void lock(videostream *stream);
	void unlock(videostream *stream);
};

class videostream : public basestream
{
public:
	int m_id;
	FrameReadyFunction _frameready;

private:
	SwsContext* m_swsContext;
public:
	videostream(movieplayer *owner, int streamnumber, AVCodec *codec, FrameReadyFunction readyfunc);
	~videostream();
public:
	virtual void FrameReady() override;
	virtual void EOS() override;
	virtual void Flushed() override;
public:
	videoframe *allocframe(FrameAllocateFunction allocfunc, FrameLockFunction lockfunc, FrameUnLockFunction unlockfunc);
	void fillframe(videoframe *frame, AVFrame *src, int64_t *time);
	uint64_t Time(uint64_t time);
};

class audiostream : public basestream
{
public:
	int m_id;
	AudioFrameReadyFunction _frameready;
	uint m_nRate, m_nChannels, m_convertsizesamples, m_bufferlen, _samplesize;
	uint64_t m_nChannelMask;
	AudioFormat m_format;
private:
	std::unique_ptr<AudioConverter> m_converter;
//std::unique_ptr<soundtouch::SoundTouch> m_resample;
	std::unique_ptr<float[]> m_convert, m_buffer;
public:
	audiostream(movieplayer *owner, int streamnumber, AVCodec *codec,/* AudioFormat fmt,*/ int samplerate, AudioFormat fmt, ChannelsLayout channels, AudioFrameReadyFunction readyfunc);
	~audiostream();
public:
	virtual void FrameReady() override;
	virtual void Flushed() override;
	virtual void EOS() override;
public:
	uint64_t Time(uint64_t time);
};

class movieplayer
{
	void trylock(std::unique_ptr< std::lock_guard<std::mutex>>& lk)
	{
		if (CURRENTTHREADID() != m_lockhtreadid)
		{
			lk.reset(new std::lock_guard<std::mutex>(_mainmutex));
		}
	}
public:
	AVFormatContext *m_avicontext;
	std::unique_ptr<std::thread> _mainloop;

	std::mutex _mainmutex;
	std::condition_variable _maincond, _quittedcond;
	std::vector<std::unique_ptr<videostream>> m_videostreams;
	std::vector<std::unique_ptr<audiostream>> m_audiostreams;

	bool m_quiting, m_running, m_stopping, m_quitted;
	THREADTYPE m_lockhtreadid; // only used with m_eosfunction

	EOSFunction m_eosfunction;
	FlushedFunction m_flushedfunction;

public:
	movieplayer(const char *filename);
	~movieplayer();

public:
	void SetCallBacks(EOSFunction eosfunction, FlushedFunction flushedfunction)
	{
		m_eosfunction = eosfunction;
		m_flushedfunction = flushedfunction;
	}

public:
	int get_videostreamcount()
	{
		std::unique_ptr< std::lock_guard<std::mutex>> lk;
		trylock(lk);

		int result = 0;

		for (uint nit = 0; nit < m_avicontext->nb_streams; nit++)
		{
			if (m_avicontext->streams[nit]->codecpar->codec_type == AVMEDIA_TYPE_VIDEO)
			{
				result++;
			}
		}
		return result;
	}
	void get_videostream(uint ind, videostreaminfo *info)
	{
		std::unique_ptr< std::lock_guard<std::mutex>> lk;
		trylock(lk);

		for (uint nit = 0, ind2 = 0; nit < m_avicontext->nb_streams; nit++)
		{
			if (m_avicontext->streams[nit]->codecpar->codec_type == AVMEDIA_TYPE_VIDEO)
			{
				if (ind2++ == ind)
				{
					info->ind = nit;
					info->width = m_avicontext->streams[nit]->codecpar->width;
					info->height = m_avicontext->streams[nit]->codecpar->height;
					info->ticks = m_avicontext->streams[nit]->codecpar->field_order == AVFieldOrder::AV_FIELD_PROGRESSIVE ? 1 : 2;
					auto stream = m_avicontext->streams[nit];

					info->fps.num = stream->time_base.den;
					info->fps.den = stream->time_base.num;

					info->time_base.den = stream->time_base.den;
					info->time_base.num = stream->time_base.num;
					return;
				}
			}
		}
	}
	int get_audiostreamcount()
	{
		std::unique_ptr< std::lock_guard<std::mutex>> lk;
		trylock(lk);

		int result = 0;

		for (uint nit = 0; nit < m_avicontext->nb_streams; nit++)
		{
			if (m_avicontext->streams[nit]->codecpar->codec_type == AVMEDIA_TYPE_AUDIO)
			{
				result++;
			}
		}
		return result;
	}
	void get_audiostream(uint ind, audiostreaminfo *info)
	{
		std::unique_ptr< std::lock_guard<std::mutex>> lk;
		trylock(lk);

		for (uint nit = 0, ind2 = 0; nit < m_avicontext->nb_streams; nit++)
		{
			if (m_avicontext->streams[nit]->codecpar->codec_type == AVMEDIA_TYPE_AUDIO)
			{
				if (ind2++ == ind)
				{
					info->ind = nit;
					info->samplerate = m_avicontext->streams[nit]->codecpar->sample_rate;
					info->channels = m_avicontext->streams[nit]->codecpar->channels;
					info->channelmask = m_avicontext->streams[nit]->codecpar->channel_layout;
					auto stream = m_avicontext->streams[nit];

					info->fps.num = stream->time_base.den;
					info->fps.den = stream->time_base.num;

					info->time_base.den = stream->time_base.den;
					info->time_base.num = stream->time_base.num;

					switch (m_avicontext->streams[nit]->codecpar->format)
					{
					default:
						info->format = AudioFormat::Float32;
						break;
					}
					if (info->channelmask == 0) { info->channelmask = (1 << info->channels) - 1; }

					return;
				}
			}
		}
	}
	void seeknow(int64_t time, int64_t timebase)
	{
		std::unique_ptr< std::lock_guard<std::mutex>> lk;
		trylock(lk);

		if (m_running)
		{
			throw new _err("can't seek while playing");
		}
		else // basestream could be running
		{
			for (auto& it : m_videostreams)
			{
				it->flushnow();
			}
			for (auto& it : m_audiostreams)
			{
				it->flushnow();
			}
		}

		basestream *stream = 0;

		if (m_videostreams.begin() != m_videostreams.end())
		{
			stream = (m_videostreams.begin()->get());
		}
		else if (m_audiostreams.begin() != m_audiostreams.end())
		{
			stream = (m_audiostreams.begin()->get());
		}

		if (stream)
		{
			uint64_t streampos = (uint64_t)((time)*stream->m_stream->time_base.den / (timebase  * stream->m_stream->time_base.num));

			int err = avformat_seek_file(m_avicontext, stream->m_streamid, 0, streampos, streampos, 0);
			//	int err = av_seek_frame(m_avicontext, m_video->m_streamid, streampos,/*AVSEEK_FLAG_ANY|*/AVSEEK_FLAG_BACKWARD);

			for (auto it = m_audiostreams.begin(); it != m_audiostreams.end(); ++it) {
				(*it)->flush();
			}
			for (auto it = m_videostreams.begin(); it != m_videostreams.end(); ++it) {
				(*it)->flush();
			}
			if (m_flushedfunction) {
				m_flushedfunction();
			}
		}
	}
	videostream *open_videostream(uint ind, FrameReadyFunction readyfunc)
	{
		std::unique_ptr< std::lock_guard<std::mutex>> lk;
		trylock(lk);

		for (auto it = m_videostreams.begin(); it != m_videostreams.end(); ++it) {
			if ((*it)->m_id == ind)
			{
				throw new _err("stream already initalized");
			}
		}

		FFMPEGLOCK();

		AVCodec * decoder = 0;
		av_find_best_stream(m_avicontext, AVMEDIA_TYPE_VIDEO, ind, -1, &decoder, 0);

		videostream *vidstream;
		try
		{
			vidstream = new videostream(this, ind, decoder, readyfunc);

			FFMPEGUNLOCK();
		}
		catch (_err *e)
		{
			FFMPEGUNLOCK();
			delete e;
			return 0;
		}
		auto tot = m_videostreams.size();
		m_videostreams.resize(tot + 1);
		m_videostreams[tot].reset(vidstream);

		return vidstream;
	}
	audiostream *open_audiostream(uint ind, AudioFormat fmt, int samplerate, ChannelsLayout channels, AudioFrameReadyFunction readyfunc)
	{
		std::unique_ptr< std::lock_guard<std::mutex>> lk;
		trylock(lk);

		for (auto it = m_audiostreams.begin(); it != m_audiostreams.end(); ++it) {
			if ((*it)->m_id == ind)
			{
				throw new _err("stream already initalized");
			}
		}

		FFMPEGLOCK();

		AVCodec * decoder = 0;
		av_find_best_stream(m_avicontext, AVMEDIA_TYPE_AUDIO, ind, -1, &decoder, 0);

		audiostream *audstream;
		try
		{
			audstream = new audiostream(this, ind, decoder, samplerate, fmt, channels, readyfunc);

			FFMPEGUNLOCK();
		}
		catch (_err *e)
		{
			FFMPEGUNLOCK();
			delete e;
			return 0;
		}
		auto tot = m_audiostreams.size();
		m_audiostreams.resize(tot + 1);
		m_audiostreams[tot].reset(audstream);

		return audstream;
	}
	int64_t duration(int64_t timebase)
	{
		std::unique_ptr< std::lock_guard<std::mutex>> lk;
		trylock(lk);

		int64_t duration = 0;

		for (auto& it : m_videostreams) {
			auto dd = (int64_t)(it->m_stream->duration * timebase * av_q2d(it->m_stream->time_base));
			duration = std::max(dd, duration);
		}
		for (auto& it : m_audiostreams) {
			auto dd = (int64_t)(it->m_stream->duration * timebase * av_q2d(it->m_stream->time_base));
			duration = std::max(dd, duration);
		}
		if (duration == 0 && m_avicontext->nb_chapters > 0)
		{
			auto ch = m_avicontext->chapters[m_avicontext->nb_chapters - 1];
			duration = (ch->end*timebase*ch->time_base.num / ch->time_base.den);
		}
		return duration;
	}
	void preparestop();
	void stop();
	void run(bool startthreads);
	void _mainthread();
};

