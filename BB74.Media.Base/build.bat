
if NOT "%DevEnvDir%"=="" goto devenvok;
call "c:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build\vcvarsx86_amd64.bat"
if "%DevEnvDir%"=="" goto Error;
:devenvok

msbuild BB74.Media.Base.csproj /p:TargetFrameworkVersion=v4.0;Configuration=Release,Platform=AnyCPU /p:OutputPath=.\package\lib\net40
IF ERRORLEVEL 1 GOTO Error
msbuild BB74.Media.Base.csproj /p:TargetFrameworkVersion=v4.5;Configuration=Release,Platform=AnyCPU /p:OutputPath=.\package\lib\net45
IF ERRORLEVEL 1 GOTO Error
msbuild BB74.Media.Base.csproj /p:TargetFrameworkVersion=v4.7.2;Configuration=Release,Platform=AnyCPU /p:OutputPath=.\package\lib\net472
IF ERRORLEVEL 1 GOTO Error

..\bin\getversion package\lib\net40\BB74.Media.Base.dll BB74.Media.Base._nuspec BB74.Media.Base.nuspec
IF ERRORLEVEL 1 GOTO Error

nuget pack BB74.Media.Base.nuspec -BasePath .\package -properties configuration=Release
IF ERRORLEVEL 1 GOTO Error

goto exit

IF ERRORLEVEL 1 GOTO Error
goto exit
:Error
echo "error"
pause
:exit