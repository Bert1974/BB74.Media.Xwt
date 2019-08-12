cmake_minimum_required(VERSION 3.5.1)

OPTION(DEFINE_WINBUILD "Option description" OFF) 
OPTION(DEFINE_LINUXBUILD "Option description" OFF) 
OPTION(DEFINE_OSXBUILD "Option description" OFF) 

IF(DEFINE_WINBUILD)
    ADD_DEFINITIONS(-DIS_WINDOWS_BUILD)
ENDIF(DEFINE_WINBUILD)
IF(DEFINE_LINUXBUILD)
    ADD_DEFINITIONS(-DIS_LINUX_BUILD)
ENDIF(DEFINE_LINUXBUILD)
IF(DEFINE_OSXBUILD)
    ADD_DEFINITIONS(-DIS_OSX_BUILD)
ENDIF(DEFINE_OSXBUILD)

#set (FFMPEG_DIR ~/Projects/BBR/BaseLib/BaseLib.FFMPEG/sandbox/native/ffmpeg_git_xp_compat_lgpl_shared)
set (FFMPEG_DIR ~/Projects/ffmpeg/sandbox/cross_compilers/native)
#set (FFMPEG_DIR ~/tmp/sandbox-org/home/bert/ffmpeg/sandbox/native/ffmpeg_git_xp_compat_lgpl_shared)

set (CMAKE_PREFIX_PATH ${FFMPEG_DIR}lib/pkgconfig )
set (ENV{PKG_CONFIG_PATH} $ENV{PKG_CONFIG_PATH};${FFMPEG_DIR}/lib/pkgconfig )

find_package(PkgConfig REQUIRED)
pkg_check_modules(FFMPEG REQUIRED libavutil libavfilter libavformat libavcodec libswresample libavresample libswscale)

link_directories(${FFMPEG_LIBRARY_DIRS})

set(BB74.Media.Native.dll PROPERTIES
   CMAKE_CXX_STANDARD 11
   CMAKE_CXX_STANDARD_REQUIRED ON)

add_library(BB74.Media.Native.dll SHARED
    interop.cpp
	all.cpp
    audioconvert.cpp
    movieplayer.cpp
    movieplayer_basestream.cpp
    movieplayer_audio.cpp
    movieplayer_video.cpp
    linux_sound.cpp
    linux.cpp
    osx_sound.cpp
    windows_sound.cpp
	movierecorder.cpp
	movierecorder_stream.cpp
    win32.cpp
)
#target_compile_options(BB74.Media.Native.dll PRIVATE -fPIC)
target_compile_options(BB74.Media.Native.dll PRIVATE -march=core2)
target_compile_options(BB74.Media.Native.dll PRIVATE -std=c++11)
target_compile_options(BB74.Media.Native.dll PRIVATE -m64)

include_directories(${FFMPEG_INCLUDE_DIRS})
#include_directories(${CMAKE_PREFIX_PATH}/include)

IF (DEFINE_OSXBUILD)
target_link_libraries(BB74.Media.Native.dll LINK_PRIVATE ${FFMPEG_LIBRARIES} vpx vorbis vorbisenc zvbi twolame theora theoradec theoraenc opus speex openjp2 aom webp ogg snappy mp3lame twolame gsm sndfile openh264 opencore-amrwb opencore-amrnb vo-amrwbenc gnutls ilbc "-framework Security" "-framework CoreVideo" "-framework CoreFoundation" "-framework AudioToolbox" "-framework VideoToolbox" "-framework CoreMedia" )
ENDIF(DEFINE_OSXBUILD)
IF (DEFINE_LINUXBUILD)
target_link_libraries(BB74.Media.Native.dll LINK_PRIVATE -Wl,--start-group ${FFMPEG_LIBRARIES} -Wl,--end-group asound)
ENDIF(DEFINE_LINUXBUILD)
