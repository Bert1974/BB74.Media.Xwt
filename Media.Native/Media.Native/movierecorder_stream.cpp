#include "stdafx.h"
#include "movierecorder.h"

#include <assert.h>


movierecorder::outputstream::outputstream(movierecorder *owner) : m_owner(owner)
, m_codec(0), m_st(0), m_enc(0), m_frame(0), m_tmp_frame(0), m_next_pts(0)
, m_convert(0)
{
}

movierecorder::outputstream::~outputstream()
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

AVFrame *movierecorder::outputstream::alloc_picture(enum AVPixelFormat pix_fmt, int width, int height)
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
AVFrame *movierecorder::outputstream::alloc_audio_frame(enum AVSampleFormat sample_fmt, uint64_t channel_layout, int sample_rate, int nb_samples)
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
int movierecorder::outputstream::write_frame(AVFormatContext *fmt_ctx, const AVRational *time_base, AVStream *st, AVPacket *pkt)
{
	/* rescale output packet timestamp values from codec to stream timebase */
	av_packet_rescale_ts(pkt, *time_base, st->time_base);
	pkt->stream_index = st->index;

	/* Write the compressed frame to the media file. */
//	log_packet(fmt_ctx, pkt);
	return av_interleaved_write_frame(fmt_ctx, pkt);
}
void movierecorder::outputstream::Clear()
{
	m_writepos = 0;
	m_converter1.reset(new AudioConverter(AV_SAMPLE_FMT_FLT, m_channelslayout, m_rate, AV_SAMPLE_FMT_FLT, m_channelslayout, m_enc->sample_rate));
	m_converter2.reset(new AudioConverter(AV_SAMPLE_FMT_FLT, m_channelslayout, m_enc->sample_rate, m_enc->sample_fmt, m_enc->channel_layout, m_enc->sample_rate));
}
void movierecorder::outputstream::Push(int64_t time, uint8_t *audiodata, int totsamples, AudioFormat fmt)
{
	assert(fmt == AudioFormat::SampleFloat32);

	int ret;
	int len = m_rate - m_writepos;// totsamples * m_enc->sample_rate / 48000;

	uint8_t *ss[AV_NUM_DATA_POINTERS] = { audiodata,0,0,0,0,0,0 };

	uint8_t *dd[AV_NUM_DATA_POINTERS] = { (uint8_t*)&m_convert[m_writepos*m_channels],0,0,0,0,0,0 };

	int tot = m_converter1->Convert((uint8_t**)dd, &len, (const uint8_t**)ss, totsamples); // change bitrate
	m_writepos += tot;

	int pos = 0;

	while (m_writepos - pos >= nb_samples)
	{
		uint8_t *s2[AV_NUM_DATA_POINTERS] = { (uint8_t*)&m_convert[pos*m_channels],0,0,0,0,0,0 };

		//int len = nb_samples * c->sample_rate / 48000;

		//totsamples = audiodata->Length / 8;

		{
			ret = av_frame_make_writable(m_frame);
		}
		len = m_writepos;
		int tot = m_converter2->Convert(m_frame->data, &len, (const uint8_t**)s2, nb_samples); // change/downconvert channels

		pos += nb_samples;

		if (tot > 0) {
			//memcpy(m_frame->data[0], (Byte*)bp, totsamples * 4);
			m_frame->nb_samples = tot;

			m_frame->pts = m_next_pts;
			m_next_pts += m_frame->nb_samples;
			//	ost->samples_count += dst_nb_samples;
		//	}
			pushframe(m_frame);

			//	pos += nb_samples;
		}
		else
		{
			break;
		}

	}

	memmove(m_convert, &m_convert[pos * m_channels], (m_writepos - pos) * sizeof(float) * m_channels);
	m_writepos -= pos;
}

void movierecorder::outputstream::Push(uint8_t* data, int stride, int width, int height, VideoFormat fmt, int64_t time)
{
	if (m_frame)
	{
		int ret = av_frame_make_writable(m_frame);
	}

	m_frame->pts = time;

	AVPixelFormat pf;
	switch (fmt)
	{
	case VideoFormat::ARGB:pf = AVPixelFormat::AV_PIX_FMT_BGRA; break;
	case VideoFormat::RGBA:pf = AVPixelFormat::AV_PIX_FMT_RGBA; break;
	case VideoFormat::RGB:pf = AVPixelFormat::AV_PIX_FMT_0RGB; break;
	case VideoFormat::YUV420:pf = AVPixelFormat::AV_PIX_FMT_YUV420P; break;
	case VideoFormat::YUV422:pf = AVPixelFormat::AV_PIX_FMT_YUV422P; break;
	}
	auto c = sws_getContext(
		width, height, pf,
		m_enc->width, m_enc->height, m_enc->pix_fmt,
		SWS_POINT, nullptr, nullptr, nullptr);

	uint8_t *ddat[] = { data,0,0,0,0,0,0,0 };
	int dp[] = { stride,0,0,0,0,0,0,0 };

	int hh = sws_scale(c, ddat, dp, 0, m_frame->height, m_frame->data, m_frame->linesize);// change pixelformat

	sws_freeContext(c);

	pushframe(m_frame);
}
void movierecorder::outputstream::pushframe(AVFrame *frame)
{

	int ret = avcodec_send_frame(m_enc, frame);
	if (ret < 0) {
		//fprintf(stderr, "Error sending a frame for encoding\n");
		//exit(1);
		return;
	}
	AVPacket pkt = { 0 };
	av_init_packet(&pkt);

	if (frame == nullptr)
	{
		pkt.data = NULL;
		pkt.size = 0;
		pkt.stream_index = m_st->stream_identifier;
	}
	while (ret >= 0) {
		ret = avcodec_receive_packet(m_enc, &pkt);
		if (ret == AVERROR(EAGAIN))
			return;
		else if (ret == AVERROR_EOF)
			return;
		else if (ret < 0) {
			//	fprintf(stderr, "Error during encoding\n");
			//	exit(1);
			return;
		}
		ret = write_frame(m_owner->m_oc, &m_enc->time_base, m_st, &pkt);

		av_packet_unref(&pkt);
	}
}

void movierecorder::outputstream::open_audio()
{
	AVCodecContext *c;
	int ret;
	c = m_enc;
	/* open it */

	m_next_pts = 0;
	m_convert = new float[m_rate * m_channels];
	m_writepos = 0;

	AVDictionary *opt = NULL;
	av_dict_copy(&opt, m_owner->m_opt, 0);

	ret = avcodec_open2(c, m_codec, &opt);
	av_dict_free(&opt);
	if (ret < 0) {
		throw new _err("Could not open audio codec");
		//fprintf(stderr, "Could not open audio codec: %s\n", av_err2str(ret));
		//exit(1);
	}
	if (c->codec->capabilities & AV_CODEC_CAP_VARIABLE_FRAME_SIZE)
		nb_samples = c->sample_rate;
	else
		nb_samples = c->frame_size;
	m_frame = alloc_audio_frame(c->sample_fmt, c->channel_layout, c->sample_rate, nb_samples);
	//		m_tmp_frame = alloc_audio_frame(AV_SAMPLE_FMT_S16, c->channel_layout, c->sample_rate, nb_samples);

	int64_t channelmask = m_channelslayout;

	m_converter1.reset(new AudioConverter(AV_SAMPLE_FMT_FLT, channelmask, m_rate, AV_SAMPLE_FMT_FLT, channelmask, c->sample_rate));
	m_converter2.reset(new AudioConverter(AV_SAMPLE_FMT_FLT, channelmask, c->sample_rate, c->sample_fmt, c->channel_layout, c->sample_rate));

	/* copy the stream parameters to the muxer */
	ret = avcodec_parameters_from_context(m_st->codecpar, c);
	if (ret < 0) {
		fprintf(stderr, "Could not copy the stream parameters\n");
		exit(1);
	}
}
void movierecorder::outputstream::open_video()
{
	int ret;
	AVCodecContext *c = m_enc;
	AVDictionary *opt = NULL;
	av_dict_copy(&opt, m_owner->m_opt, 0);
	/* open the codec */
	ret = avcodec_open2(c, m_codec, &opt);
	av_dict_free(&opt);
	if (ret < 0) {
		throw new _err("Could not open video codec:");// , GetString(av_err2str(ret))));
	}
	/* allocate and init a re-usable frame */
	m_frame = alloc_picture(c->pix_fmt, c->width, c->height);
	if (!m_frame) {
		throw new _err("Could not allocate video frame");
	}
	/* copy the stream parameters to the muxer */
	ret = avcodec_parameters_from_context(m_st->codecpar, c);
	if (ret < 0) {
		throw new _err("Could not copy the stream parameters");
	}
}
