rem if NOT "%DevEnvDir%"=="" goto devenvok;
rem call "c:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build\vcvarsx86_amd64.bat"
rem if "%DevEnvDir%"=="" goto Error;
rem :devenvok

rem "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\devenv.com" BB74.OpenTK.Xwt.sln /rebuild Release /project Media.Base
rem IF ERRORLEVEL 1 GOTO Error
rem cd Media.Base
rem nuget pack Media.Base.csproj -properties configuration=Release

cd BB74.Media.Base
call build.bat
IF ERRORLEVEL 1 GOTO Error
cd ..

cd BB74.Media.Native
call build.bat
IF ERRORLEVEL 1 GOTO Error
cd ..

rem cd ..\package
rem nuget pack BB74.Media.Base.nuspec -BasePath .\package -properties configuration=Release
rem IF ERRORLEVEL 1 GOTO Error
rem cd ..\..
goto exit
:Error
echo "error"
pause
:exit