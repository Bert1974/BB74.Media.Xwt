#include "stdafx.h"

#if defined(IS_LINUX_BUILD) || defined(IS_OSX_BUILD)

class lockit
{
public:
	std::mutex m_mutex;
	std::unique_lock<std::mutex> m_lock;

	lockit()
		: m_lock(m_mutex)
	{
		m_lock.unlock();
	}
};

lockit ffmpeglock;

void FFMPEGLOCK()
{
	ffmpeglock.m_lock.lock();
}
void FFMPEGUNLOCK()
{
	ffmpeglock.m_lock.unlock();
}


#endif