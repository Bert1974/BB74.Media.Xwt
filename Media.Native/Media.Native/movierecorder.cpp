#include "stdafx.h"
#include "movierecorder.h"

#include <assert.h>

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
void movierecorder::Clear()
{
	for (auto& p : m_audio) {
		p->Clear();
	}
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
