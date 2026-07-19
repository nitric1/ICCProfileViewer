using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ICCProfileViewer.App.Diagnostics;
using ICCProfileViewer.App.Services;
using ICCProfileViewer.App.ViewModels;
using ICCProfileViewer.App.Views;
using ICCProfileViewer.Lcms;

namespace ICCProfileViewer.App;

public sealed partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var diagnosticLog = new ApplicationDiagnosticLog();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(
                    new LcmsNativeRuntimeProbe(),
                    new LcmsProfileReader(),
                    diagnosticLog),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
