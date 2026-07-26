using System;
using Avalonia.Controls;
using Avalonia.Input;
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

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        DragDrop.SetAllowDrop(this, true);
        DragDrop.AddDragOverHandler(this, ProfileDragOver);
        DragDrop.AddDragLeaveHandler(this, ProfileDragLeave);
        DragDrop.AddDropHandler(this, ProfileDrop);
    }

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

    private async void ThirdPartyNoticesClick(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        var window = new ThirdPartyNoticesWindow();
        await window.ShowDialog(this);
    }

    private void ProfileDragOver(object? sender, DragEventArgs eventArgs)
    {
        var canAccept = TryGetDroppedProfile(eventArgs.DataTransfer, out _)
            && DataContext is MainWindowViewModel { CanOpenProfile: true };
        eventArgs.DragEffects = canAccept ? DragDropEffects.Copy : DragDropEffects.None;
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ShowDropTarget(canAccept);
        }

        eventArgs.Handled = true;
    }

    private void ProfileDragLeave(object? sender, DragEventArgs eventArgs)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.HideDropTarget();
        }

        eventArgs.Handled = true;
    }

    private async void ProfileDrop(object? sender, DragEventArgs eventArgs)
    {
        eventArgs.Handled = true;
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.HideDropTarget();
        if (viewModel.CanOpenProfile
            && TryGetDroppedProfile(eventArgs.DataTransfer, out var profileFile))
        {
            await viewModel.LoadProfileAsync(new StorageProfileFileSource(profileFile));
        }
    }

    private static bool TryGetDroppedProfile(
        IDataTransfer dataTransfer,
        out IStorageFile profileFile)
    {
        var items = dataTransfer.TryGetFiles();
        if (items is { Length: 1 }
            && items[0] is IStorageFile file
            && ProfileFileName.HasSupportedExtension(file.Name))
        {
            profileFile = file;
            return true;
        }

        profileFile = null!;
        return false;
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
