using GUISharp.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GUISharp.Views;

public sealed partial class MainWindow : Window
{
    public MainWindowViewModel ViewModel => App.MainVm;

    public MainWindow()
    {
        this.InitializeComponent();
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

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;

        _ = item.Tag switch
        {
            "Actions"        => ContentFrame.Navigate(typeof(ActionListPage)),
            "GlobalSettings" => ContentFrame.Navigate(typeof(GlobalSettingsPage)),
            "Software"       => ContentFrame.Navigate(typeof(SoftwarePage)),
            _                => false,
        };
    }
}
