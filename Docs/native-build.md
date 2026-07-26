# Building Little CMS for Windows x64

ICC Profile Viewer uses the native Little CMS shared library through `lcmsNET`. The .NET solution does not build this library automatically; run `build-lcms.cmd` separately whenever a local `lcms2.dll` is required.

## Prerequisites

- Windows x64
- Git
- Visual Studio or Visual Studio Build Tools with the `Desktop development with C++` workload
- A Visual Studio Developer Command Prompt or Developer PowerShell

The build script supports the Visual Studio solutions included with the Little CMS submodule:

| Visual Studio | Little CMS solution | Platform toolset |
|---|---|---|
| 2026 | `External/Little-CMS/Projects/VC2026/lcms2.sln` | `v145` |
| 2022 | `External/Little-CMS/Projects/VC2022/lcms2.sln` | `v143` |
| 2019 | `External/Little-CMS/Projects/VC2019/lcms2.sln` | `v142` |

## Initialize the Submodule

Little CMS is included as a Git submodule at `External/Little-CMS`.

From the repository root, initialize the submodule:

```powershell
git submodule update --init --recursive
```

## Build

Run the script from a Developer Command Prompt or Developer PowerShell at the repository root:

```powershell
.\build-lcms.cmd
```

The default build is `Release x64`. To select a configuration explicitly:

```powershell
.\build-lcms.cmd Release x64
.\build-lcms.cmd Debug x64
```

Only `Debug` and `Release` configurations and the `x64` platform are accepted.

The script uses `vswhere.exe` to locate compatible Visual Studio installations with the C++ workload. It selects the newest installed Visual Studio version for which the Little CMS submodule contains a matching solution. It then builds only the `lcms2_DLL` target.

## Output

The script copies the native build output to:

```text
Artifacts/native/win-x64/<Configuration>/
├─ lcms2.dll
├─ lcms2.lib          when generated
├─ lcms2.pdb          when generated
└─ build-info.txt
```

`build-info.txt` records the Little CMS commit, Visual Studio installation, solution, platform toolset, configuration, and platform used for the build. The `Artifacts` directory is excluded from Git.

## Troubleshooting

### The submodule is not initialized

Run:

```powershell
git submodule update --init --recursive
```

### The script must be run from a Visual Studio developer shell

Open a Developer Command Prompt or Developer PowerShell from the installed Visual Studio or Build Tools instance, then run the script again.

### No compatible Visual Studio installation was found

Open Visual Studio Installer and add the `Desktop development with C++` workload to Visual Studio 2019, 2022, or 2026. Start a new developer shell after the installation completes.

### The build succeeded but `lcms2.dll` was not found

Check the MSBuild output and `External/Little-CMS/bin/lcms2.dll`. If a future submodule update changes its solution or output layout, update `build-lcms.cmd` before using it.
