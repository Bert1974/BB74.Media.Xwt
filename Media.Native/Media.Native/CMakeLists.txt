cmake_minimum_required(VERSION 3.5.1)

OPTION(DEFINE_WINBUILD "Option description" OFF) 

IF(DEFINE_WINBUILD)
    ADD_DEFINITIONS(-DIS_WINDOWS_BUILD)
ENDIF(DEFINE_WINBUILD)

#set (FFMPEG_DIR ~/Projects/BBR/BaseLib/BaseLib.FFMPEG/sandbox/native/ffmpeg_git_xp_compat_lgpl_shared)
set (FFMPEG_DIR ~/ffmpeg/sandbox/cross_compilers/native)
#set (FFMPEG_DIR ~/tmp/sandbox-org/home/bert/ffmpeg/sandbox/native/ffmpeg_git_xp_compat_lgpl_shared)

set (CMAKE_PREFIX_PATH ${FFMPEG_DIR}lib/pkgconfig )
set (ENV{PKG_CONFIG_PATH} $ENV{PKG_CONFIG_PATH};${FFMPEG_DIR}/lib/pkgconfig )

find_package(PkgConfig REQUIRED)
pkg_check_modules(FFMPEG REQUIRED libavfilter libavformat libavcodec libswresample libavresample libavutil libswscale)

link_directories(${FFMPEG_LIBRARY_DIRS})

set(Media.Native.dll PROPERTIES
   CMAKE_CXX_STANDARD 11
   CMAKE_CXX_STANDARD_REQUIRED ON)

add_library(Media.Native.dll SHARED
    interop.cpp
    audioconvert.cpp
    movieplayer.cpp
    movieplayer_basestream.cpp
    movieplayer_audio.cpp
    movieplayer_video.cpp
    linux_sound.cpp
    linux.cpp
    windows_sound.cpp
    win32.cpp
)
#target_compile_options(Media.Native.dll PRIVATE -fPIC)
target_compile_options(Media.Native.dll PRIVATE -march=core2)
target_compile_options(Media.Native.dll PRIVATE -std=c++11)

include_directories(${FFMPEG_INCLUDE_DIRS})
#include_directories(${CMAKE_PREFIX_PATH}/include)

target_link_libraries(Media.Native.dll LINK_PRIVATE -Wl,--start-group ${FFMPEG_LIBRARIES} -Wl,--end-group asound)
