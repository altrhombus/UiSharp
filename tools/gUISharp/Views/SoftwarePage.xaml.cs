using GUISharp.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace GUISharp.Views;

public sealed partial class SoftwarePage : Page
{
    public MainWindowViewModel ViewModel => App.MainVm;

    public SoftwarePage()
    {
        this.InitializeComponent();
    }
}
