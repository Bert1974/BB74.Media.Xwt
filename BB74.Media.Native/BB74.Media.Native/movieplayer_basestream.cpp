#include "stdafx.h"
#include "movieplayer.h"
#include <assert.h>

#define max_packets	150

basestream::basestream(movieplayer *owner, int streamnumber, AVCodec *codec)
	: m_owner(owner), m_streamid(streamnumber), _codec(codec), m_last_pts(0)
	, m_avFrame(0)
	, m_stream(NULL), m_codec(NULL)
	, m_full(false), m_empty(true), m_quit(false), m_quited(false)
{
	m_stream = m_owner->m_avicontext->streams[m_streamid];

	m_codec = avcodec_alloc_context3(_codec);
	avcodec_parameters_to_context(m_codec, m_stream->codecpar);

	m_codec->time_base = m_stream->time_base;
	//av_codec_set_pkt_timebase(m_codec, m_stream->time_base);
//#pragma warning (suppress:4996)
			//	m_codec = m_owner->m_avicontext->streams[m_streamid]->codec;

	int ret;
	if ((ret = avcodec_open2(m_codec, _codec, nullptr)) != 0)
	{
		throw new _err("Can'initialize find decoder");
	}
	/*if (m_stream->duration == 0x8000000000000000)
	{
		m_duration = m_owner->m_duration;
	}
	else
	{
		m_duration = (uint64_t)(m_stream->duration * m_owner->m_ffmpeg->TimeBase * av_q2d(m_stream->time_base));
	}*/

}
basestream::~basestream()
{
	if (m_thread.get())
	//if (!m_quited)
	{
		{
			std::unique_lock<std::mutex> lk(m_mutex);
			m_quit = true;
			m_waitcondition.notify_all();

			while (!m_quited)
			{
				m_quitedcondition.wait(lk);
			}
			m_thread->join();
			m_thread.reset(0);
		}
	}
	av_frame_free(&m_avFrame);
	avcodec_free_context(&m_codec);
}


void basestream::waitend()
{
	std::unique_lock<std::mutex> lk(m_mutex);

	while (!m_empty)
	{
		if (m_quit)
		{
			return;
		}
		m_waitcondition.wait(lk);
	}
}
void basestream::flush()
{
	avcodec_flush_buffers(m_codec);

	while (m_packets.size() > 0)
	{
		AVPacket *packet = m_packets.back();
		if (packet)
		{
			av_packet_free(&packet);
		}
		m_packets.pop_back();
	}
	m_full = false;
	m_empty = true;
	Flushed();
}
void basestream::flushnow()
{
	std::unique_lock<std::mutex> lk(m_mutex);

	avcodec_flush_buffers(m_codec);

	while (m_packets.size() > 0)
	{
		AVPacket *packet = m_packets.back();
		if (packet)
		{
			av_packet_free(&packet);
		}
		m_packets.pop_back();
	}
	m_full = false;
	m_empty = true;
	Flushed();
}
void basestream::preparestop()
{
	//assert(m_thread.get() != 0);
	{
		std::lock_guard<std::mutex> lk(m_mutex);
		m_quit = true;
		m_waitcondition.notify_all();
	}

}
void basestream::run()
{
	assert(m_packets.size() == 0);
	assert(m_thread.get() == 0);
	m_full = false;
	m_empty = true;
	m_quit = false;
	m_quited = false;
	m_thread.reset(new std::thread(&basestream::_run, this));
	SETTHREADNAME(m_thread.get(), "basestream main");
//m_waitcondition.notify_all();

}
void basestream::stop()
{
	if (m_thread.get())
	{
		std::unique_lock<std::mutex> lk(m_mutex);
		m_quit = true;

		m_waitcondition.notify_all();

		while (!m_quited)
		{
			m_quitedcondition.wait(lk);
		}
		m_thread->join();
		m_thread.reset(0);
	}
}
void basestream::newpacket(AVPacket **packet)
{
	std::unique_lock<std::mutex> lk(m_mutex);

	while (true)
	{
		if (m_quit)
		{
			return;
		}
		if (m_full)
		{
			m_waitcondition.wait(lk);
		}
		else
		{
			if (packet)
			{
				m_packets.push_back(*packet);
				*packet = 0;
			}
			else
			{
				m_packets.push_back(0);
			}
			if (m_packets.size() > max_packets)
			{
				m_full = true;
			}
			m_empty = false;
			m_waitcondition.notify_all();
		/*	fprintf(stderr,
				"add packet: %ld\n",
				m_packets.size());*/
			return;
		}
	}
}

void basestream::_run()
{
	std::unique_lock<std::mutex> lk(m_mutex);

	while (!m_quit)
	{
		if (m_empty)
		{
			m_waitcondition.wait(lk);
		}
		else
		{
			AVPacket *packet = m_packets.begin().operator*();
			m_packets.erase(m_packets.begin());
			m_full = false;
			m_waitcondition.notify_all();

			/*fprintf(stderr,
				"got packet: %ld\n",
				m_packets.size());*/
			lk.unlock();
			if (packet)
			{
				decodenextpacket(packet);
				av_packet_free(&packet);
			}
			else
			{
				decodenextpacket(0);
			}
			lk.lock();
			if (m_packets.size() == 0)
			{
				m_empty = true;
				m_waitcondition.notify_all();
			}
		}
	}
	m_quited = true;
	//m_thread.reset(0);
	m_quitedcondition.notify_all();
}

void basestream::decodenextpacket(AVPacket *packet)
{
	bool  newframe = false;
	bool doflush = false;

	AVPacket pkt;
	if (packet == 0)
	{
		doflush = true;

		av_init_packet(&pkt);
		pkt.data = NULL;
		pkt.size = 0;
		pkt.stream_index = m_streamid;

		packet = &pkt;
	}
	// decode video frame
	int frameFinished = false;

	int n = avcodec_send_packet(m_codec, packet);

	if (n < 0)
	{
		return;
	}
	while (n >= 0)
	{
		n = avcodec_receive_frame(m_codec, m_avFrame);

		if (n == AVERROR_EOF)
		{
			//				EOS();
			return;
		}
		else if (n == AVERROR(EAGAIN))
		{
			return;
		}
		else if (n == AVERROR(ENOMEM))
		{
			return;
		}
		else if (n != 0)
		{
			return;
		}
		FrameReady();
	}
}