#include "stdafx.h"
#include "movieplayer.h"

audiostream::audiostream(movieplayer *owner, int streamnumber, AVCodec *codec,/* AudioFormat fmt,*/ int samplerate, AudioFormat fmt, ChannelsLayout channels, AudioFrameReadyFunction readyfunc)
	: basestream(owner, streamnumber, codec), _frameready(readyfunc), m_bufferlen(0), m_format(fmt)
{
	m_avFrame = av_frame_alloc();

	int tot = 0;
	/*	for (uint64_t nit = 0; nit < 64; nit++) {
			uint64_t v = pow(2, nit);
			if ( ( ((uint64_t)channels) & (v) ) != 0 ) {
				tot;
			}
		}*/

	for (uint64_t tmp = channels, nit = (uint64_t)pow(2, 63); nit != 0; nit /= 2)
	{
		if (tmp >= nit)
		{
			tmp -= nit;
			tot++;
		}
	}

	if (true || m_codec->channels > tot)
	{
		m_nChannels = m_codec->channels == 1 ? 1 : std::min(tot, m_codec->channels);
		m_nChannelMask = m_codec->channels == 1 ? 1 :/* (m_nChannels == m_owner->m_ffmpeg->m_mixer->Channels) ?*/ (int)channels /*: m_codec->channel_layout*/;
	}
	else
	{
		m_nChannels = m_codec->channels;
		m_nChannelMask = m_codec->channel_layout ? m_codec->channel_layout : (1 << m_codec->channels) - 1;
	}
	if (m_nChannelMask == 0) { m_nChannelMask = (1 << m_nChannels) - 1; }

	m_nRate = samplerate;
	_samplesize = tot * (fmt == AudioFormat::Short16 ? 2 : 4);
}
audiostream::~audiostream()
{
	av_frame_free(&m_avFrame);
}

void audiostream::FrameReady()
{
	if (!m_converter)
	{
		AVSampleFormat fmt;
		switch (m_format)
		{
		case AudioFormat::Short16: fmt = AV_SAMPLE_FMT_S16; break;
		case AudioFormat::Float32: fmt = AV_SAMPLE_FMT_FLT; break;
		}

		m_converter.reset(new AudioConverter(m_codec->sample_fmt, m_codec->channel_layout ? m_codec->channel_layout : (1 << m_codec->channels) - 1, m_codec->sample_rate, fmt, m_nChannelMask, m_nRate));

		m_convertsizesamples = m_nRate;
		m_convert.reset(new float[m_convertsizesamples*m_nChannels]);
	}
	uint64_t durationsamples = m_avFrame->nb_samples*m_nRate / m_codec->sample_rate;

	if (m_converter->Valid())
	{
		int len = m_convertsizesamples;
		auto p = (uint8_t*)m_convert.get();
		int total = m_converter->Convert(&p, &len, (const uint8_t**)m_avFrame->data, m_avFrame->nb_samples);

		int len2 = total * _samplesize;

		/*	if (m_bufferlen < total * m_nChannels)
			{
				m_bufferlen = total * m_nChannels;
				m_buffer.reset(new float[total * m_nChannels]);
			}*/
			//	memcpy(m_buffer.get(), &m_convert.get()[0 * m_nChannels], len2);

				/*if (m_owner->m_rate != m_rate)
				{
					m_rate = m_owner->m_rate;
					if (m_resample)
					{
						delete m_resample;
						m_resample = 0;
					}
					if (m_rate != 1)
					{
						m_resample = new soundtouch::SoundTouch();
						m_resample->setTempo(m_rate);
						m_resample->setSampleRate(m_nRate);
						m_resample->setRate(1.0);
						m_resample->setChannels(m_nChannels);
					}
				}
			/*	if (m_resample)
				{
					{
						pin_ptr<Byte> pin(&m_buffer[0]);
						m_resample->putSamples((float*)(Byte*)pin, total);
					}
					while (m_resample->numSamples() >= m_nRate * m_nChannels / m_owner->m_ffmpeg->AudioFrames)
					{
						if (m_buffer->Length < 4 * m_nRate*m_nChannels / m_owner->m_ffmpeg->AudioFrames)
						{
							m_buffer = gcnew array<Byte>(4 * m_nRate*m_nChannels / m_owner->m_ffmpeg->AudioFrames);
						}
						{
							pin_ptr<Byte> pin(&m_buffer[0]);
							total = m_resample->receiveSamples((float*)pin, m_nRate / m_owner->m_ffmpeg->AudioFrames);
						}
						m_streamout->Write(m_buffer, 0, total*m_nChannels * 4);
						m_samplepos += total;
						m_streampos += total;
					}
				}
				else*/
		{
			//	_frameready(m_buffer.get(), total);
			_frameready(m_avFrame->pts, m_convert.get(), total);
			//	m_streamout->Write(m_buffer, 0, len);
		//		m_samplepos += total;
		//		m_streampos += total;
		}
	}
}

void audiostream::Flushed()
{
} 
void audiostream::EOS()
{
	if (m_converter)
	{
		int len = m_convertsizesamples;
		auto *p = (uint8_t*)m_convert.get();
		int total = m_converter->Convert(&p, &len, NULL, 0);

		if (total > 0)
		{
			int len = total * m_nChannels * 4;

			if (m_bufferlen < total*m_nChannels)
			{
				m_bufferlen = len;
				delete[] m_buffer.get();
				m_buffer.release();
				m_buffer.reset(new float[total*m_nChannels]);
			}
			/*	try
				{
					if (m_resample)
					{
						pin_ptr<Byte> pin(&m_buffer[0]);
						m_resample->putSamples((float*)(Byte*)pin, total);
					}
					else*/
			{
				memcpy(m_buffer.get(), &m_convert.get()[0 * m_nChannels], len);
				_frameready(m_avFrame->pts, m_buffer.get(), total);
			}
			/*	}
				catch (Exception^ e)
				{
				}
				if (m_resample)
				{
					while (m_resample->numSamples() > 0)
					{
						int ll = min(m_nRate / m_owner->m_ffmpeg->AudioFrames, m_resample->numSamples());

						if (m_buffer->Length < 4 * m_nChannels * ll)
						{
							m_buffer = gcnew array<Byte>(4 * m_nChannels * ll);
						}
						{
							pin_ptr<Byte> pin(&m_buffer[0]);
							total = m_resample->receiveSamples((float*)pin, ll);

							m_streamout->Write(m_buffer, 0, total*m_nChannels * 4);
							m_samplepos += total;
							m_streampos += total;
						}
					}
				}*/
		}
	}
}

uint64_t audiostream::Time(uint64_t time)
{
	return (uint64_t)((10000000.0* time) / this->m_codec->time_base.den);
}
