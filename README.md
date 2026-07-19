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

See [Docs/native-build.md](Docs/native-build.md) for prerequisites, outputs, troubleshooting, and the post-MVP macOS/Linux policy.
