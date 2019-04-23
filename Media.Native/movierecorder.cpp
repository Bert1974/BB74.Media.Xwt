#include "stdafx.h"
#include "movierecorder.h"

#include <assert.h>

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

void movierecorder::getopts(const char *options)
{
	std::string s = options;
	std::string delimiter = ">=";

	size_t pos = 0;
	AVDictionary *opt = 0;

	while ((pos = s.find(delimiter)) != std::string::npos) {
		std::string n = s.substr(0, pos);
		s.erase(0, pos + delimiter.length());
		pos = s.find(delimiter);
		if (pos != std::string::npos)
		{
			std::string v = s.substr(0, pos);
			s.erase(0, pos + delimiter.length());

			if (n.compare("-flags") != 0 && n.compare("-fflags") != 0)
			{
				av_dict_set(&opt, n.substr(1).c_str(), v.c_str(), 0);
			}
		}
	}
	m_opt = opt;
}
movierecorder::movierecorder(movierecorder::PushPacketFunction func, const char *options)
	: m_opt(0), m_oc(0), m_writer(0), m_buffer(0), m_writefunc(func)
{
	getopts(options);

	/* allocate the output media context */
//	avformat_alloc_output_context2(&m_oc, NULL, NULL, NULL);
	if (!m_oc) {
		//	printf("Could not deduce output format from file extension: using MPEG.\n");
		avformat_alloc_output_context2(&m_oc, NULL, "mp4", NULL);
	}
	if (!m_oc) {
		throw new _err("unknown file type");
	}
}
movierecorder::movierecorder(const char *filename, const char *options)
	: m_opt(0), m_oc(0), m_writer(0), m_buffer(0), m_writefunc(0), m_fname(filename)
{
	getopts(options);

	/* allocate the output media context */
	avformat_alloc_output_context2(&m_oc, NULL, NULL, filename);
	if (!m_oc) {
		//	printf("Could not deduce output format from file extension: using MPEG.\n");
		avformat_alloc_output_context2(&m_oc, NULL, "mp4", filename);
	}
	if (!m_oc) {
		throw new _err("unknown file type");
	}
/*	AVOutputFormat *fmt = m_oc->oformat;
	if (fmt->video_codec != AV_CODEC_ID_NONE) {
		have_video = 1;
	}
	if (fmt->audio_codec != AV_CODEC_ID_NONE) {
		have_audio = 1;
	}*/

#if (false)
	m_vid2->bit_rate = 400000;
	/* emit one intra frame every ten frames
	* check frame pict_type before passing frame
	* to encoder, if frame->pict_type is AV_PICTURE_TYPE_I
	* then gop_size is ignored and the output of encoder
	* will always be I frame irrespective to gop_size
	*/
	m_vid2->gop_size = 10;
	m_vid2->max_b_frames = 1;
	m_vid2->pix_fmt = AV_PIX_FMT_YUV420P;
	if (m_vid->id == AV_CODEC_ID_H264)
		av_opt_set(m_vid2->priv_data, "preset", "slow", 0);
#endif
}
movierecorder::~movierecorder()
{
	if (m_oc)
	{
		m_video.clear();
		m_audio.clear();

		AVOutputFormat* fmt = m_oc->oformat;
		if (!(fmt->flags & AVFMT_NOFILE))
			/* Close the output file. */
			avio_closep(&m_oc->pb);
		/* free the stream */
		avformat_free_context(m_oc);
		m_oc = nullptr;
	}
}
movierecorder::outputstream * movierecorder::AddAudio(int bitrate, int samplerate, ChannelsLayout fmtreq, AudioFormat audiofmt)
{
		outputstream *str = 0;

	if (m_oc)
	{
		AVOutputFormat *fmt = m_oc->oformat;

		add_stream(&str, m_oc, fmt->audio_codec);

		AVCodecContext *c = str->m_enc;

		c->sample_fmt = str->m_codec->sample_fmts ? str->m_codec->sample_fmts[0] : AV_SAMPLE_FMT_FLT;
		c->bit_rate = bitrate;
		c->sample_rate = samplerate;

		if (str->m_codec->supported_samplerates) {
			c->sample_rate = str->m_codec->supported_samplerates[0];
			for (int i = 0; str->m_codec->supported_samplerates[i]; i++) {
				if (str->m_codec->supported_samplerates[i] == samplerate)
					c->sample_rate = samplerate;
			}
		}
		int64_t reqlayout = fmtreq;
		c->channels = av_get_channel_layout_nb_channels(reqlayout);
		c->channel_layout = reqlayout;
		if (str->m_codec->channel_layouts) {
			c->channel_layout = str->m_codec->channel_layouts[0];
			for (int i = 0; str->m_codec->channel_layouts[i]; i++) {
				if (str->m_codec->channel_layouts[i] == reqlayout)
					c->channel_layout = reqlayout;
			}
		}
		c->channels = av_get_channel_layout_nb_channels(c->channel_layout);
		str->m_st->time_base.num = 1;
		str->m_st->time_base.den = c->sample_rate;

		str->m_channels = c->channels;
		str->m_rate = samplerate;
		str->m_channelslayout = fmtreq;

		str->open_audio();

		m_audio.push_back(std::unique_ptr<outputstream>(str));
	}
	return str;
}
movierecorder::outputstream *movierecorder::AddVideo(int width, int height, FPS *fps, VideoFormat pixelformat)
{
		outputstream *str = 0;

	if (m_oc)
	{
		AVOutputFormat *fmt = m_oc->oformat;

		add_stream(&str, m_oc, fmt->video_codec);

		AVCodecContext *c = str->m_enc;
		c->codec_id = fmt->video_codec;
		//	c->bit_rate = 6000000;
			/* Resolution must be a multiple of two. */
		c->width = width;
		c->height = height;
		/* timebase: This is the fundamental unit of time (in seconds) in terms
		* of which frame timestamps are represented. For fixed-fps content,
		* timebase should be 1/framerate and timestamp increments should b
		* identical to 1. */

		c->framerate.num = fps->Number.den;
		c->framerate.den = fps->Number.num;

		str->m_st->time_base.den = fps->Number.den;
		str->m_st->time_base.num = fps->Number.num;
		c->ticks_per_frame = fps->Interlaced ? 2 : 1;

		if (m_video.empty())
		{
			c->time_base = str->m_st->time_base;
		}
		c->gop_size = 12; /* emit one intra frame every twelve frames at most */
		switch (pixelformat)
		{
		case VideoFormat::ARGB:c->pix_fmt = AVPixelFormat::AV_PIX_FMT_ARGB; break;
		case VideoFormat::RGBA:c->pix_fmt = AVPixelFormat::AV_PIX_FMT_RGBA; break;
		case VideoFormat::RGB:c->pix_fmt = AVPixelFormat::AV_PIX_FMT_0RGB; break;
		case VideoFormat::YUV420:c->pix_fmt = AVPixelFormat::AV_PIX_FMT_YUV420P; break;
		case VideoFormat::YUV422:c->pix_fmt = AVPixelFormat::AV_PIX_FMT_YUV422P; break;
		}

		if (c->codec_id == AV_CODEC_ID_MPEG2VIDEO) {
			/* just for testing, we also add B-frames */
			c->max_b_frames = 2;
		}
		if (c->codec_id == AV_CODEC_ID_MPEG1VIDEO) {
			/* Needed to avoid using macroblocks in which some coeffs overflow.
			* This does not happen with normal video, it just happens here as
			* the motion of the chroma plane does not match the luma plane. */
			c->mb_decision = 2;
		}
		str->open_video();
	//	c->time_base = str->m_st->time_base;

		m_video.push_back(std::unique_ptr<outputstream>(str));
	}
	return str;
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
void movierecorder::Clear()
{
	for (auto& p : m_audio) {
		p->Clear();
	}
}
void movierecorder::outputstream::Clear()
{
	m_writepos = 0;
	m_converter1.reset(new AudioConverter(AV_SAMPLE_FMT_FLT, m_channelslayout, m_rate, AV_SAMPLE_FMT_FLT, m_channelslayout, m_enc->sample_rate));
	m_converter2.reset(new AudioConverter(AV_SAMPLE_FMT_FLT, m_channelslayout, m_enc->sample_rate, m_enc->sample_fmt, m_enc->channel_layout, m_enc->sample_rate));
}
/*void movierecorder::Push(IVideoFrame^ frame, Int64 time)
{
	if (m_video == nullptr) {
		return;
	}
	m_video->Push(frame, time);
}
void MovieRecorder::Push(array<Byte>^ audiodata, int totsamples)
{
	if (m_audio == nullptr) {
		return;
	}
	m_audio->Push(audiodata, totsamples);
}*/
void movierecorder::outputstream::Push(int64_t time, uint8_t *audiodata, int totsamples, AudioFormat fmt)
{
	assert(fmt == AudioFormat::Float32);

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

void movierecorder::outputstream::Push(uint8_t* data, int stride, int width,int height, VideoFormat fmt, int64_t time)
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
/*	void Stop()
	{
		if (fileout)
		{
			encode(nullptr);

			//	array<unsigned char>^ endcode = { 0, 0, 1, 0xb7 };
			//	fileout->Write(endcode, 0, endcode->Length);

			fileout->Close();
			fileout = nullptr;
			AVCodecContext *p1 = m_vid2;
			avcodec_free_context(&p1);
			m_vid2 = 0;
			AVFrame *p2 = m_frame;
			av_frame_free(&p2);
			m_frame = 0;
			AVPacket *p3 = m_pkt;
			av_packet_free(&p3);
			m_pkt = 0;
		}
	}
};*/

/* Add an output stream. */
void movierecorder::add_stream(outputstream **ost, AVFormatContext *oc, enum AVCodecID codec_id)
{
	if (*ost != nullptr)
	{
		throw new _err("stream already added");
	}
	*ost = new outputstream(this);

	/* find the encoder */
	(*ost)->m_codec = avcodec_find_encoder(codec_id);

	if (!((*ost)->m_codec)) {
		throw new _err("Could not find encoder for %s",avcodec_get_name(codec_id));
	}
	(*ost)->m_st = avformat_new_stream(oc, NULL);
	if (!(*ost)->m_st) {
		throw new _err("Could not allocate stream");
	}
	(*ost)->m_st->id = oc->nb_streams - 1;

	AVCodecContext *c = avcodec_alloc_context3((*ost)->m_codec);
	if (!c) {
		throw new _err("Could not alloc an encoding context\n");
	}
	(*ost)->m_enc = c;
	switch ((*ost)->m_codec->type) {
		/*	case AVMEDIA_TYPE_AUDIO:
		c->sample_fmt = (*codec)->sample_fmts ?
		(*codec)->sample_fmts[0] : AV_SAMPLE_FMT_FLTP;
		c->bit_rate = 64000;
		c->sample_rate = 44100;
		if ((*codec)->supported_samplerates) {
		c->sample_rate = (*codec)->supported_samplerates[0];
		for (i = 0; (*codec)->supported_samplerates[i]; i++) {
		if ((*codec)->supported_samplerates[i] == 44100)
		c->sample_rate = 44100;
		}
		}
		c->channels = av_get_channel_layout_nb_channels(c->channel_layout);
		c->channel_layout = AV_CH_LAYOUT_STEREO;
		if ((*codec)->channel_layouts) {
		c->channel_layout = (*codec)->channel_layouts[0];
		for (i = 0; (*codec)->channel_layouts[i]; i++) {
		if ((*codec)->channel_layouts[i] == AV_CH_LAYOUT_STEREO)
		c->channel_layout = AV_CH_LAYOUT_STEREO;
		}
		}
		c->channels = av_get_channel_layout_nb_channels(c->channel_layout);
		ost->st->time_base = (AVRational) { 1, c->sample_rate };
		break;*/
	case AVMEDIA_TYPE_VIDEO:
		break;
	default:
		break;
	}
	/* Some formats want stream headers to be separate. */
	if (oc->oformat->flags & AVFMT_GLOBALHEADER)
		c->flags |= AV_CODEC_FLAG_GLOBAL_HEADER;
}


int movierecorder::write_packet(void *opaque, uint8_t *buf, int bufsize)
{
	return ((movierecorder*)opaque)->m_writefunc(buf, bufsize);
}
void movierecorder::Start()
{
	if (!m_video.empty() || !m_audio.empty())
	{
		AVDictionary *opt = 0;
		int ret;

		AVOutputFormat* fmt = m_oc->oformat;

	//	av_dump_format(m_oc, 0, m_fname, 1);
		/* open the output file, if needed */
		if (!(fmt->flags & AVFMT_NOFILE)) {
			ret = avio_open(&m_oc->pb, m_fname.c_str(), AVIO_FLAG_WRITE);

			if (ret < 0) {
				throw _err("IOException");
			}
			/*	if (ret < 0) {
					fprintf(stderr, "Could not open '%s': %s\n", filename,
						av_err2str(ret));
					return 1;
				}*/
		}

		if (m_writefunc)
		{
			int buflen = 1024 * 1024 * 2;
			if (!m_buffer)
			{
				m_buffer = new uint8_t[buflen];
			}

			// Allocate the AVIOContext:
			// The fourth parameter (pStream) is a user parameter which will be passed to our callback functions
			m_writer = avio_alloc_context(m_buffer, buflen,  // internal Buffer and its size
				1,                  // bWriteable (1=true,0=false) 
				this,          // user data ; will be passed to our callback functions
				0,
				movierecorder::write_packet,                  // Write callback function (not used in this example) 
				0);

			m_oc->pb = m_writer;
			m_oc->flags = AVFMT_FLAG_CUSTOM_IO | AVFMT_FLAG_NOBUFFER | AVFMT_FLAG_FLUSH_PACKETS;
		}
		/* Write the stream header, if any. */
		ret = avformat_write_header(m_oc, &opt);
		if (ret < 0) {
			throw new _err("Error occurred when opening output file");
		}
	}
	else
	{
		throw new _err("No streams added");
	}
}
void movierecorder::Stop()
{
	if (m_oc)
	{
		for (auto& a : m_audio)
		{
			a->pushframe(0);
		}
		for (auto& v : m_video)
		{
			v->pushframe(0);
		}
		/*	while (encode_video || encode_audio) {
				if (encode_video &&
					(!encode_audio || av_compare_ts(video_st.next_pts, video_st.enc->time_base,
						audio_st.next_pts, audio_st.enc->time_base) <= 0)) {
					encode_video = !write_video_frame(oc, &video_st);
				}
				else {
					encode_audio = !write_audio_frame(oc, &audio_st);
				}
			}*/
			/* Write the trailer, if any. The trailer must be written before you
			* close the CodecContexts open when you wrote the header; otherwise
			* av_write_trailer() may try to use memory that was freed on
			* av_codec_close(). */
		av_write_trailer(m_oc);
		/* Close each codec. */

		if (m_writer)
		{
			av_free(m_writer);
		}
		m_video.clear();
		m_audio.clear();

		AVOutputFormat* fmt = m_oc->oformat;
		if (!(fmt->flags & AVFMT_NOFILE))
			/* Close the output file. */
			avio_closep(&m_oc->pb);
		/* free the stream */
		avformat_free_context(m_oc);
		m_oc = nullptr;

		if (m_buffer)
		{
			delete[] m_buffer;
			m_buffer = 0;
		}
	}
}
