if NOT "%DevEnvDir%"=="" goto devenvok;
call "c:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build\vcvarsx86_amd64.bat"
if "%DevEnvDir%"=="" goto Error;
:devenvok

copy "BB74.Media.Native\make-osx\libBB74.Media.Native.dll.dylib" "package\data\x64\"
IF ERRORLEVEL 1 GOTO Error

cd BB74.Media.Native
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\devenv.com"  BB74.Media.Native.vcxproj /rebuild Release /projectconfig "Release|Win32"
IF ERRORLEVEL 1 GOTO Error
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\devenv.com" BB74.Media.Native.vcxproj /rebuild "Release|x64"
IF ERRORLEVEL 1 GOTO Error

cd ..\BB74.Media.Interop.Impl
msbuild BB74.Media.Interop.Impl.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=Release,Platform=AnyCPU /p:OutputPath=..\package\data\net40
IF ERRORLEVEL 1 GOTO Error
msbuild BB74.Media.Interop.Impl.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.5;Configuration=Release,Platform=AnyCPU /p:OutputPath=..\package\data\net45
IF ERRORLEVEL 1 GOTO Error
msbuild BB74.Media.Interop.Impl.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.7.2;Configuration=Release,Platform=AnyCPU /p:OutputPath=..\package\data\net472
IF ERRORLEVEL 1 GOTO Error

cd ..\BB74.Media.Interop
msbuild BB74.Media.Interop.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=Release,Platform=AnyCPU /p:OutputPath=..\package\lib\net40
IF ERRORLEVEL 1 GOTO Error
del ..\package\lib\net40\BB74.Media.Interop.Impl.*
IF ERRORLEVEL 1 GOTO Error
msbuild BB74.Media.Interop.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.5;Configuration=Release,Platform=AnyCPU /p:OutputPath=..\package\lib\net45
IF ERRORLEVEL 1 GOTO Error
del ..\package\lib\net45\BB74.Media.Interop.Impl.*
IF ERRORLEVEL 1 GOTO Error
msbuild BB74.Media.Interop.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.7.2;Configuration=Release,Platform=AnyCPU /p:OutputPath=..\package\lib\net472
IF ERRORLEVEL 1 GOTO Error
del ..\package\lib\net472\BB74.Media.Interop.Impl.*
IF ERRORLEVEL 1 GOTO Error

cd ..

..\bin\getversion package\lib\net40\BB74.Media.Interop.dll BB74.Media.FFMPEG._nuspec BB74.Media.FFMPEG.nuspec
IF ERRORLEVEL 1 GOTO Error

nuget pack BB74.Media.FFMPEG.nuspec -BasePath .\package -properties configuration=Release
if ERRORLEVEL 1 GOTO Error

goto exit

goto Exit
:Error
pause

:Exit