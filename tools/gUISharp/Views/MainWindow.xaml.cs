using GUISharp.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace GUISharp.Views;

public sealed partial class MainWindow : Window
{
    public MainWindowViewModel ViewModel => App.MainVm;

    public MainWindow()
    {
        try { this.InitializeComponent(); }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "guisharp_crash.txt"),
                $"MainWindow.InitializeComponent threw:\nHResult: 0x{ex.HResult:X8}\n{ex}\nInner: {ex.InnerException}");
            throw;
        }
        Title = ViewModel.WindowTitle;
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.WindowTitle))
                Title = ViewModel.WindowTitle;
        };

        AppWindow.Closing += OnWindowClosingAsync;

        // Navigate to Actions page by default.
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private async void OnWindowClosingAsync(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (!ViewModel.IsModified) return;
        args.Cancel = true;

        var dialog = new ContentDialog
        {
            Title             = "Unsaved Changes",
            Content           = "Save changes before closing?",
            PrimaryButtonText = "Save",
            SecondaryButtonText = "Don't Save",
            CloseButtonText   = "Cancel",
            XamlRoot          = Content.XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Secondary)
        {
            ViewModel.IsModified = false;
            this.Close();
        }
        else if (result == ContentDialogResult.Primary)
        {
            if (await ViewModel.TrySaveAsync())
                this.Close();
        }
    }

    private void RecentFilesFlyout_Opening(object sender, object e)
    {
        var flyout = (MenuFlyout)sender;
        while (flyout.Items.Count > 2)
            flyout.Items.RemoveAt(2);

        var recent = ViewModel.RecentFiles;
        if (recent.Count == 0)
        {
            flyout.Items.Add(new MenuFlyoutItem { Text = "No recent files", IsEnabled = false });
            return;
        }

        foreach (var path in recent)
        {
            var item = new MenuFlyoutItem { Text = System.IO.Path.GetFileName(path) };
            ToolTipService.SetToolTip(item, path);
            item.Click += (_, _) => _ = ViewModel.OpenRecentCommand.ExecuteAsync(path);
            flyout.Items.Add(item);
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;

        try
        {
            _ = item.Tag switch
            {
                "Actions"        => ContentFrame.Navigate(typeof(ActionListPage)),
                "GlobalSettings" => ContentFrame.Navigate(typeof(GlobalSettingsPage)),
                "Software"       => ContentFrame.Navigate(typeof(SoftwarePage)),
                "Variables"      => ContentFrame.Navigate(typeof(VariablesPage)),
                _                => false,
            };
        }
        catch (Exception ex)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Navigate({item.Tag}) threw ===");
            var e2 = ex;
            int d = 0;
            while (e2 is not null && d++ < 8)
            {
                sb.AppendLine($"[{d}] {e2.GetType().FullName}  HResult=0x{e2.HResult:X8}");
                sb.AppendLine($"    {e2.Message}");
                sb.AppendLine(e2.StackTrace);
                e2 = e2.InnerException;
            }
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "guisharp_crash.txt"),
                sb.ToString());
            throw;
        }
    }
}
