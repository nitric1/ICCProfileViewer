namespace ICCProfileViewer.App.Services;

public sealed record NativeRuntimeStatus(
    bool IsAvailable,
    string Summary,
    string? DiagnosticMessage);
