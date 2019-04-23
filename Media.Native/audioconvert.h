#pragma once

class AudioConverter
{
	SwrContext *m_swr_opts;
	struct SwrContext *m_ctx;
	bool m_doconvert;
	AVSampleFormat m_fmt_src, m_fmt_dst;
public:
	AudioConverter(AVSampleFormat fmt_src, uint64_t channels_src, int rate_src, AVSampleFormat fmt_dst, uint64_t channels_dst, int rate_dst);

	~AudioConverter()
	{
		Free();
	}
	void Free()
	{
		if (m_swr_opts != nullptr)
		{
			swr_free(&m_swr_opts);
			m_swr_opts = 0;
		}
		if (m_ctx != nullptr)
		{
			swr_free(&m_ctx);
			m_ctx = 0;
		}
	}
	/*AVSampleFormat AudioConverter::GetFormat(int fmt)
	{
	switch (fmt)
	{
	case SAMPLE_16:	return AV_SAMPLE_FMT_S16;
	//	case SAMPLE_24:	return AV_SAMPLE_FMT_S24;
	case SAMPLE_32:	return AV_SAMPLE_FMT_S32;
	case SAMPLE_FLOAT:	return AV_SAMPLE_FMT_FLT;
	}
	return AV_SAMPLE_FMT_NONE;
	}*/
	/*	int AudioConverter::Convert(LPVOID dst, int dlen, const LPVOID src, int slen)
	{
	if (m_ctx)
	{
	return swr_convert(m_ctx, (uint8_t**)&dst, dlen, (const uint8_t**)&src, slen);
	}
	return 0;
	}*/
	int Convert(uint8_t** dst, int *dlen, const uint8_t** src, int slen)
	{
			if (m_doconvert)
			{
				*dlen = std::min(*dlen,slen);

				if (*dlen == slen)
				{
					switch (m_fmt_src)
					{
					case AVSampleFormat::AV_SAMPLE_FMT_S16:
					{
						short *s = (short*)src[0];
						switch (m_fmt_dst)
						{
						case AVSampleFormat::AV_SAMPLE_FMT_FLTP:
						{
							float *d = (float*)dst[0];
							for (int nit = 0; nit < slen; nit++)
							{
								*d++ = (*s++) / (float)0x8000;
							}
						}
						return *dlen;
						}
					}
					break;
					case AVSampleFormat::AV_SAMPLE_FMT_FLTP:
					{
						float *s = (float*)src[0];
						switch (m_fmt_dst)
						{
						case AVSampleFormat::AV_SAMPLE_FMT_S16:
						{
							short *d = (short*)dst[0];
							for (int nit = 0; nit < slen; nit++)
							{
								*d++ = (short)std::max(-0x7fff, std::min(0x7fff, (int)(*s++ * 0x7fff)));
							}
						}
						return *dlen;
						}
					}
					break;
					}
				}
				return 0;
			}
			else if (m_ctx)
			{
				int len = swr_get_out_samples(m_ctx, slen);

				if (*dlen >= len)
				{
					*dlen = len;
					return swr_convert(m_ctx, dst, *dlen, src, slen);
				}
				*dlen = 0;
			}
	return 0;
	}
	bool Valid()
	{
		return m_ctx != 0;
	}
};