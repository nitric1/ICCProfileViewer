using System;

namespace ICCProfileViewer.App.Diagnostics;

public interface IApplicationDiagnosticLog
{
    event EventHandler? Changed;

    int Count { get; }

    void Write(
        DiagnosticLogLevel level,
        string eventName,
        string message,
        Exception? exception = null);

    string CreateReport();
}
