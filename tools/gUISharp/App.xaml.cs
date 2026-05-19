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
    public static UserSettingsService UserSettings { get; private set; } = null!;

    public App()
    {
        this.InitializeComponent();
        this.UnhandledException += (_, e) =>
        {
            e.Handled = true;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Unhandled Exception ===");
            sb.AppendLine($"Type:    {e.Exception.GetType().FullName}");
            sb.AppendLine($"HResult: 0x{e.Exception.HResult:X8}");
            sb.AppendLine($"Message: {e.Exception.Message}");
            sb.AppendLine();
            sb.AppendLine("Stack trace:");
            sb.AppendLine(e.Exception.StackTrace);
            var inner = e.Exception.InnerException;
            int depth = 0;
            while (inner is not null && depth++ < 5)
            {
                sb.AppendLine($"\n--- Inner ({depth}) ---");
                sb.AppendLine($"Type:    {inner.GetType().FullName}");
                sb.AppendLine($"HResult: 0x{inner.HResult:X8}");
                sb.AppendLine($"Message: {inner.Message}");
                sb.AppendLine(inner.StackTrace);
                inner = inner.InnerException;
            }
            var text = sb.ToString();
            System.Diagnostics.Debug.WriteLine(text);
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "guisharp_crash.txt"),
                text);
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            UserSettings = new UserSettingsService();

            var configService = new ConfigService();
            FileDialogService = new FileDialogService();
            var factory = new EditorViewModelFactory();

            MainVm = new MainWindowViewModel(configService, FileDialogService, factory);

            _window = new MainWindow();
            MainWindow = _window;
            ApplyTheme();
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

    public static void ApplyTheme()
    {
        if (MainWindow?.Content is not Microsoft.UI.Xaml.FrameworkElement root) return;
        root.RequestedTheme = UserSettings.Settings.Theme switch
        {
            AppTheme.Light  => Microsoft.UI.Xaml.ElementTheme.Light,
            AppTheme.Dark   => Microsoft.UI.Xaml.ElementTheme.Dark,
            _               => Microsoft.UI.Xaml.ElementTheme.Default,
        };
    }

    private Window? _window;
}
