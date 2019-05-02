"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\devenv.com" BB74.OpenTK.Xwt.sln /rebuild Release /project Media.Base
IF ERRORLEVEL 1 GOTO Error
cd Media.Base
nuget pack Media.Base.csproj -properties configuration=Release
IF ERRORLEVEL 1 GOTO Error
cd ..
cd Media.Native\Media.Native
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\devenv.com"  Media.Native.vcxproj /rebuild Release /projectconfig "Release|Win32"
IF ERRORLEVEL 1 GOTO Error
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\devenv.com" Media.Native.vcxproj /rebuild "Release|x64"
IF ERRORLEVEL 1 GOTO Error
cd ..\..
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\devenv.com" BB74.OpenTK.Xwt.sln /rebuild Release /project Media.Interop.Impl
IF ERRORLEVEL 1 GOTO Error
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\devenv.com" BB74.OpenTK.Xwt.sln /rebuild Release /project Media.Interop
IF ERRORLEVEL 1 GOTO Error
cd Media.Native\package
nuget pack
IF ERRORLEVEL 1 GOTO Error
cd ..\..
goto exit
:Error
echo "error"
pause
:exit