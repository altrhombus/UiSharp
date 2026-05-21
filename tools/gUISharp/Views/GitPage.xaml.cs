using GUISharp.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace GUISharp.Views;

public sealed partial class GitPage : Page
{
    public MainWindowViewModel ViewModel => App.MainVm;

    public GitPage()
    {
        this.InitializeComponent();
    }
}
