#include "stdafx.h"
#include "audioconvert.h"

AudioConverter::AudioConverter(AVSampleFormat fmt_src, uint64_t channels_src, int rate_src, AVSampleFormat fmt_dst, uint64_t channels_dst, int rate_dst)
		: m_ctx(0), m_doconvert(false)
{
	//AVSampleFormat fmt_src2 = GetFormat(fmt_src);
	//AVSampleFormat fmt_dst2 = GetFormat(fmt_dst);

	//	if (fmt_src2!=fmt_dst2 || channels_src!=channels_dst || rate_src!= rate_dst)
	{

		{
			m_swr_opts = swr_alloc();

			m_ctx = swr_alloc_set_opts(0,
				channels_dst/*AV_CH_LAYOUT_STEREO*/, fmt_dst/*AV_SAMPLE_FMT_FLT*/, rate_dst,
				channels_src, fmt_src, rate_src,
				0, 0);

			if (swr_init(m_ctx) < 0)
			{
				Free();

				if (rate_dst == rate_src && channels_src == channels_dst)
				{
					if (fmt_dst == AV_SAMPLE_FMT_FLT)
					{
						m_fmt_dst = fmt_dst;
						switch ((m_fmt_src = fmt_src))
						{
						case AVSampleFormat::AV_SAMPLE_FMT_S16:
						case AVSampleFormat::AV_SAMPLE_FMT_S32:
						case AVSampleFormat::AV_SAMPLE_FMT_FLTP:
							m_doconvert = true;
							return;
						}
					}
				}
			}
		}
	}
}
