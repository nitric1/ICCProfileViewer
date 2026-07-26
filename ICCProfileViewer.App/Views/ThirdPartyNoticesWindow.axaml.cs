using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ICCProfileViewer.App.Services;

namespace ICCProfileViewer.App.Views;

public sealed partial class ThirdPartyNoticesWindow : Window
{
    public ThirdPartyNoticesWindow()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<TextBox>("NoticeText")!.Text =
            ThirdPartyNoticesCatalog.Text;
    }

    private void CloseClick(object? sender, RoutedEventArgs eventArgs) => Close();
}
