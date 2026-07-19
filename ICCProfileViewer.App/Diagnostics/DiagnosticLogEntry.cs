using System;

namespace ICCProfileViewer.App.Diagnostics;

public sealed record DiagnosticLogEntry(
    DateTimeOffset Timestamp,
    DiagnosticLogLevel Level,
    string EventName,
    string Message,
    string? ExceptionDetails);
