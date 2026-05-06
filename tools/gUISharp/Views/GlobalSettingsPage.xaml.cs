using GUISharp.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace GUISharp.Views;

public sealed partial class GlobalSettingsPage : Page
{
    public MainWindowViewModel ViewModel => App.MainVm;

    public GlobalSettingsPage()
    {
        this.InitializeComponent();
    }
}
