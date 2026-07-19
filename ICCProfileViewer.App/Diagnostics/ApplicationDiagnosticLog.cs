using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ICCProfileViewer.App.Diagnostics;

public sealed class ApplicationDiagnosticLog : IApplicationDiagnosticLog
{
    public const int DefaultCapacity = 200;

    private readonly object sync = new();
    private readonly Queue<DiagnosticLogEntry> entries;
    private readonly int capacity;
    private readonly TimeProvider timeProvider;

    public ApplicationDiagnosticLog(
        int capacity = DefaultCapacity,
        TimeProvider? timeProvider = null)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Diagnostic log capacity must be positive.");
        }

        this.capacity = capacity;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        entries = new Queue<DiagnosticLogEntry>(capacity);
    }

    public event EventHandler? Changed;

    public int Count
    {
        get
        {
            lock (sync)
            {
                return entries.Count;
            }
        }
    }

    public void Write(
        DiagnosticLogLevel level,
        string eventName,
        string message,
        Exception? exception = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var entry = new DiagnosticLogEntry(
            timeProvider.GetUtcNow(),
            level,
            eventName,
            message,
            exception?.ToString());

        lock (sync)
        {
            while (entries.Count >= capacity)
            {
                entries.Dequeue();
            }

            entries.Enqueue(entry);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public string CreateReport()
    {
        DiagnosticLogEntry[] snapshot;
        lock (sync)
        {
            snapshot = entries.ToArray();
        }

        var report = new StringBuilder();
        foreach (var entry in snapshot)
        {
            report
                .Append(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff 'UTC'", CultureInfo.InvariantCulture))
                .Append(" [")
                .Append(FormatLevel(entry.Level))
                .Append("] ")
                .Append(entry.EventName)
                .Append(" - ")
                .AppendLine(entry.Message);

            if (entry.ExceptionDetails is not null)
            {
                report.AppendLine(entry.ExceptionDetails);
            }
        }

        return report.ToString().TrimEnd();
    }

    private static string FormatLevel(DiagnosticLogLevel level) => level switch
    {
        DiagnosticLogLevel.Information => "INFO",
        DiagnosticLogLevel.Warning => "WARN",
        DiagnosticLogLevel.Error => "ERROR",
        _ => level.ToString().ToUpperInvariant(),
    };
}
