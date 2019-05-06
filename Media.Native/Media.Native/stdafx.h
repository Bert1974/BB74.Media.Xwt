// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once

#ifdef IS_WINDOWS_BUILD
#include "targetver.h"

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
// Windows Header Files
#include <windows.h>
#include <mmreg.h>
#include <mmeapi.h>
#include <deque>
//#if IS_WINDOWS_BUILD
#include <queue>
/*#else
#include <stl_queue.h>
#endif*/

#undef min
#undef max

#endif

#ifndef IS_WINDOWS_BUILD // or something like that...
#define __stdcall
#endif

#ifdef IS_WINDOWS_BUILD
#define FUNCEXP	__declspec(dllexport)
#else
#define FUNCEXP
#endif

#ifdef IS_WINDOWS_BUILD
#define THREADTYPE DWORD
#define SETTHREADNAME(th,name) SetThreadDescription((HANDLE)th->native_handle(),L##name)
#define CURRENTTHREADID() (DWORD)GetCurrentThreadId()
//#define SETTHREADPRI(th,pri) SetThreadPriority((HANDLE)th->native_handle(), pri)
#elif defined(IS_OSX_BUILD)
#define THREADTYPE pthread_t
#define SETTHREADNAME(th,name)
#define CURRENTTHREADID() (pthread_t)pthread_self()
#elif defined(IS_LINUX_BUILD)
#define THREADTYPE pthread_t
#define SETTHREADNAME(th,name) pthread_setname_np((pthread_t)th->native_handle(),name)
#define CURRENTTHREADID() (pthread_t)pthread_self()
#else
#error "no os defined"
#endif
// reference additional headers your program requires here

extern "C" {
#ifndef __STDC_CONSTANT_MACROS
#  define __STDC_CONSTANT_MACROS
#endif

	//#define inline __inline

#include "stdint.h"
#include <inttypes.h>
	//#include "config.h"
#include <math.h>
#include <limits.h>
#include "libavutil/avstring.h"
	//>>#include "libavutil/colorspace.h"
#include "libavutil/pixdesc.h"
	//#include "libavcore/imgutils.h"
	//#include "libavcore/parseutils.h"
	//#include "libavcore/samplefmt.h"
#include "libavformat/avformat.h"
#include "libavdevice/avdevice.h"
#include "libswscale/swscale.h"
	//>>#include "libavcodec/audioconvert.h"
	//#include "libavcodec/opt.h"
#include "libavcodec/avfft.h"
#include "libavfilter/avfilter.h"
#include "libavutil/pixfmt.h"
#include "libswresample/swresample.h"

	typedef struct AVCodecTag {
		enum AVCodecID id;
		unsigned int tag;
	} AVCodecTag;


	//>>#include "libavformat/id3v2.h"

//#define SWS_CPUFLAGS	(SWS_CPU_CAPS_MMX|SWS_CPU_CAPS_MMX2|SWS_CPU_CAPS_3DNOW|SWS_CPU_CAPS_ALTIVEC|SWS_CPU_CAPS_BFIN|SWS_CPU_CAPS_SSE2)
}

//#include "soundtouch/include/SoundTouch.h"

#include <string>
#include <stdarg.h>  // For va_start, etc.
#include <memory>    // For std::unique_ptr
#include <vector>
#include <thread>
#include <mutex>
#include <condition_variable>
#include <cmath>
#include <algorithm>    // std::min

typedef unsigned int	uint;


void FFMPEGLOCK();
void FFMPEGUNLOCK();

std::string string_format(const std::string fmt, ...);
std::string ERRSTR(int error);


typedef void(__stdcall *debugstringfunction)(const char *txt);
extern "C" {
	FUNCEXP void __printf(std::string fmt, ...);
}
class _err
{
public:
	_err(const char * text)
		: m_text(text)
	{
	}
	_err(const std::string text)
		: m_text(text)
	{
	}
	_err(std::string fmt, ...)
	{
		int size = ((int)fmt.size()) * 2 + 50;   // Use a rubric appropriate for your code
		std::string str;
		va_list ap;
		while (1) {     // Maximum two passes on a POSIX system...
			str.resize(size);
			va_start(ap, fmt);
			int n = vsnprintf((char *)str.data(), size, fmt.c_str(), ap);
			va_end(ap);
			if (n > -1 && n < size) {  // Everything worked
				str.resize(n);

				m_text = str;
				return ;
			}
			if (n > -1)  // Needed size returned
				size = n + 1;   // For null char
			else
				size *= 2;      // Guess at a larger size (OS specific)
		}
		m_text= str;
	}
public:
	std::string m_text;
};
