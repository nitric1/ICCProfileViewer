using ICCProfileViewer.App.Services;

namespace ICCProfileViewer.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private bool showSrgb = true;
    private bool showDisplayP3;
    private bool showDciP3;
    private bool showAdobeRgb;
    private bool showBt2020;
    private bool showWhitePoints = true;

    public MainWindowViewModel(INativeRuntimeProbe nativeRuntimeProbe)
    {
        var runtimeStatus = nativeRuntimeProbe.Probe();
        NativeRuntimeSummary = runtimeStatus.Summary;
        DiagnosticMessage = runtimeStatus.DiagnosticMessage;
        IsIccEngineAvailable = runtimeStatus.IsAvailable;
        State = runtimeStatus.IsAvailable
            ? ApplicationViewState.Empty
            : ApplicationViewState.NativeDependencyError;
        StatusMessage = runtimeStatus.IsAvailable
            ? "Ready. Profile opening will be implemented next."
            : "ICC profile loading is disabled until Little CMS is available.";
    }

    public string WindowTitle => "ICC Profile Viewer";

    public ApplicationViewState State { get; }

    public string StateName => State.ToString();

    public bool IsIccEngineAvailable { get; }

    public string NativeRuntimeSummary { get; }

    public string? DiagnosticMessage { get; }

    public bool HasDiagnosticMessage => DiagnosticMessage is not null;

    public string StatusMessage { get; }

    public string ProfileName => "No profile loaded";

    public string EmptyValue => "—";

    public bool ShowSrgb
    {
        get => showSrgb;
        set => SetProperty(ref showSrgb, value);
    }

    public bool ShowDisplayP3
    {
        get => showDisplayP3;
        set => SetProperty(ref showDisplayP3, value);
    }

    public bool ShowDciP3
    {
        get => showDciP3;
        set => SetProperty(ref showDciP3, value);
    }

    public bool ShowAdobeRgb
    {
        get => showAdobeRgb;
        set => SetProperty(ref showAdobeRgb, value);
    }

    public bool ShowBt2020
    {
        get => showBt2020;
        set => SetProperty(ref showBt2020, value);
    }

    public bool ShowWhitePoints
    {
        get => showWhitePoints;
        set => SetProperty(ref showWhitePoints, value);
    }
}
