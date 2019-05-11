#pragma once

#pragma pack(push, 1)              // Set structure packing to 1 

enum VideoFormat : int
{
	RGB,
	RGBA,
	ARGB,
	YUV420,
	YUV422
};

enum AudioFormat : int
{
	SampleShort16,
	SampleFloat32,
	SampleInt32
};

enum ChannelsLayout : uint64_t
{
	Stereo = 3,
	Dolby = 0x60f
};

struct FPS
{
	struct AVRational Number;
	bool Interlaced;
};

struct videostreaminfo
{
	uint ind;
	int width, height, ticks;
	struct AVRational fps, time_base;
};


struct audiostreaminfo
{
	uint ind;
	uint samplerate, channels;
	AudioFormat format;
	//uint filler;
	uint64_t channelmask;
	struct AVRational fps, time_base;
};

#pragma pack(pop)              // Set structure packing back to default

