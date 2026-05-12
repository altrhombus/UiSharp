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
        this.UnhandledException += (_, e) =>
        {
            e.Handled = true;
            var msg = $"Unhandled exception:\n{e.Exception}";
            System.Diagnostics.Debug.WriteLine(msg);
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "guisharp_crash.txt"),
                msg);
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            var configService = new ConfigService();
            FileDialogService = new FileDialogService();
            var factory = new EditorViewModelFactory();

            MainVm = new MainWindowViewModel(configService, FileDialogService, factory);

            _window = new MainWindow();
            MainWindow = _window;
            _window.Activate();
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "guisharp_crash.txt"),
                ex.ToString());
            throw;
        }
    }

    private Window? _window;
}
