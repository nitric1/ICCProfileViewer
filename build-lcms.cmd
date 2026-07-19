@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "CONFIGURATION=%~1"
set "PLATFORM=%~2"

if not "%~3"=="" goto usage
if not defined CONFIGURATION set "CONFIGURATION=Release"
if not defined PLATFORM set "PLATFORM=x64"

if /i "%CONFIGURATION%"=="Debug" set "CONFIGURATION=Debug"
if /i "%CONFIGURATION%"=="Release" set "CONFIGURATION=Release"
if not "%CONFIGURATION%"=="Debug" if not "%CONFIGURATION%"=="Release" goto usage

if /i "%PLATFORM%"=="x64" set "PLATFORM=x64"
if not "%PLATFORM%"=="x64" goto usage

if not defined VSINSTALLDIR (
    echo ERROR: Run this script from a Visual Studio Developer Command Prompt or Developer PowerShell.
    exit /b 2
)

set "REPOSITORY_ROOT=%~dp0"
set "LCMS_ROOT=%REPOSITORY_ROOT%External\Little-CMS"
set "OUTPUT_DIR=%REPOSITORY_ROOT%Artifacts\native\win-x64\%CONFIGURATION%"

if not exist "%LCMS_ROOT%\Projects" (
    echo ERROR: Little-CMS submodule is not initialized at "%LCMS_ROOT%".
    echo Run: git submodule update --init --recursive
    exit /b 3
)

set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
    echo ERROR: vswhere.exe was not found at "%VSWHERE%".
    echo Install Visual Studio or Visual Studio Build Tools with the Desktop development with C++ workload.
    exit /b 4
)

set "VS_PATH="
set "VC_DIRECTORY="
set "PLATFORM_TOOLSET="

call :select_visual_studio 18.0 19.0 VC2026 v145
if defined VS_PATH goto visual_studio_selected
call :select_visual_studio 17.0 18.0 VC2022 v143
if defined VS_PATH goto visual_studio_selected
call :select_visual_studio 16.0 17.0 VC2019 v142
if defined VS_PATH goto visual_studio_selected

echo ERROR: No compatible Visual Studio C++ installation and Little-CMS solution pair was found.
echo Checked: VC2026, VC2022, and VC2019.
exit /b 5

:visual_studio_selected
set "MSBUILD=%VS_PATH%\MSBuild\Current\Bin\MSBuild.exe"
set "SOLUTION=%LCMS_ROOT%\Projects\%VC_DIRECTORY%\lcms2.sln"

if not exist "%MSBUILD%" (
    echo ERROR: MSBuild was not found at "%MSBUILD%".
    exit /b 6
)

if not exist "%SOLUTION%" (
    echo ERROR: Little-CMS solution was not found at "%SOLUTION%".
    exit /b 7
)

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
if errorlevel 1 (
    echo ERROR: Could not create output directory "%OUTPUT_DIR%".
    exit /b 8
)

echo Building Little-CMS lcms2_DLL...
echo   Visual Studio: %VS_PATH%
echo   Solution:      %SOLUTION%
echo   Toolset:       %PLATFORM_TOOLSET%
echo   Configuration: %CONFIGURATION%
echo   Platform:      %PLATFORM%

"%MSBUILD%" "%SOLUTION%" /nologo /m /t:lcms2_DLL /p:Configuration=%CONFIGURATION% /p:Platform=%PLATFORM%
set "MSBUILD_EXIT_CODE=%ERRORLEVEL%"
if not "%MSBUILD_EXIT_CODE%"=="0" (
    echo ERROR: Little-CMS build failed with exit code %MSBUILD_EXIT_CODE%.
    exit /b %MSBUILD_EXIT_CODE%
)

set "BUILT_DLL=%LCMS_ROOT%\bin\lcms2.dll"
if not exist "%BUILT_DLL%" (
    echo ERROR: Build succeeded but "%BUILT_DLL%" was not found.
    exit /b 10
)

copy /y "%BUILT_DLL%" "%OUTPUT_DIR%\lcms2.dll" >nul
if errorlevel 1 (
    echo ERROR: Could not copy lcms2.dll to "%OUTPUT_DIR%".
    exit /b 11
)

for %%F in (lcms2.lib lcms2.pdb) do if exist "%LCMS_ROOT%\bin\%%F" copy /y "%LCMS_ROOT%\bin\%%F" "%OUTPUT_DIR%\%%F" >nul

set "LCMS_REVISION=unknown"
for /f "delims=" %%I in ('git -C "%LCMS_ROOT%" rev-parse HEAD 2^>nul') do set "LCMS_REVISION=%%I"

> "%OUTPUT_DIR%\build-info.txt" (
    echo Little-CMS revision: %LCMS_REVISION%
    echo Visual Studio path: %VS_PATH%
    echo Little-CMS solution: %SOLUTION%
    echo Platform toolset: %PLATFORM_TOOLSET%
    echo Configuration: %CONFIGURATION%
    echo Platform: %PLATFORM%
)

echo Build completed successfully.
echo Output: %OUTPUT_DIR%
exit /b 0

:select_visual_studio
set "CANDIDATE_DIRECTORY=%~3"
set "CANDIDATE_SOLUTION=%LCMS_ROOT%\Projects\%CANDIDATE_DIRECTORY%\lcms2.sln"
if not exist "%CANDIDATE_SOLUTION%" exit /b 0

set "CANDIDATE_PATH="
for /f "delims=" %%I in ('call "%VSWHERE%" -latest -prerelease -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -version "[%~1,%~2)" -property installationPath') do set "CANDIDATE_PATH=%%I"
if not defined CANDIDATE_PATH exit /b 0

set "VS_PATH=%CANDIDATE_PATH%"
set "VC_DIRECTORY=%CANDIDATE_DIRECTORY%"
set "PLATFORM_TOOLSET=%~4"
exit /b 0

:usage
echo Usage: %~nx0 [Debug^|Release] [x64]
echo Defaults: Release x64
exit /b 1
