# ICCProfileViewer

The implementation plan targets a .NET 10 and Avalonia ICC profile metadata and chromaticity diagram viewer.

## Prerequisites

The MVP is officially supported and tested only on Windows 10/11 x64. The macOS and Linux prerequisites below are provided for source development; those platforms are not yet officially supported or tested by this project.

All platforms require:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Windows x64

- Visual Studio or Visual Studio Build Tools with the `Desktop development with C++` workload
- A Visual Studio Developer Command Prompt or Developer PowerShell

See [Docs/native-build-windows-x64.md](Docs/native-build-windows-x64.md) for native build instructions, toolchain selection, outputs, and troubleshooting.

### macOS

Install Little CMS with [Homebrew](https://brew.sh/):

```bash
brew install little-cms2
export ICC_PROFILE_VIEWER_LCMS_PATH="$(brew --prefix little-cms2)/lib/liblcms2.dylib"
```

Avalonia uses its own macOS backend, so the .NET macOS workload is not required for this project.

### Linux

Install the Little CMS development package so the unversioned `liblcms2.so` name used by `lcmsNET` is available:

```bash
# Debian / Ubuntu
sudo apt install liblcms2-dev

# Fedora
sudo dnf install lcms2-devel

# Arch Linux
sudo pacman -S lcms2
```

A graphical X11 environment and the distribution-specific [Avalonia native dependencies](https://docs.avaloniaui.net/docs/supported-platforms) are also required. If Little CMS is not found through the system loader, set `ICC_PROFILE_VIEWER_LCMS_PATH` to the full path of the installed `liblcms2.so` or `liblcms2.so.2`.

## Development

For a Git checkout, initialize the Little-CMS submodule when building the bundled Windows native library:

```powershell
git submodule update --init --recursive
```

On Windows x64, build the native library from a Visual Studio Developer Command Prompt or Developer PowerShell:

```powershell
.\build-lcms.cmd Release x64
```

Restore, build, and run the MSTest suite:

```powershell
dotnet restore ICCProfileViewer.slnx
dotnet build ICCProfileViewer.slnx -c Debug --no-restore
dotnet test ICCProfileViewer.slnx -c Debug --no-build
```

The Little-CMS integration tests use `Artifacts/native/win-x64/Release/lcms2.dll`, so run the native build command before the test command.

Run the Avalonia application:

```powershell
$env:ICC_PROFILE_VIEWER_LCMS_PATH = "$PWD\Artifacts\native\win-x64\Release\lcms2.dll"
dotnet run --project ICCProfileViewer.App
```

When `ICCProfileViewer.App` is selected as the Visual Studio startup project, choose the `ICCProfileViewer.App (Local LittleCMS)` launch profile. It sets the working directory to the repository root and injects `ICC_PROFILE_VIEWER_LCMS_PATH=Artifacts\native\win-x64\Release\lcms2.dll` automatically.

Use **Open Profile** to select an `.icc` or `.icm` file, or drag one profile file onto the application window. The application reads ICC v2/v4 metadata and displays the profile summary and tag table without requiring a temporary file. It remains open when Little-CMS is unavailable and displays a native-dependency diagnostic instead.

Expand **Diagnostics** at the bottom of the window to inspect and copy the bounded in-memory log. It records the runtime environment, resolved Little-CMS version and location, profile load results, cancellations, and exception details without writing log files to disk.

For supported RGB Matrix/TRC profiles, the application displays CIE 1931 `xy` and CIE 1976 `u'v'` chromaticity diagrams. Use the legend controls to independently compare the profile with sRGB, Display P3, DCI-P3 (DCI white), Adobe RGB (1998), and BT.2020. Hover over a diagram to inspect coordinates and double-click to copy the current coordinate. Profiles without a supported gamut remain available for metadata inspection and are not shown as an inaccurate triangle.

## Windows x64 Single-File Release

Build Little-CMS and publish the Windows x64 release:

```powershell
.\build-lcms.cmd Release x64
.\publish-win-x64.cmd
```

The publish script creates one self-contained executable:

```text
Artifacts/publish/win-x64-single-file/ICCProfileViewer.exe
```

The executable includes the .NET runtime, Avalonia native dependencies, and `lcms2.dll`. No separately installed .NET or Little-CMS runtime is required. The .NET single-file host extracts bundled native libraries under the user's temporary directory at run time.

Use **Third-party notices** in the main window to view the license and attribution information embedded in the executable.
