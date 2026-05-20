using GUISharp.Services;
using GUISharp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace GUISharp.Views;

public sealed partial class ConfigMgrImportDialog : ContentDialog
{
    public ConfigMgrImportViewModel Vm { get; } = new(new ConfigMgrService());

    public ConfigMgrImportDialog()
    {
        this.InitializeComponent();
        this.Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"];
    }

    public IEnumerable<CmSelectableItem> GetSelectedItems() => Vm.GetSelectedItems();

    // x:Bind TwoWay on TextBox.Text only pushes to the source on LostFocus.
    // These handlers keep CanConnect live as the user types.
    private void ServerNameBox_TextChanged(object sender, TextChangedEventArgs e)
        => Vm.ServerName = ((TextBox)sender).Text;

    private void SiteCodeBox_TextChanged(object sender, TextChangedEventArgs e)
        => Vm.SiteCode = ((TextBox)sender).Text;

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        await Vm.ConnectAsync();
    }

    private void TypeRadio_Checked(object sender, RoutedEventArgs e)
    {
        Vm.ShowApps = ReferenceEquals(sender, AppsRadio);
    }

    private void CredentialPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
            Vm.AltPassword = pb.Password;
    }
}
