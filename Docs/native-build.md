# Preparing the Little-CMS Native Library

This document explains how to prepare the Little-CMS native library used by ICC Profile Viewer. Building the `.slnx` solution does not build Little-CMS automatically.

## 1. Pinned Version

The repository includes [Little-CMS](https://github.com/mm2/Little-CMS) as a Git submodule at `External/Little-CMS`.

- Release tag: `lcms2.19.1`
- Pinned commit: `21c582a594fe5279f90c0b93437c398f93bf62b0`

Initialize the submodule after obtaining the repository:

```powershell
git submodule update --init --recursive
```

Use the following command when cloning a new working copy:

```powershell
git clone --recurse-submodules <repository-url>
```

## 2. Windows 10/11 x64

Windows 10/11 x64 is the officially supported MVP platform.

### 2.1 Prerequisites

- Visual Studio or Visual Studio Build Tools
- The `Desktop development with C++` workload
- A Visual Studio Developer Command Prompt or Developer PowerShell
- An initialized Git submodule

Little-CMS 2.19.1 includes the following Visual Studio solutions:

| Visual Studio | Little-CMS directory | Platform toolset |
|---|---|---|
| Visual Studio 2026 | `Projects/VC2026` | `v145` |
| Visual Studio 2022 | `Projects/VC2022` | `v143` |
| Visual Studio 2019 | `Projects/VC2019` | `v142` |

`build-lcms.cmd` uses `vswhere.exe` to find Visual Studio or Build Tools installations that include the C++ workload. It selects the highest installed version for which Little-CMS provides a matching solution. It does not retarget a solution to a different toolset.

### 2.2 Building

Run the script from the repository root:

```powershell
.\build-lcms.cmd
.\build-lcms.cmd Debug x64
.\build-lcms.cmd Release x64
```

The default is `Release x64`. The MVP does not accept a platform argument other than `x64`.

The script builds only the `lcms2_DLL` target rather than the entire solution. JPEG/TIFF-based utilities and plugins therefore do not introduce additional dependencies.

Successful build outputs are copied to:

```text
Artifacts/native/win-x64/<Configuration>/
├─ lcms2.dll
├─ lcms2.lib          when available
├─ lcms2.pdb          when available
└─ build-info.txt
```

`build-info.txt` records the submodule commit, selected Visual Studio path, solution, toolset, configuration, and platform. The `Artifacts` directory is excluded from Git.

### 2.3 Troubleshooting

For `Little-CMS submodule is not initialized`, run:

```powershell
git submodule update --init --recursive
```

If `vswhere.exe` is missing or the script cannot find a compatible Visual Studio installation:

1. Open Visual Studio Installer.
2. Modify the Visual Studio or Build Tools installation.
3. Install the `Desktop development with C++` workload.
4. Open a new Developer Command Prompt or Developer PowerShell and run the script again.

If the build succeeds but the script cannot find the DLL, inspect `External/Little-CMS/bin/lcms2.dll` and the MSBuild output. If a future pinned submodule revision changes the output location, update both the script and this document.

### 2.4 Using a Separately Installed Library

A source build is optional. A compatible `lcms2.dll` prepared separately can be placed where the application loader can find it. The application implementation will use the following lookup order:

1. The file specified by `ICC_PROFILE_VIEWER_LCMS_PATH`
2. `lcms2.dll` in the application executable directory
3. The default Windows dynamic-library search paths

`lcmsNET` 1.2.1 imports the library as `lcms2`. The application registers a `NativeLibrary.SetDllImportResolver` for the `lcmsNET` assembly and applies the lookup order above before allowing the runtime to use its default search paths.

The Windows integration tests set `ICC_PROFILE_VIEWER_LCMS_PATH` to `Artifacts/native/win-x64/Release/lcms2.dll`. Build that artifact before running:

```powershell
dotnet test ICCProfileViewer.slnx -c Debug
```

The test suite also starts an isolated helper process to verify all Windows lookup branches independently:

1. `ICC_PROFILE_VIEWER_LCMS_PATH`
2. app-local `lcms2.dll`
3. the default Windows loader with a separately supplied `PATH` entry
4. an actionable error for a missing explicitly configured DLL

### 2.5 Visual Studio Launch Profile

`ICCProfileViewer.App/Properties/launchSettings.json` provides the `ICCProfileViewer.App (Local LittleCMS)` profile. Select it next to Visual Studio's Start button when `ICCProfileViewer.App` is the startup project.

The profile uses the repository root as its working directory and injects:

```text
ICC_PROFILE_VIEWER_LCMS_PATH=Artifacts\native\win-x64\Release\lcms2.dll
```

Run `build-lcms.cmd Release x64` first. The launch profile is for local development only and is not included in publish output.

### 2.6 Publishing a Single Executable

After preparing the Release x64 native artifact, run this command from the repository root:

```powershell
.\publish-win-x64.cmd
```

The script uses `ICCProfileViewer.App/Properties/PublishProfiles/WinX64SingleFile.pubxml` to create a self-contained `win-x64` single-file release. It embeds the .NET runtime, Avalonia native dependencies, and `lcms2.dll`, removes publish-only symbol files, and verifies that the output directory contains exactly one file:

```text
Artifacts/publish/win-x64-single-file/ICCProfileViewer.exe
```

The native library must exist at `Artifacts/native/win-x64/Release/lcms2.dll` before publishing. It is included in the executable during the single-file bundling step, so copying it after `dotnet publish` is not equivalent.

At run time, the .NET single-file host extracts bundled native libraries under `%TEMP%/.net`. The application does not require `ICC_PROFILE_VIEWER_LCMS_PATH`, a separately installed .NET runtime, or a separately installed Little-CMS runtime for this release format.

## 3. macOS and Linux

macOS and Linux support is planned after the MVP. Neither platform is officially supported until it has been tested on physical hardware or in CI.

The recommended installation path when implementing support is as follows.

macOS:

```bash
brew install little-cms2
```

On Linux, install the `lcms2` runtime package using the distribution's package manager. Document the actual package name, minimum version, soname, and loader behavior after selecting and testing the supported distributions.

When no suitable package is available, use the upstream CMake, Autotools, or Meson build. According to `BUILDING.md` in Little-CMS 2.19.1, Autotools is fully supported, CMake is supported and recommended for native Windows builds, and Meson support is in testing.

## 4. Version Update Procedure

When updating the Little-CMS submodule, verify all of the following:

1. Little-CMS release notes and security advisories
2. The `Projects/VCyyyy` directory, solution, `lcms2_DLL` target, and output paths
3. `lcmsNET` compatibility and the native version actually loaded at runtime
4. Integration tests using ICC v2 and v4 fixtures
5. Version references in `build-lcms.cmd`, this document, and the open-source notices

Commit the submodule gitlink only after validating the selected release commit. Do not commit compiled DLLs to the repository.
