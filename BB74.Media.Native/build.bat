set BB74_CONFIG=Release
set BB74_PUBLISH=default
set BB74_VERSIONONLY=FALSE

:parse
IF "%~1"=="" GOTO endparse
IF "%~1"=="debug" set BB74_CONFIG=Debug
IF "%~1"=="release" set BB74_CONFIG=Release
IF "%~1"=="versiononly" set BB74_VERSIONONLY=true
IF "%~1"=="bert" set BB74_PUBLISH=bert
IF NOT "%~1"=="clean" goto noclean
for %%i in (.\packages\*) do if not "%%i"==".\packages\.gitignore" del %%i
goto Exit
:noclean
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
md ref
md ref\net40
rem md lib\net45
rem md lib\net472

cd ..

if NOT "%BB74_PUBLISH%"=="bert" goto _skipincver
..\bin\incversion BB74.Media.Interop\Properties\AssemblyInfo.cs 
IF ERRORLEVEL 1 GOTO Error
..\bin\incversion BB74.Media.Interop.Impl\Properties\AssemblyInfo.cs 
IF ERRORLEVEL 1 GOTO Error
:_skipincver

msbuild ..\BB74.OpenTK.Xwt.sln /t:BB74_Media_Native\BB74_Media_Interop_Impl:Rebuild;BB74_Media_Native\BB74_Media_Interop:Rebuild /p:ProjectReferences=false /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%;Platform="Any CPU"
IF ERRORLEVEL 1 GOTO Error

msbuild ..\BB74.OpenTK.Xwt.sln /t:BB74_Media_Native\BB74_Media_Native:Rebuild;BB74_Media_Native\BB74_Media_Interop_Impl:Rebuild /p:ProjectReferences=false /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%;Platform=x86
IF ERRORLEVEL 1 GOTO Error

msbuild ..\BB74.OpenTK.Xwt.sln /t:BB74_Media_Native\BB74_Media_Native:Rebuild;BB74_Media_Native\BB74_Media_Interop_Impl:Rebuild /p:ProjectReferences=false /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%;Platform=x64
IF ERRORLEVEL 1 GOTO Error

copy "BB74.Media.Native\make-x11\libBB74.Media.Native.dll.so" "package\data\x64"
IF ERRORLEVEL 1 GOTO Error

copy "BB74.Media.Native\make-osx\libBB74.Media.Native.dll.dylib" "package\data\x64"
IF ERRORLEVEL 1 GOTO Error

rem cd BB74.Media.Native
rem msbuild BB74.Media.Native.vcxproj /t:Clean,Build /p:Configuration=%BB74_CONFIG%,Platform=Win32
rem rem "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\devenv.com"  BB74.Media.Native.vcxproj /rebuild %BB74_CONFIG% /projectconfig "%BB74_CONFIG%|Win32"
rem IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Media.Native.vcxproj /t:Clean,Build /p:Configuration=%BB74_CONFIG%,Platform=x64
rem rem "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\devenv.com" BB74.Media.Native.vcxproj /rebuild "%BB74_CONFIG%|x64"
rem IF ERRORLEVEL 1 GOTO Error

copy BB74.Media.Interop\bin\%BB74_CONFIG%\BB74.Media.Interop.pdb package\lib\net40\
copy BB74.Media.Interop\bin\%BB74_CONFIG%\BB74.Media.Interop.dll package\lib\net40\
IF ERRORLEVEL 1 GOTO Error
copy BB74.Media.Interop.Impl\bin\%BB74_CONFIG%\BB74.Media.Interop.Impl.* package\ref\net40\
IF ERRORLEVEL 1 GOTO Error
copy BB74.Media.Interop.Impl\bin\x64\%BB74_CONFIG%\BB74.Media.Interop.x64.* package\data\
IF ERRORLEVEL 1 GOTO Error
copy BB74.Media.Interop.Impl\bin\x86\%BB74_CONFIG%\BB74.Media.Interop.x86.* package\data\
IF ERRORLEVEL 1 GOTO Error

rem cd ..
rem msbuild BB74.Media.Interop.Impl\BB74.Media.Interop.Impl.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=..\package\ref\net40
rem IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Media.Interop.Impl\BB74.Media.Interop.Impl.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=x64 /p:OutputPath=..\package\lib\net40 /p:DefineConstants=X64_BUILD /p:AssemblyName=BB74.Media.Interop.x64
rem IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Media.Interop.Impl\BB74.Media.Interop.Impl.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=x86 /p:OutputPath=..\package\lib\net40 /p:DefineConstants=X86_BUILD /p:AssemblyName=BB74.Media.Interop.x86
rem IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Media.Interop.Impl\BB74.Media.Interop.Impl.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=Release,Platform=AnyCPU
rem IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Media.Interop\BB74.Media.Interop.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=..\package\lib\net40
rem IF ERRORLEVEL 1 GOTO Error
del package\lib\net40\BB74.Media.Interop.Impl.*
IF ERRORLEVEL 1 GOTO Error

:versiononly

..\bin\getversion -version_ext "%BB74_VERSION%" -assembly package\lib\net40\BB74.Media.Interop.dll BB74.Media.FFMPEG._nuspec _tmp\BB74.Media.FFMPEG.nuspec
IF ERRORLEVEL 1 GOTO Error

if "%BB74_VERSIONONLY%"=="TRUE" goto exit

nuget pack _tmp\BB74.Media.FFMPEG.nuspec -BasePath .\package -OutputDirectory packages\ -properties configuration=%BB74_CONFIG%
if ERRORLEVEL 1 GOTO Error

if NOT "%BB74_PUBLISH%"=="bert" goto exit

..\bin\getversion -version_ext "%BB74_VERSION%" -assembly package\lib\net40\BB74.Media.Interop.dll copy._bat _tmp\copy.bat
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