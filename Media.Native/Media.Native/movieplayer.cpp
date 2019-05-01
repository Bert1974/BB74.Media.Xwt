	#include "stdafx.h"
#include "movieplayer.h"

movieplayer::movieplayer(const char *filename)
	: m_avicontext(nullptr)
	, m_quiting(false), m_running(false), m_stopping(false),m_quitted(false)
	, m_lockhtreadid(0)
	, m_eosfunction(0), m_flushedfunction(0)
{
	_mainloop.reset(new std::thread(&movieplayer::_mainthread, this));
	SETTHREADNAME(_mainloop.get(), "movie main");
	try
	{
		FFMPEGLOCK();

		int err = avformat_open_input(&m_avicontext, filename, nullptr/*format*/, nullptr/*options*/);
		if (err < 0) {
			throw new _err("error {%s} for av_open_input_file", ERRSTR(err).c_str());
			return;
		}
		err = avformat_find_stream_info(m_avicontext, NULL);

		/*	AVCodec * viddecoder = 0;
			//	AVCodec * auddecoder = 0;

			{
				m_vidstreamnumber = av_find_best_stream(tmp, AVMEDIA_TYPE_VIDEO, -1, -1, &viddecoder, 0);

				//		msclr::lock l(%BaseStream::m_avcodec_lock);
				// Retrieve stream information
				if (m_vidstreamnumber < 0) {
					throw gcnew IOException(String::Format("{0} no video stream found", System::IO::Path::GetFileName(m_filename)));
				}
				//	m_audstreamnumber = av_find_best_stream(tmp, AVMEDIA_TYPE_AUDIO, -1, -1, &auddecoder, 0);
			}
			//avicontext->streams[nit]->codec->codec=avcodec_find_decoder((CodecID)22);
			m_video = gcnew VideoStream(this, m_vidstreamnumber, viddecoder);

			m_audstreamnumber = -2;
			int totaud = 0;
			for (UINT nit = 0; nit < m_avicontext->nb_streams; nit++)
			{
				if (m_avicontext->streams[nit]->codecpar->codec_type == AVMEDIA_TYPE_AUDIO)
				{
					if (m_audstreamnumber == -2)
					{
						m_audstreamnumber = nit;
						totaud++;

						if (m_avicontext->streams[m_audstreamnumber]->codecpar->channels != 1)
						{
							break;
						}
					}
					else if (m_avicontext->streams[nit]->codecpar->channels == 1)
					{
						totaud++;
						m_audstreamnumber = -1;
						break;
					}
				}
			}
			if (allocstreamfunction)
			{
				if (m_audstreamnumber >= 0)
				{
					try
					{
						AVCodec * auddecoder = 0;

						m_audstreamnumber = av_find_best_stream(m_avicontext, AVMEDIA_TYPE_AUDIO, m_audstreamnumber, -1, &auddecoder, 0);

						m_audio = gcnew AudioStream(this, m_audstreamnumber, auddecoder, allocstreamfunction, downconvertaudio);
					}
					catch (Exception^ e)
					{
					}
				}
				else if (m_audstreamnumber == -1)
				{
					try
					{
						m_audio = gcnew MultiAudioStream(this, allocstreamfunction);
					}
					catch (Exception^ e)
					{
					}
				}
			}
			*/
			/*	m_duration = 0;

				m_duration = max(m_duration, m_video->m_duration);
				if (m_audio != nullptr)
				{
					m_duration = max(m_duration, m_audio->get_duration());
				}
				if (m_duration == 0 && m_avicontext->nb_chapters > 0)
				{
					auto ch = m_avicontext->chapters[m_avicontext->nb_chapters - 1];
					m_duration = (ch->end*ffmpeg->TimeBase*ch->time_base.num / ch->time_base.den);
				}*/
		FFMPEGUNLOCK();
	}
	catch (_err*)
	{
		if (m_avicontext) {
			avformat_close_input(&m_avicontext);
			m_avicontext = 0;
		}
		FFMPEGUNLOCK();

		{
			std::unique_lock<std::mutex> lk(_mainmutex);
			m_quiting = true;
			_maincond.notify_all();

			while (!m_quitted)
			{
				_quittedcond.wait(lk);
			}
		}
		if (m_avicontext) {
			avformat_close_input(&m_avicontext);
		}
		throw;
	}
}


movieplayer::~movieplayer()
{
	{
		std::unique_lock<std::mutex> lk(_mainmutex);

		for (auto& it : m_audiostreams) {
			it->stop();
		}
		for (auto& it : m_videostreams) {
			it->stop();
		}

		m_quiting = true;
		_maincond.notify_all();

		while (!m_quitted)
		{
			_quittedcond.wait(lk);
		}
		_mainloop->join();
	}
	if (m_avicontext) {
		avformat_close_input(&m_avicontext);
	}
}
void movieplayer::_mainthread()
{
	while (true)
	{
		std::unique_lock<std::mutex> lk(_mainmutex);

		if (!m_running)
		{
			_maincond.wait(lk);
		}
		if (m_quiting)
		{
			m_quitted = true;
			_quittedcond.notify_all();
			lk.unlock();
			return;
		}
		if (m_running)
		{
			lk.unlock();

			do
			{
				lk.lock();
				if (m_quiting || !m_running)
				{
					lk.unlock();
					break;
				}
				lk.unlock();

				AVPacket *packet = av_packet_alloc();
				av_init_packet(packet);

				try
				{
					auto r = av_read_frame(m_avicontext, packet);

					if ((r))
					{
						if (r == AVERROR(EAGAIN)) {
							//	av_usleep(10000);
							//	ret = 0;
							av_packet_free(&packet);
							continue;
						}
						else if (r < 0)
						{
							if (r == AVERROR(ENOMEM))
							{
							}
							else if (r == AVERROR_EOF)
							{
							//	lk.lock();

								for (auto it = m_audiostreams.begin(); it != m_audiostreams.end(); ++it) {
									(*it)->newpacket(0);
								}
								for (auto it = m_videostreams.begin(); it != m_videostreams.end(); ++it) {
									(*it)->newpacket(0);
								}
								for (auto it = m_audiostreams.begin(); it != m_audiostreams.end(); ++it) {
									(*it)->waitend();
								}
								for (auto it = m_videostreams.begin(); it != m_videostreams.end(); ++it) {
									(*it)->waitend();
								}
								lk.lock();

								m_running = false;

							/*	basestream *stream = 0;

								if (m_videostreams.begin() != m_videostreams.end())
								{
									stream = (m_videostreams.begin()->get());
								}
								else if (m_audiostreams.begin() != m_audiostreams.end())
								{
									stream = (m_audiostreams.begin()->get());
								}

								uint64_t streampos=0;

								int err = avformat_seek_file(m_avicontext, stream->m_streamid, 0, streampos, streampos, 0);

								for (auto it = m_audiostreams.begin(); it != m_audiostreams.end(); ++it) {
									(*it)->flush();
								}
								for (auto it = m_videostreams.begin(); it != m_videostreams.end(); ++it) {
									(*it)->flush();
								}
								if (m_flushedfunction) {
									m_flushedfunction();
								}*/

								this->m_lockhtreadid = CURRENTTHREADID(); // only used here.. should be in trylock

								if (m_eosfunction) {
									m_eosfunction();
								}
								this->m_lockhtreadid = 0;
								lk.unlock();
							}
						}
					}
					else
					{
						if (packet->data != nullptr)
						{
						//	lk.lock();
							//	for (auto it = m_audiostreams.begin(); it != m_audiostreams.end(); ++it)
							for (auto& it : m_audiostreams) {
								if (it->m_streamid == packet->stream_index)
								{
									it->newpacket(&packet);
									av_packet_free(&packet);
									break;
								}
							}
							for (auto it = m_videostreams.begin(); packet && it != m_videostreams.end(); ++it) {
								if ((*it)->m_streamid == packet->stream_index)
								{
									(*it)->newpacket(&packet);
									av_packet_free(&packet);
									break;
								}
							}
						//	lk.unlock();
						}
					}
				}
				catch (_err *e)
				{
					delete e;
				}
				av_packet_free(&packet);

			} while (true);

		}
		else
		{
			lk.unlock();
		}
	}
}
void movieplayer::run(bool startthreads)
{
	{
		std::unique_ptr< std::lock_guard<std::mutex>> lk;
		trylock(lk);

		if (startthreads)
		{
			for (auto& it : m_audiostreams) {
				it->run();
			}
			for (auto& it : m_videostreams) {
				it->run();
			}
		}
		m_running = true;
	_maincond.notify_all();
	}
}

void movieplayer::preparestop()
{
	{
		std::unique_ptr< std::lock_guard<std::mutex>> lk;
		trylock(lk);

		m_running = false;

		for (auto& it : m_audiostreams) {
			it->preparestop();
		}
		for (auto& it : m_videostreams) {
			it->preparestop();
		}
	_maincond.notify_all();
	}

}
void movieplayer::stop()
{
	{
		std::unique_ptr< std::lock_guard<std::mutex>> lk;
		trylock(lk);

		m_running = false;
		//m_quiting = true;
	_maincond.notify_all();
	}

	for (auto& it : m_audiostreams) {
		it->stop();
	}
	for (auto& it : m_videostreams) {
		it->stop();
	}
}

