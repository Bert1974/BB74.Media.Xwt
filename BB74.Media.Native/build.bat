set BB74_CONFIG=Release
set BB74_PUBLISH=default
set BB74_VERSIONONLY=FALSE

:parse
IF "%~1"=="" GOTO endparse
IF "%~1"=="debug" set BB74_CONFIG=Debug
IF "%~1"=="release" set BB74_CONFIG=Release
IF "%~1"=="versiononly" set BB74_VERSIONONLY=true
IF "%~1"=="bert" set BB74_PUBLISH=bert
SHIFT
GOTO parse
:endparse

if "%BB74_VERSIONONLY%"=="TRUE" goto versiononly

IF "%BB74_CONFIG%"=="Release" goto release_build
set BB74_VERSION=-debug
goto _release_build
:release_build
set BB74_VERSION=
:_release_build

if NOT "%DevEnvDir%"=="" goto devenvok
call "c:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build\vcvarsx86_amd64.bat"
if "%DevEnvDir%"=="" goto Error
:devenvok

cd package
IF ERRORLEVEL 1 GOTO Error

rmdir data /q /s
IF ERRORLEVEL 1 GOTO Error
rmdir lib  /q /s
IF ERRORLEVEL 1 GOTO Error
rmdir ref  /q /s
IF ERRORLEVEL 1 GOTO Error

md data
md data\x86
md data\x64
md lib
md lib\net40
rem md lib\net45
rem md lib\net472

cd ..

copy "BB74.Media.Native\make-x11\libBB74.Media.Native.dll.so" "package\data\x64"
IF ERRORLEVEL 1 GOTO Error
copy "BB74.Media.Native\make-osx\libBB74.Media.Native.dll.dylib" "package\data\x64"
IF ERRORLEVEL 1 GOTO Error

cd BB74.Media.Native
msbuild BB74.Media.Native.vcxproj /t:Clean,Build /p:Configuration=%BB74_CONFIG%,Platform=Win32
rem "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\devenv.com"  BB74.Media.Native.vcxproj /rebuild %BB74_CONFIG% /projectconfig "%BB74_CONFIG%|Win32"
IF ERRORLEVEL 1 GOTO Error
msbuild BB74.Media.Native.vcxproj /t:Clean,Build /p:Configuration=%BB74_CONFIG%,Platform=x64
rem "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\devenv.com" BB74.Media.Native.vcxproj /rebuild "%BB74_CONFIG%|x64"
IF ERRORLEVEL 1 GOTO Error

cd ..
msbuild BB74.Media.Interop.Impl\BB74.Media.Interop.Impl.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=..\package\ref\net40
IF ERRORLEVEL 1 GOTO Error
msbuild BB74.Media.Interop.Impl\BB74.Media.Interop.Impl.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=x64 /p:OutputPath=..\package\ref\net40 /p:DefineConstants=X64_BUILD /p:AssemblyName=BB74.Media.Interop.x64
IF ERRORLEVEL 1 GOTO Error
msbuild BB74.Media.Interop.Impl\BB74.Media.Interop.Impl.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=x86 /p:OutputPath=..\package\ref\net40 /p:DefineConstants=X86_BUILD /p:AssemblyName=BB74.Media.Interop.x86
IF ERRORLEVEL 1 GOTO Error
msbuild BB74.Media.Interop.Impl\BB74.Media.Interop.Impl.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=Release,Platform=AnyCPU
IF ERRORLEVEL 1 GOTO Error
msbuild BB74.Media.Interop\BB74.Media.Interop.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=..\package\lib\net40
IF ERRORLEVEL 1 GOTO Error
del package\lib\net40\BB74.Media.Interop.Impl.*
IF ERRORLEVEL 1 GOTO Error

rem msbuild BB74.Media.Interop.Impl\BB74.Media.Interop.Impl.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.5;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=..\package\ref\net45
rem IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Media.Interop.Impl\BB74.Media.Interop.Impl.x64.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.5;Configuration=%BB74_CONFIG%,Platform=x64 /p:OutputPath=..\package\lib\net45
rem IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Media.Interop.Impl\BB74.Media.Interop.Impl.x86.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.5;Configuration=%BB74_CONFIG%,Platform=x86 /p:OutputPath=..\package\lib\net45
rem IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Media.Interop.Impl\BB74.Media.Interop.Impl.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.5;Configuration=Release,Platform=AnyCPU
rem IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Media.Interop\BB74.Media.Interop.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.5;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=..\package\lib\net45
rem IF ERRORLEVEL 1 GOTO Error
rem del .\package\lib\net45\BB74.Media.Interop.Impl.*
rem IF ERRORLEVEL 1 GOTO Error

rem msbuild BB74.Media.Interop.Impl\BB74.Media.Interop.Impl.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.7.2;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=..\package\ref\net472
rem IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Media.Interop.Impl\BB74.Media.Interop.Impl.x64.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.7.2;Configuration=%BB74_CONFIG%,Platform=x64 /p:OutputPath=..\package\lib\net472
rem IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Media.Interop.Impl\BB74.Media.Interop.Impl.x86.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.7.2;Configuration=%BB74_CONFIG%,Platform=x86 /p:OutputPath=..\package\lib\net472
rem IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Media.Interop.Impl\BB74.Media.Interop.Impl.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.7.2;Configuration=Release,Platform=AnyCPU
rem IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Media.Interop\BB74.Media.Interop.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.7.2;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=..\package\lib\net472
rem IF ERRORLEVEL 1 GOTO Error
rem del .\package\lib\net472\BB74.Media.Interop.Impl.*
rem IF ERRORLEVEL 1 GOTO Error

:versiononly

..\bin\getversion -version_ext "%BB74_VERSION%" package\lib\net40\BB74.Media.Interop.dll BB74.Media.FFMPEG._nuspec _tmp\BB74.Media.FFMPEG.nuspec
IF ERRORLEVEL 1 GOTO Error

if "%BB74_VERSIONONLY%"=="TRUE" goto exit

nuget pack _tmp\BB74.Media.FFMPEG.nuspec -BasePath .\package -properties configuration=%BB74_CONFIG% -OutputDirectory packages\
if ERRORLEVEL 1 GOTO Error

if NOT "%BB74_PUBLISH%"=="bert" goto exit

..\bin\getversion -version_ext "%BB74_VERSION%" package\lib\net40\BB74.Media.Interop.dll copy._bat _tmp\copy.bat
IF ERRORLEVEL 1 GOTO Error

call _tmp\copy.bat
IF ERRORLEVEL 1 GOTO Error
goto exit

goto Exit
:Error
pause

:Exit
set BB74_CONFIG=
set BB74_VERSION=