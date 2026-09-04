using UiSharp.Editor.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace UiSharp.Editor.Views;

public sealed partial class GitPage : Page
{
    public MainWindowViewModel ViewModel => App.MainVm;

    public GitPage()
    {
        this.InitializeComponent();
    }
}
