set BB74_CONFIG=Release
set BB74_PUBLISH=default

:parse
IF "%~1"=="" GOTO endparse
IF "%~1"=="debug" set BB74_CONFIG=Debug
IF "%~1"=="release" set BB74_CONFIG=Release
IF "%~1"=="bert" set BB74_PUBLISH=bert
SHIFT
GOTO parse
:endparse

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

cd BB74.Xwt.OpenTK

rem del /s /q .\bin\%BB74_CONFIG%\*.*
rem IF ERRORLEVEL 1 GOTO Error

msbuild BB74.Xwt.OpenTK.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=.\package\lib\net40
IF ERRORLEVEL 1 GOTO Error

..\..\bin\getversion -version_ext "%BB74_VERSION%" -assembly .\package\lib\net40\BB74.Xwt.OpenTK.dll BB74.Xwt.OpenTK._nuspec BB74.Xwt.OpenTK.nuspec
IF ERRORLEVEL 1 GOTO Error

nuget pack BB74.Xwt.OpenTK.nuspec -BasePath .\package -build -properties configuration=%BB74_CONFIG%;Platform=AnyCPU -OutputDirectory ..\packages\
IF ERRORLEVEL 1 GOTO Error


if NOT "%BB74_PUBLISH%"=="bert" goto skip1

..\..\bin\getversion -version_ext "%BB74_VERSION%" -assembly .\package\lib\net40\BB74.Xwt.OpenTK.dll ..\copy._bat ..\_tmp\copy.bat
IF ERRORLEVEL 1 GOTO Error

call ..\_tmp\copy.bat BB74.Xwt.OpenTK

IF ERRORLEVEL 1 GOTO Error
:skip1
cd..

cd BB74.Xwt.OpenTK.GTK

del /s /q .\package\*.*
IF ERRORLEVEL 1 GOTO Error

msbuild BB74.Xwt.OpenTK.GTK.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=.\package\lib\net40
IF ERRORLEVEL 1 GOTO Error

..\..\bin\getversion -version_ext "%BB74_VERSION%" -assembly .\package\lib\net40\BB74.Xwt.OpenTK.GTK.dll BB74.Xwt.OpenTK.GTK._nuspec BB74.Xwt.OpenTK.GTK.nuspec
IF ERRORLEVEL 1 GOTO Error

nuget pack BB74.Xwt.OpenTK.GTK.nuspec -BasePath .\package  -properties configuration=%BB74_CONFIG%;Platform=AnyCPU -OutputDirectory ..\packages\
IF ERRORLEVEL 1 GOTO Error


if NOT "%BB74_PUBLISH%"=="bert" goto skip2

..\..\bin\getversion  -version_ext "%BB74_VERSION%" -assembly .\package\lib\net40\BB74.Xwt.OpenTK.GTK.dll ..\copy._bat ..\_tmp\copy2.bat
IF ERRORLEVEL 1 GOTO Error

call ..\_tmp\copy2.bat BB74.Xwt.OpenTK.GTK
IF ERRORLEVEL 1 GOTO Error
:skip2
cd..


cd BB74.Xwt.OpenTK.WPF

del /s /q .\package\*.*
IF ERRORLEVEL 1 GOTO Error

msbuild BB74.Xwt.OpenTK.WPF.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=.\package\lib\net40
IF ERRORLEVEL 1 GOTO Error

..\..\bin\getversion -version_ext "%BB74_VERSION%" -assembly .\package\lib\net40\BB74.Xwt.OpenTK.WPF.dll BB74.Xwt.OpenTK.WPF._nuspec BB74.Xwt.OpenTK.WPF.nuspec
IF ERRORLEVEL 1 GOTO Error

nuget pack BB74.Xwt.OpenTK.WPF.nuspec -BasePath .\package  -properties configuration=%BB74_CONFIG%;Platform=AnyCPU -OutputDirectory ..\packages\
IF ERRORLEVEL 1 GOTO Error


if NOT "%BB74_PUBLISH%"=="bert" goto skip3

..\..\bin\getversion  -version_ext "%BB74_VERSION%" -assembly .\package\lib\net40\BB74.Xwt.OpenTK.WPF.dll ..\copy._bat ..\_tmp\copy3.bat
IF ERRORLEVEL 1 GOTO Error

call ..\_tmp\copy3.bat BB74.Xwt.OpenTK.WPF
IF ERRORLEVEL 1 GOTO Error
:skip3
cd..


goto exit

goto Exit
:Error
pause

:Exit
set BB74_CONFIG=
set BB74_VERSION=