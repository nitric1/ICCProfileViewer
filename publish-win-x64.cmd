@echo off
setlocal EnableExtensions DisableDelayedExpansion

if not "%~1"=="" goto usage

set "REPOSITORY_ROOT=%~dp0"
set "APP_PROJECT=%REPOSITORY_ROOT%ICCProfileViewer.App\ICCProfileViewer.App.csproj"
set "LCMS_DLL=%REPOSITORY_ROOT%Artifacts\native\win-x64\Release\lcms2.dll"
set "OUTPUT_DIR=%REPOSITORY_ROOT%Artifacts\publish\win-x64-single-file"
set "OUTPUT_EXE=%OUTPUT_DIR%\ICCProfileViewer.exe"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo ERROR: dotnet was not found on PATH. Install the .NET 10 SDK.
    exit /b 2
)

if not exist "%APP_PROJECT%" (
    echo ERROR: App project was not found at "%APP_PROJECT%".
    exit /b 3
)

if not exist "%LCMS_DLL%" (
    echo ERROR: Little-CMS was not found at "%LCMS_DLL%".
    echo Run build-lcms.cmd Release x64 from a Visual Studio Developer shell first.
    exit /b 4
)

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
if errorlevel 1 (
    echo ERROR: Could not create output directory "%OUTPUT_DIR%".
    exit /b 5
)

if exist "%OUTPUT_DIR%\ThirdPartyNotices" (
    rmdir /s /q "%OUTPUT_DIR%\ThirdPartyNotices"
    if errorlevel 1 (
        echo ERROR: Could not remove the previous generated notice directory.
        exit /b 6
    )
)

for /d %%D in ("%OUTPUT_DIR%\*") do (
    echo ERROR: The output directory contains an unexpected subdirectory: "%%~fD".
    echo Remove it and run this script again.
    exit /b 7
)

del /q "%OUTPUT_DIR%\*" >nul 2>nul

echo Publishing ICC Profile Viewer as a self-contained single file...
dotnet publish "%APP_PROJECT%" ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --property:PublishProfile=WinX64SingleFile ^
    --property:BundleLcmsNativeLibrary=true ^
    --property:LcmsNativeLibraryPath="%LCMS_DLL%" ^
    --property:UsedAvaloniaProducts= ^
    --output "%OUTPUT_DIR%"
set "PUBLISH_EXIT_CODE=%ERRORLEVEL%"
if not "%PUBLISH_EXIT_CODE%"=="0" (
    echo ERROR: dotnet publish failed with exit code %PUBLISH_EXIT_CODE%.
    exit /b %PUBLISH_EXIT_CODE%
)

del /q "%OUTPUT_DIR%\*.pdb" >nul 2>nul

if not exist "%OUTPUT_EXE%" (
    echo ERROR: Publish completed but "%OUTPUT_EXE%" was not found.
    exit /b 8
)

for /d %%D in ("%OUTPUT_DIR%\*") do (
    echo ERROR: Publish produced an unexpected subdirectory: "%%~fD".
    exit /b 9
)

set "PUBLISHED_FILE_COUNT=0"
for /f "delims=" %%F in ('dir /b /a-d "%OUTPUT_DIR%"') do set /a PUBLISHED_FILE_COUNT+=1
if not "%PUBLISHED_FILE_COUNT%"=="1" (
    echo ERROR: Expected one published file, but found %PUBLISHED_FILE_COUNT% in "%OUTPUT_DIR%".
    dir /b /a-d "%OUTPUT_DIR%"
    exit /b 10
)

for %%F in ("%OUTPUT_EXE%") do set "OUTPUT_SIZE=%%~zF"

echo Publish completed successfully.
echo Output: %OUTPUT_EXE%
echo Size:   %OUTPUT_SIZE% bytes
echo.
echo The executable contains .NET, Avalonia native libraries, and lcms2.dll.
echo Native libraries are extracted by the .NET single-file host at run time.
exit /b 0

:usage
echo Usage: %~nx0
exit /b 1
