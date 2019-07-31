set BB74_CONFIG=Release
set BB74_PUBLISH=default
:parse
IF "%~1"=="" GOTO endparse
IF "%~1"=="debug" set BB74_CONFIG=Debug
if "%~1"=="bert" set BB74_PUBLISH=bert
IF NOT "%~1"=="clean" goto noclean
for %%i in (.\packages\*) do if not "%%i"==".\packages\.gitignore" del %%i
goto exit
:noclean
SHIFT
GOTO parse
:endparse

IF "%BB74_CONFIG%"=="Release" goto release_build
set BB74_VERSION=-debug
goto _release_build
:release_build
set BB74_VERSION=
:_release_build

if NOT "%DevEnvDir%"=="" goto devenvok;
call "c:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build\vcvarsx86_amd64.bat"
if "%DevEnvDir%"=="" goto Error;
:devenvok


if NOT "%BB74_PUBLISH%"=="bert" goto _noinc
..\bin\incversion.exe -inc build "Properties\AssemblyInfo.cs"
IF ERRORLEVEL 1 GOTO Error
:_noinc

msbuild BB74.Media.Base.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=.\package\lib\net40
IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Media.Base.csproj /p:TargetFrameworkVersion=v4.5;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=.\package\lib\net45
rem IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Media.Base.csproj /p:TargetFrameworkVersion=v4.7.2;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=.\package\lib\net472
rem IF ERRORLEVEL 1 GOTO Error

..\bin\getversion -version_ext "%BB74_VERSION%" -assembly package\lib\net40\BB74.Media.Base.dll BB74.Media.Base._nuspec _tmp\BB74.Media.Base.nuspec
IF ERRORLEVEL 1 GOTO Error

nuget pack _tmp/BB74.Media.Base.nuspec -BasePath .\package -OutputDirectory packages\ -properties configuration=%BB74_CONFIG%
IF ERRORLEVEL 1 GOTO Error

if NOT "%BB74_PUBLISH%"=="bert" goto exit

..\bin\getversion -version_ext "%BB74_VERSION%" -assembly package\lib\net40\BB74.Media.Base.dll copy._bat _tmp\copy.bat
IF ERRORLEVEL 1 GOTO Error

call _tmp\copy.bat
IF ERRORLEVEL 1 GOTO Error

goto exit

IF ERRORLEVEL 1 GOTO Error
goto exit
:Error
echo "error"
pause
:exit