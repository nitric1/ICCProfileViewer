using System;
using ICCProfileViewer.App.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.App.Tests;

[TestClass]
public sealed class ApplicationDiagnosticLogTests
{
    [TestMethod]
    public void Write_FormatsEntriesAndRaisesChanged()
    {
        var log = new ApplicationDiagnosticLog(
            timeProvider: new FixedTimeProvider(
                new DateTimeOffset(2026, 7, 20, 1, 2, 3, 456, TimeSpan.Zero)));
        var changedCount = 0;
        log.Changed += (_, _) => changedCount++;

        log.Write(
            DiagnosticLogLevel.Error,
            "Profile.LoadFailed",
            "Could not load profile.",
            new InvalidOperationException("Native failure."));

        var report = log.CreateReport();
        Assert.AreEqual(1, changedCount);
        Assert.AreEqual(1, log.Count);
        StringAssert.Contains(report, "2026-07-20 01:02:03.456 UTC");
        StringAssert.Contains(report, "[ERROR] Profile.LoadFailed");
        StringAssert.Contains(report, "System.InvalidOperationException: Native failure.");
    }

    [TestMethod]
    public void Write_WhenCapacityIsReached_RemovesOldestEntry()
    {
        var log = new ApplicationDiagnosticLog(capacity: 2);

        log.Write(DiagnosticLogLevel.Information, "First", "first message");
        log.Write(DiagnosticLogLevel.Warning, "Second", "second message");
        log.Write(DiagnosticLogLevel.Error, "Third", "third message");

        var report = log.CreateReport();
        Assert.AreEqual(2, log.Count);
        Assert.IsFalse(report.Contains("First", StringComparison.Ordinal));
        StringAssert.Contains(report, "[WARN] Second");
        StringAssert.Contains(report, "[ERROR] Third");
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset timestamp;

        public FixedTimeProvider(DateTimeOffset timestamp)
        {
            this.timestamp = timestamp;
        }

        public override DateTimeOffset GetUtcNow() => timestamp;
    }
}
