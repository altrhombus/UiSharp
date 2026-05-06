using GUISharp.Services;
using GUISharp.ViewModels;
using GUISharp.Views;
using Microsoft.UI.Xaml;

namespace GUISharp;

public partial class App : Application
{
    public static MainWindowViewModel MainVm { get; private set; } = null!;
    public static IFileDialogService FileDialogService { get; private set; } = null!;
    public static Window? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var configService = new ConfigService();
        FileDialogService = new FileDialogService();
        var factory = new EditorViewModelFactory();

        MainVm = new MainWindowViewModel(configService, FileDialogService, factory);

        _window = new MainWindow();
        MainWindow = _window;
        _window.Activate();
    }

    private Window? _window;
}
