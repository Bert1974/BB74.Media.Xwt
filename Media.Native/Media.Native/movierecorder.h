#pragma once

#include "media.h"
#include "audioconvert.h"


class movierecorder
{
public:
	typedef int(__stdcall *PushPacketFunction)(uint8_t *buf, int buf_size);
	class outputstream
	{
	public:
		int nb_samples;
		movierecorder * m_owner;
		AVCodec *m_codec;
		AVStream *m_st;
		AVCodecContext *m_enc;
		/* pts of the next frame that will be generated */
		int64_t m_next_pts;
		//int samples_count;
		AVFrame *m_frame, *m_tmp_frame;
		//float t, tincr, tincr2;
		//	struct SwsContext *sws_ctx;
		//	struct SwrContext *swr_ctx;
		/*AVCodecContext *m_codec;
		AVStream *m_stream;
		AVCodec *_codec;*/
		std::unique_ptr< AudioConverter> m_converter1, m_converter2;
		float *m_convert;
		int m_writepos;

		int m_rate, m_channels;
		ChannelsLayout m_channelslayout;

	public:
		outputstream(movierecorder *owner) : m_owner(owner)
			, m_codec(0), m_st(0), m_enc(0), m_frame(0), m_tmp_frame(0), m_next_pts(0)
	 	    ,m_convert(0)
		{
		}
		~outputstream()
		{
			if (m_convert)
			{
				delete[] m_convert;
			}

			avcodec_free_context(&m_enc);

			av_frame_free(&m_frame);

			av_frame_free(&m_tmp_frame);
			//	sws_freeContext(ost->sws_ctx);
			//	swr_free(&ost->swr_ctx);
		}
	private:
		static AVFrame *alloc_picture(enum AVPixelFormat pix_fmt, int width, int height)
		{
			AVFrame *picture;
			int ret;
			picture = av_frame_alloc();
			if (!picture)
				return NULL;
			picture->format = pix_fmt;
			picture->width = width;
			picture->height = height;
			/* allocate the buffers for the frame data */
			ret = av_frame_get_buffer(picture, 32);
			if (ret < 0) {
				av_frame_unref(picture);
				return NULL;
			}
			return picture;
		}
		AVFrame *alloc_audio_frame(enum AVSampleFormat sample_fmt, uint64_t channel_layout, int sample_rate, int nb_samples)
		{
			AVFrame *frame = av_frame_alloc();
			int ret;
			if (!frame) {
				throw new _err("Error allocating an audio frame");
			}
			frame->format = sample_fmt;
			frame->channel_layout = channel_layout;
			frame->sample_rate = sample_rate;
			frame->nb_samples = nb_samples;
			if (nb_samples) {
				ret = av_frame_get_buffer(frame, 0);
				if (ret < 0) {
					throw new _err("Error allocating an audio buffer\n");
				}
			}
			return frame;
		}

		int write_frame(AVFormatContext *fmt_ctx, const AVRational *time_base, AVStream *st, AVPacket *pkt);
	public:
		void open_video();
		void open_audio();
		void Push(uint8_t* data, int stride, int width, int height, VideoFormat fmt, int64_t time);
		void Push(int64_t time, uint8_t *audiodata, int totalsamples, AudioFormat fmt);
		void pushframe(AVFrame *frame);
		void Clear();
	};
//	FFMPEG^ m_ffmpeg;
//	IMixer^ m_mixer;
private:
	std::string m_fname;

	AVFormatContext *m_oc;
	AVDictionary *m_opt;
	AVIOContext *m_writer;
	PushPacketFunction m_writefunc;

	std::vector< std::unique_ptr<outputstream>> m_video, m_audio;
	uint8_t *m_buffer;
public:
	movierecorder(const char *filename, const char *options);
	movierecorder(movierecorder::PushPacketFunction func, const char *options);
	~movierecorder();
public:
	void SetWriteFunc(PushPacketFunction func)
	{
		m_writefunc = func;
	}
	outputstream * AddAudio(int bitrate, int samplerate, ChannelsLayout channels, AudioFormat audiofmt);
	outputstream * AddVideo(int width, int height, FPS *rate, VideoFormat pixelformat);
	//void AddAudio(int rate);
	void Start();
	void Stop();
private:
	void getopts(const char *options);
	void add_stream(outputstream **ost, AVFormatContext *oc, enum AVCodecID codec_id);
public:
//	void Push(IVideoFrame^ frame, Int64 time);
//	void Push(array<Byte>^ audio, int totalsamples);
	//	void encode(AVFrame *frame);
	/*void Stop();*/
public:
	//	property int SampleSize {int get() { return m_audio->nb_samples; }}
	//	property int SampleRate {int get() { return m_audio->m_enc->sample_rate; 

	void Clear();
public:
/*	Int64 Time(Int64 frame)
	{
		return frame * m_video->m_enc->time_base.num / m_video->m_enc->time_base.den;
	}*/

	static int write_packet(void *opaque, uint8_t *buf, int buf_size);
};