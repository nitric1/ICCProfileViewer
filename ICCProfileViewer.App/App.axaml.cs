using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ICCProfileViewer.App.Services;
using ICCProfileViewer.App.ViewModels;
using ICCProfileViewer.App.Views;

namespace ICCProfileViewer.App;

public sealed partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(new LcmsNativeRuntimeProbe()),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
