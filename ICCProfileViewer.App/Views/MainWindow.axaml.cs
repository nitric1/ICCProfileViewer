using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ICCProfileViewer.App.Services;
using ICCProfileViewer.App.ViewModels;

namespace ICCProfileViewer.App.Views;

public sealed partial class MainWindow : Window
{
    private static readonly FilePickerFileType IccProfileFileType = new("ICC color profiles")
    {
        Patterns = new[] { "*.icc", "*.icm" },
        MimeTypes = new[] { "application/vnd.iccprofile" },
    };

    public MainWindow() => AvaloniaXamlLoader.Load(this);

    private async void OpenProfileClick(object? sender, RoutedEventArgs eventArgs)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open ICC Profile",
                AllowMultiple = false,
                FileTypeFilter = new[] { IccProfileFileType },
            });

            if (files.Count == 1 && DataContext is MainWindowViewModel viewModel)
            {
                await viewModel.LoadProfileAsync(new StorageProfileFileSource(files[0]));
            }
        }
        catch (Exception exception)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ReportFilePickerError(exception);
            }
        }
    }

    protected override void OnClosed(EventArgs eventArgs)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnClosed(eventArgs);
    }
}
