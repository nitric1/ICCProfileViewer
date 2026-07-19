using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICCProfileViewer.App.Services;
using ICCProfileViewer.App.ViewModels;
using ICCProfileViewer.Core.Colorimetry;
using ICCProfileViewer.Core.Profiles;
using ICCProfileViewer.Lcms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.App.Tests;

[TestClass]
public sealed class MainWindowViewModelTests
{
    [TestMethod]
    public void Constructor_WithAvailableRuntime_StartsInEmptyState()
    {
        using var viewModel = CreateViewModel(
            new NativeRuntimeStatus(true, "Little CMS 2.19 ready", null));

        Assert.AreEqual(ApplicationViewState.Empty, viewModel.State);
        Assert.IsTrue(viewModel.IsIccEngineAvailable);
        Assert.IsTrue(viewModel.CanOpenProfile);
        Assert.AreEqual("Little CMS 2.19 ready", viewModel.NativeRuntimeSummary);
        Assert.IsFalse(viewModel.HasDiagnosticMessage);
        Assert.IsTrue(viewModel.ShowSrgb);
        Assert.IsTrue(viewModel.ShowWhitePoints);
    }

    [TestMethod]
    public void Constructor_WithMissingRuntime_ReportsNativeDependencyError()
    {
        using var viewModel = CreateViewModel(
            new NativeRuntimeStatus(false, "Little CMS is not available", "Copy lcms2.dll."));

        Assert.AreEqual(ApplicationViewState.NativeDependencyError, viewModel.State);
        Assert.IsFalse(viewModel.IsIccEngineAvailable);
        Assert.IsFalse(viewModel.CanOpenProfile);
        Assert.IsTrue(viewModel.HasDiagnosticMessage);
        Assert.AreEqual("Copy lcms2.dll.", viewModel.DiagnosticMessage);
        StringAssert.Contains(viewModel.StatusMessage, "disabled");
    }

    [TestMethod]
    public async Task LoadProfileAsync_WithValidProfile_DisplaysMetadataAndTags()
    {
        var profile = CreateProfile("sample.icc");
        using var viewModel = CreateViewModel(
            new NativeRuntimeStatus(true, "ready", null),
            (_, _, _) => Task.FromResult(profile));

        await viewModel.LoadProfileAsync(new StubProfileFileSource("sample.icc"));

        Assert.AreEqual(ApplicationViewState.Loaded, viewModel.State);
        Assert.AreEqual("sample.icc", viewModel.ProfileName);
        Assert.AreEqual("4.3", viewModel.ProfileVersion);
        Assert.AreEqual("Display", viewModel.ProfileClass);
        Assert.AreEqual("RGB", viewModel.DataColorSpace);
        Assert.AreEqual("XYZ", viewModel.ProfileConnectionSpace);
        Assert.AreEqual("Perceptual", viewModel.RenderingIntent);
        Assert.AreEqual("Example profile", viewModel.Description);
        Assert.AreEqual("Matrix/TRC", viewModel.ProfileStructure);
        Assert.AreEqual("1 tag", viewModel.TagSummary);
        Assert.HasCount(1, viewModel.Tags);
        Assert.IsFalse(viewModel.HasDiagnosticMessage);
        Assert.IsTrue(viewModel.CanOpenProfile);
    }

    [TestMethod]
    public async Task LoadProfileAsync_WithInvalidProfile_ReportsInvalidProfileState()
    {
        var readException = new LcmsProfileReadException(
            "bad.icc",
            Array.Empty<LcmsError>(),
            new InvalidDataException("The ICC signature is missing."));
        using var viewModel = CreateViewModel(
            new NativeRuntimeStatus(true, "ready", null),
            (_, _, _) => Task.FromException<IccProfileInfo>(readException));

        await viewModel.LoadProfileAsync(new StubProfileFileSource("bad.icc"));

        Assert.AreEqual(ApplicationViewState.InvalidProfile, viewModel.State);
        StringAssert.Contains(viewModel.StatusMessage, "not a valid");
        StringAssert.Contains(viewModel.DiagnosticMessage, "ICC signature is missing");
        Assert.AreEqual("No profile loaded", viewModel.ProfileName);
    }

    [TestMethod]
    public async Task LoadProfileAsync_WhenSecondLoadStarts_CancelsAndIgnoresFirstLoad()
    {
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var viewModel = CreateViewModel(
            new NativeRuntimeStatus(true, "ready", null),
            async (_, displayName, cancellationToken) =>
            {
                if (displayName == "first.icc")
                {
                    firstStarted.SetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }

                return CreateProfile(displayName);
            });

        var firstLoad = viewModel.LoadProfileAsync(new StubProfileFileSource("first.icc"));
        await firstStarted.Task;
        await viewModel.LoadProfileAsync(new StubProfileFileSource("second.icc"));
        await firstLoad;

        Assert.AreEqual(ApplicationViewState.Loaded, viewModel.State);
        Assert.AreEqual("second.icc", viewModel.ProfileName);
        Assert.IsTrue(viewModel.CanOpenProfile);
    }

    [TestMethod]
    public async Task LoadProfileAsync_WhenStaleLoadFails_DoesNotReplaceNewerProfile()
    {
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstCompletion = new TaskCompletionSource<IccProfileInfo>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var viewModel = CreateViewModel(
            new NativeRuntimeStatus(true, "ready", null),
            (_, displayName, _) =>
            {
                if (displayName == "first.icc")
                {
                    firstStarted.SetResult();
                    return firstCompletion.Task;
                }

                return Task.FromResult(CreateProfile(displayName));
            });

        var firstLoad = viewModel.LoadProfileAsync(new StubProfileFileSource("first.icc"));
        await firstStarted.Task;
        await viewModel.LoadProfileAsync(new StubProfileFileSource("second.icc"));
        firstCompletion.SetException(new IOException("The stale read failed."));
        await firstLoad;

        Assert.AreEqual(ApplicationViewState.Loaded, viewModel.State);
        Assert.AreEqual("second.icc", viewModel.ProfileName);
        Assert.IsFalse(viewModel.HasDiagnosticMessage);
    }

    [TestMethod]
    public void ReportFilePickerError_ReportsUnexpectedErrorState()
    {
        using var viewModel = CreateViewModel(
            new NativeRuntimeStatus(true, "ready", null));

        viewModel.ReportFilePickerError(new IOException("The picker failed."));

        Assert.AreEqual(ApplicationViewState.UnexpectedError, viewModel.State);
        Assert.AreEqual("The picker failed.", viewModel.DiagnosticMessage);
        StringAssert.Contains(viewModel.StatusMessage, "file picker");
    }

    [TestMethod]
    public void OverlayProperties_RaisePropertyChangedOnlyWhenValueChanges()
    {
        using var viewModel = CreateViewModel(
            new NativeRuntimeStatus(true, "ready", null));
        var changeCount = 0;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainWindowViewModel.ShowDisplayP3))
            {
                changeCount++;
            }
        };

        viewModel.ShowDisplayP3 = true;
        viewModel.ShowDisplayP3 = true;

        Assert.AreEqual(1, changeCount);
    }

    private static MainWindowViewModel CreateViewModel(
        NativeRuntimeStatus status,
        Func<Stream, string, CancellationToken, Task<IccProfileInfo>>? readAsync = null)
    {
        return new MainWindowViewModel(
            new StubProbe(status),
            new StubProfileReader(readAsync ?? ((_, displayName, _) =>
                Task.FromResult(CreateProfile(displayName)))));
    }

    private static IccProfileInfo CreateProfile(string displayName)
    {
        return new IccProfileInfo(
            displayName,
            4096,
            4.3,
            0x04300000,
            "Display",
            "RGB",
            "XYZ",
            new DateTime(2026, 7, 20, 12, 34, 56),
            "Perceptual",
            "Example profile",
            "Example manufacturer",
            "Example model",
            "Example copyright",
            "EXMP",
            "MODL",
            1,
            true,
            new IccColorTagData(
                null,
                null,
                null,
                new XyzColor(0.9642, 1, 0.8249),
                new XyzColor(0, 0, 0),
                null),
            new[] { new IccTagInfo("desc", "mluc", 256, 128) });
    }

    private sealed class StubProbe : INativeRuntimeProbe
    {
        private readonly NativeRuntimeStatus status;

        public StubProbe(NativeRuntimeStatus status)
        {
            this.status = status;
        }

        public NativeRuntimeStatus Probe() => status;
    }

    private sealed class StubProfileReader : IIccProfileReader
    {
        private readonly Func<Stream, string, CancellationToken, Task<IccProfileInfo>> readAsync;

        public StubProfileReader(
            Func<Stream, string, CancellationToken, Task<IccProfileInfo>> readAsync)
        {
            this.readAsync = readAsync;
        }

        public Task<IccProfileInfo> ReadAsync(
            Stream profileStream,
            string displayName,
            CancellationToken cancellationToken) =>
            readAsync(profileStream, displayName, cancellationToken);
    }

    private sealed class StubProfileFileSource : IProfileFileSource
    {
        public StubProfileFileSource(string displayName)
        {
            DisplayName = displayName;
        }

        public string DisplayName { get; }

        public Task<Stream> OpenReadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3 }));
        }
    }
}
