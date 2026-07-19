using ICCProfileViewer.App.Services;
using ICCProfileViewer.App.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ICCProfileViewer.App.Tests;

[TestClass]
public sealed class MainWindowViewModelTests
{
    [TestMethod]
    public void Constructor_WithAvailableRuntime_StartsInEmptyState()
    {
        var viewModel = new MainWindowViewModel(new StubProbe(
            new NativeRuntimeStatus(true, "Little CMS 2.19 ready", null)));

        Assert.AreEqual(ApplicationViewState.Empty, viewModel.State);
        Assert.IsTrue(viewModel.IsIccEngineAvailable);
        Assert.AreEqual("Little CMS 2.19 ready", viewModel.NativeRuntimeSummary);
        Assert.IsFalse(viewModel.HasDiagnosticMessage);
        Assert.IsTrue(viewModel.ShowSrgb);
        Assert.IsTrue(viewModel.ShowWhitePoints);
    }

    [TestMethod]
    public void Constructor_WithMissingRuntime_ReportsNativeDependencyError()
    {
        var viewModel = new MainWindowViewModel(new StubProbe(
            new NativeRuntimeStatus(false, "Little CMS is not available", "Copy lcms2.dll.")));

        Assert.AreEqual(ApplicationViewState.NativeDependencyError, viewModel.State);
        Assert.IsFalse(viewModel.IsIccEngineAvailable);
        Assert.IsTrue(viewModel.HasDiagnosticMessage);
        Assert.AreEqual("Copy lcms2.dll.", viewModel.DiagnosticMessage);
        StringAssert.Contains(viewModel.StatusMessage, "disabled");
    }

    [TestMethod]
    public void OverlayProperties_RaisePropertyChangedOnlyWhenValueChanges()
    {
        var viewModel = new MainWindowViewModel(new StubProbe(
            new NativeRuntimeStatus(true, "ready", null)));
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

    private sealed class StubProbe : INativeRuntimeProbe
    {
        private readonly NativeRuntimeStatus status;

        public StubProbe(NativeRuntimeStatus status)
        {
            this.status = status;
        }

        public NativeRuntimeStatus Probe() => status;
    }
}
