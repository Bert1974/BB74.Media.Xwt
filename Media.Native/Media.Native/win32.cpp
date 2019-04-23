#include "stdafx.h"

#ifdef IS_WINDOWS_BUILD
class CriticalSectionHolder
{
public:
	CriticalSectionHolder(void);
	virtual ~CriticalSectionHolder(void);

	LPCRITICAL_SECTION GetCriticalSection() { return &m_criticalSection; }

private:
	CRITICAL_SECTION m_criticalSection;
};

CriticalSectionHolder::CriticalSectionHolder(void)
{
	::InitializeCriticalSection(&m_criticalSection);
}

CriticalSectionHolder::~CriticalSectionHolder(void)
{
	::DeleteCriticalSection(&m_criticalSection);
}
CriticalSectionHolder ffmpeglock;

void FFMPEGLOCK()
{
	EnterCriticalSection(ffmpeglock.GetCriticalSection());
}
void FFMPEGUNLOCK()
{
	LeaveCriticalSection(ffmpeglock.GetCriticalSection());
}

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		break;
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

#endif