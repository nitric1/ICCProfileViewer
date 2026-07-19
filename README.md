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

Run the Avalonia application shell:

```powershell
$env:ICC_PROFILE_VIEWER_LCMS_PATH = "$PWD\Artifacts\native\win-x64\Release\lcms2.dll"
dotnet run --project ICCProfileViewer.App
```

The shell still starts when Little-CMS is unavailable and displays a native-dependency diagnostic. Profile selection and diagram rendering are subsequent implementation steps.

See [Docs/native-build.md](Docs/native-build.md) for prerequisites, outputs, troubleshooting, and the post-MVP macOS/Linux policy.
