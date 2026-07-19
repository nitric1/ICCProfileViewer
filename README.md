# ICCProfileViewer

The implementation plan targets a .NET 10 and Avalonia ICC profile metadata and chromaticity diagram viewer.

## Development

Initialize the Little-CMS submodule after cloning:

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

See [Docs/native-build.md](Docs/native-build.md) for prerequisites, outputs, troubleshooting, and the post-MVP macOS/Linux policy.
