using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICCProfileViewer.App.Services;
using ICCProfileViewer.Core.Colorimetry;
using ICCProfileViewer.Core.Profiles;
using ICCProfileViewer.Lcms;

namespace ICCProfileViewer.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IIccProfileReader profileReader;
    private CancellationTokenSource? loadCancellation;
    private ApplicationViewState state;
    private IccProfileInfo? profile;
    private bool isIccEngineAvailable;
    private string nativeRuntimeSummary;
    private string? diagnosticMessage;
    private string statusMessage;
    private bool isDropTargetVisible;
    private bool canAcceptDrop;
    private bool showSrgb = true;
    private bool showDisplayP3;
    private bool showDciP3;
    private bool showAdobeRgb;
    private bool showBt2020;
    private bool showWhitePoints = true;

    public MainWindowViewModel(
        INativeRuntimeProbe nativeRuntimeProbe,
        IIccProfileReader profileReader)
    {
        this.profileReader = profileReader;
        var runtimeStatus = nativeRuntimeProbe.Probe();
        nativeRuntimeSummary = runtimeStatus.Summary;
        diagnosticMessage = runtimeStatus.DiagnosticMessage;
        isIccEngineAvailable = runtimeStatus.IsAvailable;
        state = runtimeStatus.IsAvailable
            ? ApplicationViewState.Empty
            : ApplicationViewState.NativeDependencyError;
        statusMessage = runtimeStatus.IsAvailable
            ? "Ready. Open an ICC or ICM profile."
            : "ICC profile loading is disabled until Little CMS is available.";
    }

    public string WindowTitle => "ICC Profile Viewer";

    public ApplicationViewState State => state;

    public string StateName => State.ToString();

    public bool IsLoading => State == ApplicationViewState.Loading;

    public bool CanOpenProfile => IsIccEngineAvailable && !IsLoading;

    public bool IsIccEngineAvailable => isIccEngineAvailable;

    public string NativeRuntimeSummary => nativeRuntimeSummary;

    public string? DiagnosticMessage => diagnosticMessage;

    public bool HasDiagnosticMessage => DiagnosticMessage is not null;

    public string StatusMessage => statusMessage;

    public bool IsDropTargetVisible => isDropTargetVisible;

    public bool CanAcceptDrop => canAcceptDrop;

    public string DropTargetMessage => CanAcceptDrop
        ? "Drop to open this color profile"
        : "Drop one .icc or .icm file";

    public string ProfileName => profile?.DisplayName ?? "No profile loaded";

    public string ProfileSize => profile is null ? EmptyValue : FormatSize(profile.SizeInBytes);

    public string ProfileVersion => profile?.Version.ToString("0.0#", CultureInfo.InvariantCulture) ?? EmptyValue;

    public string ProfileClass => profile?.ProfileClass ?? EmptyValue;

    public string DataColorSpace => profile?.DataColorSpace ?? EmptyValue;

    public string ProfileConnectionSpace => profile?.ProfileConnectionSpace ?? EmptyValue;

    public string CreationDate => profile?.CreationDate?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? EmptyValue;

    public string RenderingIntent => profile?.RenderingIntent ?? EmptyValue;

    public string Description => DisplayOrEmpty(profile?.Description);

    public string Manufacturer => DisplayOrEmpty(profile?.Manufacturer ?? profile?.HeaderManufacturerSignature);

    public string Model => DisplayOrEmpty(profile?.Model ?? profile?.HeaderModelSignature);

    public string Copyright => DisplayOrEmpty(profile?.Copyright);

    public string WhitePoint => FormatXyz(profile?.ColorTags.MediaWhitePoint);

    public string BlackPoint => FormatXyz(profile?.ColorTags.MediaBlackPoint);

    public string ProfileStructure => profile is null
        ? EmptyValue
        : profile.IsMatrixShaper ? "Matrix/TRC" : "LUT or other";

    public IReadOnlyList<ProfileMetadataRow> SummaryRows => new[]
    {
        new ProfileMetadataRow("File size", ProfileSize),
        new ProfileMetadataRow("Version", ProfileVersion),
        new ProfileMetadataRow("Profile class", ProfileClass),
        new ProfileMetadataRow("Color space", DataColorSpace),
        new ProfileMetadataRow("PCS", ProfileConnectionSpace),
        new ProfileMetadataRow("Created", CreationDate),
        new ProfileMetadataRow("Rendering intent", RenderingIntent),
        new ProfileMetadataRow("Structure", ProfileStructure),
        new ProfileMetadataRow("Description", Description),
        new ProfileMetadataRow("Manufacturer", Manufacturer),
        new ProfileMetadataRow("Model", Model),
        new ProfileMetadataRow("White point", WhitePoint),
        new ProfileMetadataRow("Black point", BlackPoint),
        new ProfileMetadataRow("Copyright", Copyright),
    };

    public string TagSummary => profile is null
        ? "No tags"
        : profile.TagCount == 1 ? "1 tag" : $"{profile.TagCount} tags";

    public IReadOnlyList<IccTagInfo> Tags => profile?.Tags ?? Array.Empty<IccTagInfo>();

    public bool HasTags => Tags.Count > 0;

    public string EmptyValue => "—";

    public async Task LoadProfileAsync(IProfileFileSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!IsIccEngineAvailable)
        {
            return;
        }

        var cancellation = new CancellationTokenSource();
        var previousCancellation = loadCancellation;
        loadCancellation = cancellation;
        previousCancellation?.Cancel();

        ClearProfile();
        SetDiagnosticMessage(null);
        SetStatusMessage($"Loading {source.DisplayName}...");
        SetState(ApplicationViewState.Loading);

        try
        {
            await using var stream = await source.OpenReadAsync(cancellation.Token);
            var loadedProfile = await profileReader.ReadAsync(
                stream,
                source.DisplayName,
                cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();

            if (!ReferenceEquals(loadCancellation, cancellation))
            {
                return;
            }

            SetProfile(loadedProfile);
            SetStatusMessage($"Metadata loaded successfully ({loadedProfile.TagCount} tags).");
            SetState(ApplicationViewState.Loaded);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (LcmsNativeLibraryException exception)
        {
            if (!ReferenceEquals(loadCancellation, cancellation))
            {
                return;
            }

            SetIccEngineAvailable(false);
            SetNativeRuntimeSummary("Little CMS is not available");
            SetDiagnosticMessage(exception.Message);
            SetStatusMessage("ICC profile loading is disabled until Little CMS is available.");
            SetState(ApplicationViewState.NativeDependencyError);
        }
        catch (LcmsProfileReadException exception)
        {
            if (!ReferenceEquals(loadCancellation, cancellation))
            {
                return;
            }

            SetDiagnosticMessage(CreateProfileReadDiagnostic(exception));
            SetStatusMessage($"{source.DisplayName} is not a valid or supported ICC profile.");
            SetState(ApplicationViewState.InvalidProfile);
        }
        catch (Exception exception)
        {
            if (!ReferenceEquals(loadCancellation, cancellation))
            {
                return;
            }

            SetDiagnosticMessage(exception.Message);
            SetStatusMessage($"Could not open {source.DisplayName}.");
            SetState(ApplicationViewState.UnexpectedError);
        }
        finally
        {
            if (ReferenceEquals(loadCancellation, cancellation))
            {
                loadCancellation = null;
                OnPropertyChanged(nameof(CanOpenProfile));
            }

            cancellation.Dispose();
        }
    }

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

    public void Dispose()
    {
        loadCancellation?.Cancel();
        loadCancellation = null;
    }

    public void ReportFilePickerError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        SetDiagnosticMessage(exception.Message);
        SetStatusMessage("Could not open the system file picker.");
        SetState(ApplicationViewState.UnexpectedError);
    }

    public void ShowDropTarget(bool acceptsProfile)
    {
        if (SetProperty(ref canAcceptDrop, acceptsProfile, nameof(CanAcceptDrop)))
        {
            OnPropertyChanged(nameof(DropTargetMessage));
        }

        SetProperty(ref isDropTargetVisible, true, nameof(IsDropTargetVisible));
    }

    public void HideDropTarget() =>
        SetProperty(ref isDropTargetVisible, false, nameof(IsDropTargetVisible));

    private static string DisplayOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;

    private static string FormatSize(long sizeInBytes)
    {
        if (sizeInBytes >= 1024 * 1024)
        {
            return $"{sizeInBytes / (1024d * 1024d):0.##} MiB ({sizeInBytes:N0} bytes)";
        }

        if (sizeInBytes >= 1024)
        {
            return $"{sizeInBytes / 1024d:0.##} KiB ({sizeInBytes:N0} bytes)";
        }

        return $"{sizeInBytes:N0} bytes";
    }

    private static string FormatXyz(XyzColor? value) => value is null
        ? "—"
        : FormattableString.Invariant($"X {value.Value.X:0.####}, Y {value.Value.Y:0.####}, Z {value.Value.Z:0.####}");

    private static string CreateProfileReadDiagnostic(LcmsProfileReadException exception)
    {
        var details = exception.InnerException?.Message;
        return string.IsNullOrWhiteSpace(details)
            ? exception.Message
            : $"{exception.Message} {details}";
    }

    private void ClearProfile() => SetProfile(null);

    private void SetProfile(IccProfileInfo? value)
    {
        profile = value;
        OnPropertyChanged(nameof(ProfileName));
        OnPropertyChanged(nameof(ProfileSize));
        OnPropertyChanged(nameof(ProfileVersion));
        OnPropertyChanged(nameof(ProfileClass));
        OnPropertyChanged(nameof(DataColorSpace));
        OnPropertyChanged(nameof(ProfileConnectionSpace));
        OnPropertyChanged(nameof(CreationDate));
        OnPropertyChanged(nameof(RenderingIntent));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(Manufacturer));
        OnPropertyChanged(nameof(Model));
        OnPropertyChanged(nameof(Copyright));
        OnPropertyChanged(nameof(WhitePoint));
        OnPropertyChanged(nameof(BlackPoint));
        OnPropertyChanged(nameof(ProfileStructure));
        OnPropertyChanged(nameof(SummaryRows));
        OnPropertyChanged(nameof(TagSummary));
        OnPropertyChanged(nameof(Tags));
        OnPropertyChanged(nameof(HasTags));
    }

    private void SetState(ApplicationViewState value)
    {
        if (!SetProperty(ref state, value, nameof(State)))
        {
            return;
        }

        OnPropertyChanged(nameof(StateName));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(CanOpenProfile));
    }

    private void SetIccEngineAvailable(bool value)
    {
        if (SetProperty(ref isIccEngineAvailable, value, nameof(IsIccEngineAvailable)))
        {
            OnPropertyChanged(nameof(CanOpenProfile));
        }
    }

    private void SetNativeRuntimeSummary(string value) =>
        SetProperty(ref nativeRuntimeSummary, value, nameof(NativeRuntimeSummary));

    private void SetDiagnosticMessage(string? value)
    {
        if (SetProperty(ref diagnosticMessage, value, nameof(DiagnosticMessage)))
        {
            OnPropertyChanged(nameof(HasDiagnosticMessage));
        }
    }

    private void SetStatusMessage(string value) =>
        SetProperty(ref statusMessage, value, nameof(StatusMessage));
}
