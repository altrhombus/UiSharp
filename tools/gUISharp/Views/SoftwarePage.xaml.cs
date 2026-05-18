using GUISharp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GUISharp.Views;

public sealed partial class SoftwarePage : Page
{
    public MainWindowViewModel ViewModel => App.MainVm;

    public SoftwarePage()
    {
        this.InitializeComponent();
    }

    private async void ImportFromConfigMgr_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ConfigMgrImportDialog { XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ViewModel.Software.ImportItems(dialog.GetSelectedItems());
            ViewModel.MarkModified();
        }
    }

    private async void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        var selected = ViewModel.Software.SelectedItem;
        if (selected is null) return;

        var dialog = new ContentDialog
        {
            Title             = "Remove Software Item",
            Content           = $"Remove \"{selected.Label}\"? Any TSVarList references to this item will become unresolved.",
            PrimaryButtonText = "Remove",
            CloseButtonText   = "Cancel",
            DefaultButton     = ContentDialogButton.Close,
            XamlRoot          = XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        ViewModel.Software.RemoveItemCommand.Execute(null);
        ViewModel.MarkModified();
    }
}
