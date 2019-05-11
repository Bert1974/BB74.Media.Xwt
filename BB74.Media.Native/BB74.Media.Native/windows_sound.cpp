#include "stdafx.h"
#include "media.h"

#ifdef IS_WINDOWS_BUILD

class AudioOut
{
private:
	static void CALLBACK waveOutProc(HWAVEOUT hwo, UINT uMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);
	void SampleDone(WAVEHDR *h);
public:
	typedef void(__stdcall *BufferedFucnction)();
	void Write(uint8_t *data, int samples);
private:
	const int deviceid = 0;
	//	const int bitrate = 48000; // float,stereo
	const int bits = 32;
	const int buffers = 3;
public:
	int m_writelen;
private:
	std::unique_ptr<char[]> m_buffer;
	HWAVEOUT hWaveOut;
	WAVEFORMATEXTENSIBLE wfxext;
	HANDLE m_readyevent, m_stopevent, m_stoppedpevent, m_gotdata, m_canwrite, m_hthread, m_playing;
	CRITICAL_SECTION  m_wavlock, m_bufferlock;
	UCHAR *buffer;
	WAVEHDR header[25];
	ULONG m_readymask;
	UINT m_timefmt;
	__int64 btime, ltime;
	int m_bitrate, m_channels, m_frames;
	WAVEOUTCAPS m_caps; 
	AudioFormat m_format;
	int m_wpos, m_buffers;
	BufferedFucnction m_callback;
public:
	AudioOut(int bitrate, AudioFormat format, ChannelsLayout channels, int frames, int buffers);
	~AudioOut();
private:
	void Close();
	void StartThread();
public:
	DWORD audiothread();
	bool TryOpen();
	/*		bool TryOpen16();*/
	__int64 Calc(MMTIME *time);
	__int64 RefTime();
	float get_volume();
	void set_volume(float volume);

public:
	void SetBufferedCallback(BufferedFucnction callback)
	{
		m_callback = callback;
	}
	void Start();
	void Stop();
};

void CALLBACK AudioOut::waveOutProc(HWAVEOUT hwo, UINT uMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2)
{
	switch (uMsg)
	{
	case WOM_DONE:
		((AudioOut*)dwInstance)->SampleDone((WAVEHDR*)dwParam1);
		break;
	}
}
void AudioOut::SampleDone(WAVEHDR *h)
{
	::EnterCriticalSection(&m_wavlock);

	int m = (1 << (int)h->dwUser);
	m_readymask |= m;
	/*		if (m_readymask == (1 << buffers) - 1)
			{
				late++;
				//	InterlockedIncrement(&late);
			}*/
	SetEvent(m_readyevent);
	::LeaveCriticalSection(&m_wavlock);
}
	static DWORD WINAPI _audiothread(LPVOID p)
	{
		return ((AudioOut*)p)->audiothread();
	}
	DWORD AudioOut::audiothread()
	{
		SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_HIGHEST);

		HANDLE h[] = { m_stopevent, m_gotdata }; // gotdata->anything queued up for write

		while (WaitForMultipleObjects(2, h, false, INFINITE) == 1)
		{
			HANDLE h2[] = { m_stopevent,m_readyevent };// m_readyevent, wavout has free buffers

			while (WaitForMultipleObjects(2, h2, false, INFINITE) == 1)
			{
				::EnterCriticalSection(&m_bufferlock);
				int wpos = m_wpos;
				::LeaveCriticalSection(&m_bufferlock);

				long value;
				{
					::EnterCriticalSection(&m_wavlock);
					value = m_readymask;
					::LeaveCriticalSection(&m_wavlock);
				}
				int pos = 0;
				for (int nit = 0; nit < buffers; nit++)
				{
					if ((value&(1 << nit)) != 0)
					{
						int len = std::min(m_writelen, wpos-pos);

						if (len < m_writelen)
						{
							ResetEvent(m_gotdata);
							break;
						}
						WAVEHDR *h = &header[nit];

						memcpy(h->lpData, &m_buffer.get()[pos], len);

						::EnterCriticalSection(&m_wavlock);
						m_readymask &= ~(1 << (int)h->dwUser);
						if (m_readymask == 0)
						{
							ResetEvent(m_readyevent);
						}
						::LeaveCriticalSection(&m_wavlock);

						waveOutWrite(hWaveOut, h, sizeof(WAVEHDR));

						pos += len;
					}
				}// for
				if (pos > 0)
				{
					::EnterCriticalSection(&m_bufferlock);
					memmove(m_buffer.get(), &m_buffer.get()[pos], m_wpos - pos);

					m_wpos -= pos;
					if (m_wpos < m_writelen)
					{
						ResetEvent(m_gotdata);
					}
					SetEvent(m_canwrite);
					::LeaveCriticalSection(&m_bufferlock);
				}
				if (!WaitForSingleObject(m_gotdata, 0))
				{
					break;
				}
			} // while wavready
		} // while !stopped
		m_hthread = 0;
		SetEvent(m_stoppedpevent);
		return 0;
	}
	void AudioOut::Write(uint8_t *data, int samples)
	{
		HANDLE h[] = {m_stopevent, m_canwrite };

		while (samples > 0 && WaitForMultipleObjects(2, h, false, -1) == 1)
		{
			EnterCriticalSection(&m_bufferlock);

			int wlen = std::min(m_writelen * m_buffers - m_wpos, samples*wfxext.Format.nBlockAlign);

			memcpy(&m_buffer.get()[m_wpos], data, wlen);

			m_wpos += wlen; data -= wlen; samples -= wlen / wfxext.Format.nBlockAlign;

			if (m_wpos == m_writelen * m_buffers)
			{
				ResetEvent(m_canwrite);
				if (WaitForSingleObject(m_playing,0)!=WAIT_OBJECT_0)
				{
					if (m_callback)
					{
						m_callback(); // buffered
					}
				}
			}
			if (m_wpos >= m_writelen)
			{
				if (WaitForSingleObject(m_playing, 0) == WAIT_OBJECT_0)
				{
					SetEvent(m_gotdata); // start/continue play
				}
			}
			LeaveCriticalSection(&m_bufferlock);
		}
	}

AudioOut::AudioOut(int bitrate, AudioFormat format, ChannelsLayout channels, int frames,int buffers) : hWaveOut(0)
, m_timefmt(TIME_BYTES)
, m_frames(frames), m_buffers(buffers)
, m_callback(0)
{
	m_format = format;
	m_channels = channels;
	m_bitrate = bitrate;
	memset(header, 0, sizeof(header));
	m_readyevent = CreateEvent(0, true, false, 0);
	m_stopevent = CreateEvent(0, true, false, 0);
	m_stoppedpevent = CreateEvent(0, false, false, 0);
	m_gotdata = CreateEvent(0, true, false, 0);
	m_canwrite = CreateEvent(0, true, true, 0);
	m_playing = CreateEvent(0, true, false, 0);

	InitializeCriticalSection(&m_wavlock);
	InitializeCriticalSection(&m_bufferlock);
}
AudioOut::~AudioOut()
{
	Close();

	CloseHandle(m_readyevent);
	CloseHandle(m_stopevent);
	CloseHandle(m_stoppedpevent);
	CloseHandle(m_gotdata);
	CloseHandle(m_canwrite);
	CloseHandle(m_playing);

	DeleteCriticalSection(&m_wavlock);
	DeleteCriticalSection(&m_bufferlock);

	delete[] m_buffer.get();
	m_buffer.release();
}
void AudioOut::Close()
{
	if (hWaveOut != 0)
	{
		SetEvent(m_stopevent);

		if (m_hthread != 0)
		{
				WaitForSingleObject(m_stoppedpevent, INFINITE);
		}
		waveOutReset(hWaveOut);
		if (buffer != 0)
		{
			for (int nit = 0; nit < buffers; nit++)
			{
				waveOutUnprepareHeader(hWaveOut, &header[nit], sizeof(WAVEHDR));
			}
			free(buffer);
			buffer = 0;
		}
		waveOutClose(hWaveOut);
		hWaveOut = 0;
	}
}
void  AudioOut::StartThread()
{
	buffer = (UCHAR*)malloc(m_writelen*buffers);
	memset(buffer, 0, m_writelen*buffers);

	memset(header, 0, sizeof(WAVEHDR)*buffers);

	for (int nit = 0; nit < buffers; nit++)
	{
		header[nit].dwBufferLength = m_writelen;
		header[nit].lpData = (LPSTR)&buffer[nit*m_writelen];
		header[nit].dwUser = (DWORD_PTR)nit;

		waveOutPrepareHeader(hWaveOut, &header[nit], sizeof(WAVEHDR));
	}
	m_readymask = (1 << buffers) - 1;

	SetEvent(m_readyevent);

	MMTIME time2;
	time2.wType = m_timefmt;
	waveOutGetPosition(hWaveOut, &time2, sizeof(time2));

	m_timefmt = time2.wType;

	ltime = Calc(&time2);
	btime = -ltime;

	m_buffer.reset(new char[m_writelen*m_buffers]);
	m_wpos = 0;

/*	DWORD thid;
	HANDLE ht=::CreateThread(0, 0, &_audiothread, this, 0, &thid);
	::SetThreadDescription(ht, L"audioout");*/
}
bool  AudioOut::TryOpen()
{
	int tot = 0;
	for (uint64_t tmp = m_channels, nit = pow(2, 63); nit != 0; nit /= 2)
	{
		if (tmp >= nit)
		{
			tmp -= nit;
			tot++;
		}
	}

	memset(&wfxext, 0, sizeof(wfxext));

	wfxext.Format.cbSize = 22;
	wfxext.Format.nChannels = tot;
	wfxext.Format.nSamplesPerSec = m_bitrate;

	switch (m_format)
	{
		case AudioFormat::SampleFloat32:
			{
				GUID guid = KSDATAFORMAT_SUBTYPE_IEEE_FLOAT;
				wfxext.Format.wFormatTag = WAVE_FORMAT_EXTENSIBLE;
				wfxext.SubFormat = guid;
				wfxext.Format.wBitsPerSample = 32;
			}
			break;
		case AudioFormat::SampleShort16:
			{
			wfxext.Format.wFormatTag = WAVE_FORMAT_PCM;
			wfxext.Format.wBitsPerSample = 16;
			}
			break;
		default:
			throw new _err("not implemented");
	}


	wfxext.Format.nBlockAlign = (wfxext.Format.wBitsPerSample/8) * tot;
	wfxext.Format.nAvgBytesPerSec = m_bitrate * wfxext.Format.nBlockAlign;

	wfxext.Samples.wValidBitsPerSample = wfxext.Format.wBitsPerSample;
	wfxext.dwChannelMask = m_channels;// (1 << m_channels) - 1;;

	m_writelen = m_buffers*(m_bitrate / m_frames)*wfxext.Format.nBlockAlign;

	if (waveOutOpen(&hWaveOut, deviceid, &wfxext.Format, (DWORD_PTR)&waveOutProc, (DWORD_PTR)this, CALLBACK_FUNCTION) == MMSYSERR_NOERROR)
	{
		waveOutGetDevCaps((UINT_PTR)hWaveOut, &m_caps, sizeof(m_caps));

		StartThread();

		__printf("sound open");

		return true;
	}
	return false;
}

__int64  AudioOut::Calc(MMTIME *time)
{
	switch (time->wType)
	{
	case TIME_MS:
		return time->u.ms;
	case TIME_BYTES:
		return (__int64)((((__int64)time->u.cb) / wfxext.Format.nBlockAlign) / (wfxext.Format.nSamplesPerSec / 1000.0));
	case TIME_SMPTE:
		return ((__int64)time->u.smpte.hour * 3600 + time->u.smpte.min * 60 + time->u.smpte.sec) * 1000 + time->u.smpte.frame * 1000 / time->u.smpte.fps;
	}
}
__int64  AudioOut::RefTime()
{
	MMTIME time;
	time.wType = m_timefmt;
	waveOutGetPosition(hWaveOut, &time, sizeof(time));

	__int64 result = Calc(&time);

	return result + this->btime;
}
float AudioOut::get_volume()
{
	UINT volume;
	waveOutGetVolume(hWaveOut, (DWORD*)&volume);

	return (volume & 0xffff) / (float)0xffff;
}
void AudioOut::set_volume(float volume)
{
	UINT n = std::max(0, std::min(0xffff, (int)(volume * 0xffff)));

	if (m_caps.dwSupport&WAVECAPS_LRVOLUME)
	{
		waveOutSetVolume(hWaveOut, ((n) | (n << 16)));
	}
	else
	{
		waveOutSetVolume(hWaveOut, n);
	}
}

void AudioOut::Start()
{
	DWORD thid;
	m_hthread = ::CreateThread(0, 0, &_audiothread, this, 0, &thid);
	::SetThreadDescription(m_hthread, L"audioout");

	EnterCriticalSection(&m_bufferlock);
	SetEvent(m_playing);
	if (m_wpos >= m_writelen) 
	{
		SetEvent(m_gotdata);
	}
	LeaveCriticalSection(&m_bufferlock);
}
void AudioOut::Stop()
{
	SetEvent(m_stopevent);
	if (m_hthread != 0)
	{
		WaitForSingleObject(m_stoppedpevent, INFINITE);
	}
	m_wpos = 0;

	EnterCriticalSection(&m_bufferlock);
	ResetEvent(m_playing);
	LeaveCriticalSection(&m_bufferlock);
}

extern "C" {

	FUNCEXP AudioOut *openaudio(int bitrate, AudioFormat format, ChannelsLayout channels, int frames, int buffers,char *error)
	{
		try
		{
			auto result = new AudioOut(bitrate, format, channels, frames, buffers);

			if (result->TryOpen())
			{
				return result;
			}
			delete result;
#pragma warning (suppress:4996)
			strcpy(error, "can't open");
			return 0;
		}
		catch (_err *e)
		{
#pragma warning (suppress:4996)
			strcpy(error, e->m_text.c_str());
			delete e;
			return 0;
		}
	}
	FUNCEXP void closeaudio(AudioOut* p)
	{
		if (p) {
			delete p;
		}
	}
	FUNCEXP void audio_write(AudioOut * audio, uint8_t *data, int leninsamples)
	{
		audio->Write(data, leninsamples);
	}
	FUNCEXP int audio_bufsize(AudioOut * audio)
	{
		return audio->m_writelen;
	}
	FUNCEXP void audio_start(AudioOut * audio)
	{
		audio->Start();
	}
	FUNCEXP void audio_stop(AudioOut * audio)
	{
		audio->Stop();
	}
	FUNCEXP void audio_setcallback(AudioOut *audio, AudioOut::BufferedFucnction callback)
	{
		audio->SetBufferedCallback(callback);		
	}
}

#endif