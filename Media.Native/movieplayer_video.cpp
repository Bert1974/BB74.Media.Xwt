#include "stdafx.h"
#include "movieplayer.h"

videoframe::videoframe(FrameAllocateFunction allocfunc, FrameLockFunction lockfunc, FrameUnLockFunction unlockfunc) : m_data(0), m_allocated(false), m_pitch(-1), m_allocfunc(allocfunc), m_lockfunc(lockfunc), m_unlockfunc(unlockfunc)
{
}

videoframe::~videoframe()
{
	if (m_allocated)
	{
		delete[] m_data;
		m_data = 0;
	}
}

void videoframe::alloc(videostream *stream)
{
	if (m_allocfunc)
	{
		VideoFormat fmt;
		switch (stream->m_codec->pix_fmt)
		{
		case AV_PIX_FMT_YUV420P: //fmt = VideoFormat::YUV420; break;
		case AV_PIX_FMT_YUYV422:
		case AV_PIX_FMT_YUV422P: fmt = VideoFormat::YUV422; break;
		case AV_PIX_FMT_ARGB: fmt = VideoFormat::ARGB; break;
		case AV_PIX_FMT_RGBA: fmt = VideoFormat::RGBA; break;
		default:fmt = VideoFormat::RGB; break;
		}
		m_allocfunc(stream, stream->m_avFrame->pts, 400000, stream->m_codec->width, stream->m_codec->height, fmt, &m_data, &m_pitch, &_fmt);
	}
	else if (!m_data)
	{
		m_pitch = stream->m_codec->width * 4;
		m_data = new uint8_t[m_pitch* stream->m_codec->height];
		m_allocated = true;
	}
}
void videoframe::lock(videostream *stream)
{
	if (m_lockfunc && !m_allocated)
	{
		m_lockfunc(stream, &m_data, &m_pitch);
	}
}
void videoframe::unlock(videostream *stream)
{
	if (m_unlockfunc && !m_allocated)
	{
		m_unlockfunc();
	}
}



videostream::videostream(movieplayer *owner, int streamnumber, AVCodec *codec, FrameReadyFunction readyfunc)
	: basestream(owner, streamnumber, codec), _frameready(readyfunc), m_swsContext(0)
{
	m_avFrame = av_frame_alloc();
}
videostream::~videostream()
{
	if (m_swsContext)
	{
		sws_freeContext(m_swsContext);
		m_swsContext = 0;
	}
}

void videostream::FrameReady()
{
	if (_frameready(m_avFrame, m_avFrame->pts, m_avFrame->pkt_duration))
	{
		m_avFrame = av_frame_alloc();
	}
}

void videostream::EOS()
{

}
void videostream::Flushed()
{

}

videoframe *videostream::allocframe(FrameAllocateFunction allocfunc, FrameLockFunction lockfunc, FrameUnLockFunction unlockfunc)
{
	return new videoframe(allocfunc, lockfunc, unlockfunc);
}

void videostream::fillframe(videoframe *frame, AVFrame *src, int64_t *time)
{
	frame->alloc(this);

	frame->lock(this);

	AVPixelFormat fmt;

	switch (frame->_fmt)
	{
	case VideoFormat::RGB:	fmt = AVPixelFormat::AV_PIX_FMT_BGRA; break;
	case VideoFormat::ARGB: fmt = AVPixelFormat::AV_PIX_FMT_ARGB; break;
	case VideoFormat::RGBA: fmt = AVPixelFormat::AV_PIX_FMT_RGBA; break;
	case VideoFormat::YUV422:fmt=AVPixelFormat::AV_PIX_FMT_YUYV422; break;
	default:
	case VideoFormat::YUV420:throw new _err("unsupported pixelformat");
	}
	if (!m_swsContext)
	{
		m_swsContext = sws_getContext(
			m_codec->width, m_codec->height, m_codec->pix_fmt,
			m_codec->width, m_codec->height, fmt,
			SWS_POINT, nullptr, nullptr, nullptr);
	}
	uint8_t *ddat[] = { (uint8_t*)frame->m_data,0,0,0,0,0,0,0 };
	int dp[] = { frame->m_pitch,0,0,0,0,0,0,0 };

	int hh = sws_scale(m_swsContext, src->data, src->linesize, 0, m_codec->height, ddat, dp);// change pixelformat

	*time = src->pts;

	frame->unlock(this);
}

uint64_t videostream::Time(uint64_t time)
{
	return (uint64_t)((10000000.0* time) / this->m_codec->time_base.den);
}