using GUISharp.ViewModels;
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

        // Navigate to Actions page by default.
        NavView.SelectedItem = NavView.MenuItems[0];
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
