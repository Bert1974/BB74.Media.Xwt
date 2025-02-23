set BB74_CONFIG=Release
set BB74_PUBLISH=default

:parse
IF "%~1"=="" GOTO endparse
IF "%~1"=="debug" set BB74_CONFIG=Debug
IF "%~1"=="release" set BB74_CONFIG=Release
IF "%~1"=="bert" set BB74_PUBLISH=bert
IF NOT "%~1"=="clean" goto noclean
for %%i in (.\packages\*) do if not "%%i"==".\packages\.gitignore" del %%i
msbuild BB74.Xwt.OpenTK\BB74.Xwt.OpenTK.csproj /t:Clean /p:Configuration=Debug,Platform=AnyCPU
msbuild BB74.Xwt.OpenTK\BB74.Xwt.OpenTK.csproj /t:Clean	/p:Configuration=Release,Platform=AnyCPU
msbuild BB74.Xwt.OpenTK.GTK\BB74.Xwt.OpenTK.GTK.csproj /t:Clean /p:Configuration=Debug,Platform=AnyCPU
msbuild BB74.Xwt.OpenTK.GTK\BB74.Xwt.OpenTK.GTK.csproj /t:Clean /p:Configuration=Release,Platform=AnyCPU
msbuild BB74.Xwt.OpenTK.WPF\BB74.Xwt.OpenTK.WPF.csproj /t:Clean /p:Configuration=Debug,Platform=AnyCPU
msbuild BB74.Xwt.OpenTK.WPF\BB74.Xwt.OpenTK.WPF.csproj /t:Clean	/p:Configuration=Release,Platform=AnyCPU
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

if NOT "%DevEnvDir%"=="" goto devenvok
call "c:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build\vcvarsx86_amd64.bat"
if "%DevEnvDir%"=="" goto Error
:devenvok
	
if NOT "%BB74_PUBLISH%"=="bert" goto skip0
..\bin\incversion BB74.Xwt.OpenTK\Properties\AssemblyInfo.cs
IF ERRORLEVEL 1 GOTO Error
..\bin\incversion BB74.Xwt.OpenTK.GTK\Properties\AssemblyInfo.cs 
IF ERRORLEVEL 1 GOTO Error
..\bin\incversion BB74.Xwt.OpenTK.WPF\Properties\AssemblyInfo.cs 
IF ERRORLEVEL 1 GOTO Error
:skip0

msbuild ..\BB74.OpenTK.Xwt.sln /t:BB74_Media_OpenTK\BB74_Xwt_OpenTK:Rebuild;BB74_Media_OpenTK\BB74_Xwt_OpenTK_GTK:Rebuild;BB74_Media_OpenTK\BB74_Xwt_OpenTK_WPF:Rebuild /p:ProjectReferences=false /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=x86
IF ERRORLEVEL 1 GOTO Error

cd BB74.Xwt.OpenTK

copy bin\%BB74_CONFIG%\BB74.Xwt.OpenTK.* .\package\lib\net40\
IF ERRORLEVEL 1 GOTO Error

rem del /s /q .\bin\%BB74_CONFIG%\*.*
rem IF ERRORLEVEL 1 GOTO Error

rem ..\..\bin\incversion

rem msbuild BB74.Xwt.OpenTK.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=.\package\lib\net40
rem IF ERRORLEVEL 1 GOTO Error

..\..\bin\getversion -version_ext "%BB74_VERSION%" -assembly .\package\lib\net40\BB74.Xwt.OpenTK.dll BB74.Xwt.OpenTK._nuspec BB74.Xwt.OpenTK.nuspec
IF ERRORLEVEL 1 GOTO Error

nuget pack BB74.Xwt.OpenTK.nuspec -BasePath .\package -build -OutputDirectory ..\packages\ -properties configuration=%BB74_CONFIG%;Platform=AnyCPU
IF ERRORLEVEL 1 GOTO Error


if NOT "%BB74_PUBLISH%"=="bert" goto skip1

..\..\bin\getversion -version_ext "%BB74_VERSION%" -assembly .\package\lib\net40\BB74.Xwt.OpenTK.dll ..\copy._bat ..\_tmp\copy.bat
IF ERRORLEVEL 1 GOTO Error

call ..\_tmp\copy.bat BB74.Xwt.OpenTK

IF ERRORLEVEL 1 GOTO Error
:skip1
cd..

cd BB74.Xwt.OpenTK.GTK
copy bin\%BB74_CONFIG%\BB74.Xwt.OpenTK.GTK.* .\package\lib\net40\
IF ERRORLEVEL 1 GOTO Error

rem del /s /q .\package\*.*
rem IF ERRORLEVEL 1 GOTO Error

rem ..\..\bin\incversion

rem msbuild BB74.Xwt.OpenTK.GTK.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=.\package\lib\net40
rem IF ERRORLEVEL 1 GOTO Error

..\..\bin\getversion -version_ext "%BB74_VERSION%" -assembly .\package\lib\net40\BB74.Xwt.OpenTK.GTK.dll BB74.Xwt.OpenTK.GTK._nuspec BB74.Xwt.OpenTK.GTK.nuspec
IF ERRORLEVEL 1 GOTO Error

nuget pack BB74.Xwt.OpenTK.GTK.nuspec -BasePath .\package -OutputDirectory ..\packages\  -properties configuration=%BB74_CONFIG%;Platform=AnyCPU
IF ERRORLEVEL 1 GOTO Error


if NOT "%BB74_PUBLISH%"=="bert" goto skip2

..\..\bin\getversion  -version_ext "%BB74_VERSION%" -assembly .\package\lib\net40\BB74.Xwt.OpenTK.GTK.dll ..\copy._bat ..\_tmp\copy2.bat
IF ERRORLEVEL 1 GOTO Error

call ..\_tmp\copy2.bat BB74.Xwt.OpenTK.GTK
IF ERRORLEVEL 1 GOTO Error
:skip2
cd..


cd BB74.Xwt.OpenTK.WPF
copy bin\%BB74_CONFIG%\BB74.Xwt.OpenTK.WPF.* .\package\lib\net40\
IF ERRORLEVEL 1 GOTO Error

rem del /s /q .\package\*.*
rem IF ERRORLEVEL 1 GOTO Error

rem ..\..\bin\incversion

rem msbuild BB74.Xwt.OpenTK.WPF.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=.\package\lib\net40
rem IF ERRORLEVEL 1 GOTO Error

..\..\bin\getversion -version_ext "%BB74_VERSION%" -assembly .\package\lib\net40\BB74.Xwt.OpenTK.WPF.dll BB74.Xwt.OpenTK.WPF._nuspec BB74.Xwt.OpenTK.WPF.nuspec
IF ERRORLEVEL 1 GOTO Error

nuget pack BB74.Xwt.OpenTK.WPF.nuspec -BasePath .\package -OutputDirectory ..\packages\ -properties configuration=%BB74_CONFIG%;Platform=AnyCPU
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