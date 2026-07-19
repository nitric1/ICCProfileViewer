namespace ICCProfileViewer.App.ViewModels;

public enum ApplicationViewState
{
    Empty,
    Loading,
    Loaded,
    PartiallySupported,
    InvalidProfile,
    NativeDependencyError,
    UnexpectedError,
}
