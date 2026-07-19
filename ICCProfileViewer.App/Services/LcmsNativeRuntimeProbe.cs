using ICCProfileViewer.Lcms;

namespace ICCProfileViewer.App.Services;

public sealed class LcmsNativeRuntimeProbe : INativeRuntimeProbe
{
    public NativeRuntimeStatus Probe()
    {
        if (NativeLibraryBootstrapper.TryInitialize(out var runtimeInfo, out var errorMessage))
        {
            var location = runtimeInfo.LibraryPath ?? runtimeInfo.LibrarySource;
            return new NativeRuntimeStatus(
                true,
                $"Little CMS {runtimeInfo.Version} ready ({location})",
                null);
        }

        return new NativeRuntimeStatus(
            false,
            "Little CMS is not available",
            errorMessage);
    }
}
