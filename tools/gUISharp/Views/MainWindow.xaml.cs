using UiSharp.Editor.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace UiSharp.Editor.Views;

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

        ViewModel.NavigationRequested += OnNavigationRequested;

        NavView.IsPaneOpen = App.UserSettings.Settings.NavPaneOpen;
        NavView.PaneOpened += (_, _) => { App.UserSettings.Settings.NavPaneOpen = true;  App.UserSettings.Save(); };
        NavView.PaneClosed += (_, _) => { App.UserSettings.Settings.NavPaneOpen = false; App.UserSettings.Save(); };

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
            ViewModel.ClearModified();
            this.Close();
        }
        else if (result == ContentDialogResult.Primary)
        {
            if (await ViewModel.TrySaveAsync())
                this.Close();
        }
    }

    private void OnNavigationRequested(string tag)
    {
        if (NavView.MenuItems.OfType<NavigationViewItem>()
                             .FirstOrDefault(i => (string?)i.Tag == tag) is { } item)
            NavView.SelectedItem = item;
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

        flyout.Items.Add(new MenuFlyoutSeparator());
        var clearItem = new MenuFlyoutItem { Text = "Clear Recent Files" };
        clearItem.Click += (_, _) => ViewModel.ClearRecentFilesCommand.Execute(null);
        flyout.Items.Add(clearItem);
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(PreferencesPage));
            return;
        }

        if (args.SelectedItem is not NavigationViewItem item) return;

        _ = item.Tag switch
        {
            "Actions"        => ContentFrame.Navigate(typeof(ActionListPage)),
            "GlobalSettings" => ContentFrame.Navigate(typeof(GlobalSettingsPage)),
            "Software"       => ContentFrame.Navigate(typeof(SoftwarePage)),
            "Variables"      => ContentFrame.Navigate(typeof(VariablesPage)),
            "Git"            => ContentFrame.Navigate(typeof(GitPage)),
            _                => false,
        };
    }
}
