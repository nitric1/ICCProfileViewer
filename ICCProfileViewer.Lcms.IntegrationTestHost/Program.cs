using System;
using System.Text.Json;
using ICCProfileViewer.Lcms;

var success = NativeLibraryBootstrapper.TryInitialize(
    out var runtimeInfo,
    out var errorMessage);

Console.WriteLine(JsonSerializer.Serialize(new
{
    Success = success,
    runtimeInfo?.EncodedVersion,
    runtimeInfo?.Version,
    runtimeInfo?.LibrarySource,
    runtimeInfo?.LibraryPath,
    ErrorMessage = errorMessage,
}));

return success ? 0 : 2;
