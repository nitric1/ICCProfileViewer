using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using lcmsNET;

namespace ICCProfileViewer.Lcms;

public static class NativeLibraryBootstrapper
{
    public const string LibraryPathEnvironmentVariable = "ICC_PROFILE_VIEWER_LCMS_PATH";

    private const string ImportName = "lcms2";
    private const int MinimumEncodedVersion = 2090;
    private static readonly object SyncRoot = new();
    private static bool resolverRegistered;
    private static nint loadedLibraryHandle;
    private static string? resolvedLibraryPath;
    private static string librarySource = "OperatingSystem";

    public static LcmsRuntimeInfo Initialize()
    {
        RegisterResolver();

        try
        {
            var encodedVersion = Cms.EncodedCMMVersion;
            if (encodedVersion < MinimumEncodedVersion)
            {
                throw new LcmsNativeLibraryException(
                    $"Little CMS 2.9 or later is required, but version {FormatVersion(encodedVersion)} was loaded.");
            }

            return new LcmsRuntimeInfo(
                encodedVersion,
                FormatVersion(encodedVersion),
                librarySource,
                resolvedLibraryPath);
        }
        catch (LcmsNativeLibraryException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is DllNotFoundException or
            BadImageFormatException or
            EntryPointNotFoundException)
        {
            throw new LcmsNativeLibraryException(CreateLoadFailureMessage(), exception);
        }
    }

    public static bool TryInitialize(
        [NotNullWhen(true)] out LcmsRuntimeInfo? runtimeInfo,
        [NotNullWhen(false)] out string? errorMessage)
    {
        try
        {
            runtimeInfo = Initialize();
            errorMessage = null;
            return true;
        }
        catch (LcmsNativeLibraryException exception)
        {
            runtimeInfo = null;
            errorMessage = exception.Message;
            return false;
        }
    }

    private static void RegisterResolver()
    {
        lock (SyncRoot)
        {
            if (resolverRegistered)
            {
                return;
            }

            try
            {
                NativeLibrary.SetDllImportResolver(typeof(Cms).Assembly, ResolveImport);
                resolverRegistered = true;
            }
            catch (InvalidOperationException exception)
            {
                throw new LcmsNativeLibraryException(
                    "The native-library resolver for lcmsNET was already configured by another component.",
                    exception);
            }
        }
    }

    private static nint ResolveImport(
        string libraryName,
        Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, ImportName, StringComparison.Ordinal))
        {
            return nint.Zero;
        }

        var configuredPath = Environment.GetEnvironmentVariable(LibraryPathEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return LoadFromPath(configuredPath, "ExplicitPath");
        }

        if (OperatingSystem.IsWindows())
        {
            var appLocalPath = Path.Combine(AppContext.BaseDirectory, "lcms2.dll");
            if (File.Exists(appLocalPath))
            {
                return LoadFromPath(appLocalPath, "AppLocal");
            }
        }

        librarySource = "OperatingSystem";
        resolvedLibraryPath = null;
        return nint.Zero;
    }

    private static nint LoadFromPath(string configuredPath, string source)
    {
        var fullPath = Path.GetFullPath(configuredPath);
        if (!File.Exists(fullPath))
        {
            throw new DllNotFoundException(
                $"The Little CMS library configured by {LibraryPathEnvironmentVariable} does not exist: {fullPath}");
        }

        lock (SyncRoot)
        {
            if (loadedLibraryHandle != nint.Zero)
            {
                if (!string.Equals(resolvedLibraryPath, fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Little CMS is already loaded from '{resolvedLibraryPath}' and cannot be switched to '{fullPath}' in the same process.");
                }

                return loadedLibraryHandle;
            }

            loadedLibraryHandle = NativeLibrary.Load(fullPath);
            librarySource = source;
            resolvedLibraryPath = fullPath;
            return loadedLibraryHandle;
        }
    }

    private static string CreateLoadFailureMessage()
    {
        var configuredPath = Environment.GetEnvironmentVariable(LibraryPathEnvironmentVariable);
        var configuredPathDetails = string.IsNullOrWhiteSpace(configuredPath)
            ? "The environment variable is not set."
            : $"The environment variable currently points to '{configuredPath}'.";

        return $"Could not load Little CMS 2 for the current process. " +
            $"Set {LibraryPathEnvironmentVariable} to a compatible lcms2.dll, copy lcms2.dll next to the application, " +
            $"or install it in the operating system's library search path. {configuredPathDetails} " +
            "Repository builds can create the Windows x64 library by running build-lcms.cmd from a Visual Studio Developer shell; " +
            "see Docs/native-build-windows-x64.md.";
    }

    private static string FormatVersion(int encodedVersion)
    {
        var major = encodedVersion / 1000;
        var minor = encodedVersion / 10 % 100;
        var patch = encodedVersion % 10;
        return patch == 0 ? $"{major}.{minor}" : $"{major}.{minor}.{patch}";
    }
}
