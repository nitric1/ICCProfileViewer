namespace ICCProfileViewer.Lcms;

public sealed record LcmsRuntimeInfo(
    int EncodedVersion,
    string Version,
    string LibrarySource,
    string? LibraryPath);
