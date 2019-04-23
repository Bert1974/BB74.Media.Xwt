#include "stdafx.h"
#include "movieplayer.h"
#include "movierecorder.h"

float RootMeanSquare(float *values, int start, int end, int channels)
{
	double s = 0;

	for (int i = start; i < end; i += channels)
	{
		s += values[i] * values[i];
	}
	return (float)sqrt(s / ((end - start) / channels));
}
float RootMeanSquare(short *values, int start, int end, int channels)
{
	double s = 0;

	for (int i = start; i < end; i += channels)
	{
		s += values[i] * values[i];
	}
	return (float)sqrt(s / ((end - start) / channels));
}


extern "C" {

	FUNCEXP movierecorder *openrecorder(const char *fname, char *error)
	{
		try
		{
			auto result = new movierecorder(fname, "");
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
	FUNCEXP movierecorder *openrecorder2(movierecorder::PushPacketFunction func, char *error)
	{
		try
		{
			auto result = new movierecorder(func, "");
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
	FUNCEXP void destroyrecorder(movierecorder *recorder)
	{
		if (recorder)
		{
			delete recorder;
		}
	}
	FUNCEXP	void recorder_setwritefunc(movierecorder *recorder, movierecorder::PushPacketFunction func)
	{
		recorder->SetWriteFunc(func);
	}
	FUNCEXP void recorder_start(movierecorder *recorder)
	{
		recorder->Start();
	}
	FUNCEXP void recorder_stop(movierecorder *recorder)
	{
		recorder->Stop();
	}
	FUNCEXP movierecorder::outputstream *recorder_addvideo(movierecorder *recorder, int width, int height, FPS *fps, VideoFormat fmt, char *error)
	{
		try
		{
			auto result = recorder->AddVideo(width, height, fps, fmt);
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
	FUNCEXP movierecorder::outputstream *recorder_addaudio(movierecorder *recorder, int bitrate, int samplerate, ChannelsLayout channels, AudioFormat audiofmt, char *error)
	{
		try
		{
			auto result = recorder->AddAudio(bitrate, samplerate, channels, audiofmt);
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
	FUNCEXP void recorder_video_push(movierecorder::outputstream *stream, uint8_t* data, int stride, int width, int height, VideoFormat fmt, int64_t time)
	{
		stream->Push(data, stride, width, height, fmt, time);
	}
	FUNCEXP void recorder_video_push2(movierecorder::outputstream *stream, AVFrame *frame)
	{
		stream->pushframe(frame);
	}
	FUNCEXP void recorder_audio_push(movierecorder::outputstream *stream, int64_t time, uint8_t *audiodata, int totalsamples, AudioFormat fmt)
	{
		stream->Push(time, audiodata, totalsamples, fmt);
	}


	FUNCEXP movieplayer *openplayer(const char *fname, char *error)
	{
		try
		{
			auto result = new movieplayer(fname);
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
	FUNCEXP void player_set_callbacks(movieplayer *player, EOSFunction eosfunction, FlushedFunction flushedfunction)
	{
		player->SetCallBacks(eosfunction, flushedfunction);
	}

	FUNCEXP void destroyplayer(movieplayer *player)
	{
		if (player)
		{
			delete player;
		}
	}
	FUNCEXP int _player_videostreamcount(movieplayer *player)
	{
		return player->get_videostreamcount();
	}
	FUNCEXP void _player_get_videostream(movieplayer *player, uint ind, videostreaminfo *info)
	{
		player->get_videostream(ind, info);
	}
	FUNCEXP videostream *_player_openvideo(movieplayer *player, uint ind, FrameReadyFunction frameready, char *error)
	{
		try
		{
			return player->open_videostream(ind, frameready);
		}
		catch (_err *e)
		{
#pragma warning (suppress:4996)
			strcpy(error, e->m_text.c_str());
			delete e;
			return 0;
		}
	}
	FUNCEXP void _player_run(movieplayer *player, int64_t time, int64_t timebase)
	{
		player->seeknow(time, timebase);
		player->run(true);
	}
	FUNCEXP void _player_seek(movieplayer *player, int64_t time, int64_t timebase)
	{
		player->seeknow(time, timebase);
		player->run(false);
	}
	FUNCEXP void _player_preparestop(movieplayer *player)
	{
		player->preparestop();
	}
	FUNCEXP void _player_stop(movieplayer *player)
	{
		player->stop();
	}
	FUNCEXP int64_t _player_duration(movieplayer *player, int64_t timebase)
	{
		return player->duration(timebase);
	}
	FUNCEXP videoframe *_player_vid_allocframe(videostream *stream, FrameAllocateFunction framealloc, FrameLockFunction lockfunc, FrameUnLockFunction unlockfunc)
	{
		return stream->allocframe(framealloc, lockfunc, unlockfunc);
	}
	FUNCEXP void _player_vid_fillframe(videostream *stream, videoframe *frame, AVFrame *src, int64_t *time)
	{
		stream->fillframe(frame, src, time);
	}
	FUNCEXP void _player_vidframe_freeframe(videoframe *frame)
	{
		if (frame)
		{
			delete frame;
		}
	}
	FUNCEXP void _player_vidframe_freeavframe(AVFrame *frame)
	{
		if (frame)
		{
			av_frame_free(&frame);
		}
	}
	FUNCEXP int _player_audiostreamcount(movieplayer *player)
	{
		return player->get_audiostreamcount();
	}
	FUNCEXP void _player_get_audiostream(movieplayer *player, uint ind, audiostreaminfo *info)
	{
		player->get_audiostream(ind, info);
	}
	FUNCEXP audiostream *_player_openaudio(movieplayer *player, uint ind, int samplerate, AudioFormat fmt, ChannelsLayout channels, AudioFrameReadyFunction frameready, char *error)
	{
		try
		{
			return player->open_audiostream(ind, fmt, samplerate, channels, frameready);
		}
		catch (_err *e)
		{
#pragma warning (suppress:4996)
			strcpy(error, e->m_text.c_str());
			delete e;
			return 0;
		}
	}
	FUNCEXP void Add2BufferFloat(float *s, float *d, int totsamples, int schannels, int dchannels, bool mono, float volume)
	{
		if (mono)
		{
			for (int nit = 0; nit < totsamples; nit++)
			{
				for (int njt = 0; njt < dchannels - 1; njt++)
				{
					*d++ += *s*volume;
				}
				*d++ += *s++*volume;
			}
		}
		else
		{
			int ch = std::min(schannels, dchannels), doff = std::max(dchannels - schannels, 0), soff = std::max(schannels - dchannels, 0);

			for (int nit = 0; nit < totsamples; nit++)
			{
				for (int njt = ch; njt > 0; njt--)
				{
					*d++ += *s++*volume;
				}
				d += doff;
				s += soff;
			}
		}
	}
	FUNCEXP void Add2BufferShort(short*s, short *d, int totsamples, int schannels, int dchannels, bool mono, float volume)
	{
		if (mono)
		{
			for (int nit = 0; nit < totsamples; nit++)
			{
				for (int njt = 0; njt < dchannels - 1; njt++)
				{
					*d++ += *s*volume;
				}
				*d++ += *s++*volume;
			}
		}
		else
		{
			int ch = std::min(schannels, dchannels), doff = std::max(dchannels - schannels, 0), soff = std::max(schannels - dchannels, 0);

			for (int nit = 0; nit < totsamples; nit++)
			{
				for (int njt = ch; njt > 0; njt--)
				{
					*d++ += *s++*volume;
				}
				d += doff;
				s += soff;
			}
		}
	}
	FUNCEXP void RootMeanSquareFloat(float *dst, float *values, int length, int channels)
	{
		for (int nit = 0; nit < channels; nit++)
		{
			dst[nit] = RootMeanSquare(values, nit, length / 4, channels);
		}
	}
	FUNCEXP void RootMeanSquareShort(float *dst, short *values, int length, int channels)
	{
		for (int nit = 0; nit < channels; nit++)
		{
			dst[nit] = RootMeanSquare(values, nit, length / 4, channels);
		}
	}
}

std::string string_format(const std::string fmt, ...) {
	int size = ((int)fmt.size()) * 2 + 50;   // Use a rubric appropriate for your code
	std::string str;
	va_list ap;
	while (1) {     // Maximum two passes on a POSIX system...
		str.resize(size);
		va_start(ap, fmt);
		int n = vsnprintf((char *)str.data(), size, fmt.c_str(), ap);
		va_end(ap);
		if (n > -1 && n < size) {  // Everything worked
			str.resize(n);
			return str;
		}
		if (n > -1)  // Needed size returned
			size = n + 1;   // For null char
		else
			size *= 2;      // Guess at a larger size (OS specific)
	}
	return str;
}
std::string ERRSTR(int error)
{
	char buffer[2048];
	av_strerror(error, buffer, sizeof(buffer) / sizeof(buffer[0]));
	return buffer;
}
