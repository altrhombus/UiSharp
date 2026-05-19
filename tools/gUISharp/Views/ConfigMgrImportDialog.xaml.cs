using GUISharp.Services;
using GUISharp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
