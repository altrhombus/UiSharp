using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUISharp.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UIpp.Core.Configuration;

namespace GUISharp.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IConfigService _configService;
    private readonly IFileDialogService _fileDialogService;

    public ActionListViewModel    ActionList    { get; }
    public GlobalSettingsViewModel GlobalSettings { get; } = new();
    public SoftwareViewModel      Software      { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    public partial string? CurrentFile { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    public partial bool IsModified { get; set; }

    public string WindowTitle =>
        CurrentFile is null
            ? "gUI#"
            : $"{(IsModified ? "* " : string.Empty)}{Path.GetFileName(CurrentFile)} — gUI#";

    public MainWindowViewModel(
        IConfigService configService,
        IFileDialogService fileDialogService,
        EditorViewModelFactory factory)
    {
        _configService     = configService;
        _fileDialogService = fileDialogService;
        ActionList         = new ActionListViewModel(factory);
        ActionList.Dirtied += (_, _) => MarkModified();
    }

    [RelayCommand]
    private async Task New()
    {
        if (!await ConfirmDiscardChangesAsync()) return;
        LoadConfig(_configService.NewConfig());
        CurrentFile = null;
        IsModified  = false;
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        if (!await ConfirmDiscardChangesAsync()) return;
        var path = await _fileDialogService.PickOpenFileAsync();
        if (path is null) return;

        try
        {
            var config = await _configService.LoadAsync(path);
            LoadConfig(config);
            CurrentFile = path;
            IsModified  = false;
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to open file", ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (CurrentFile is null)
        {
            await SaveAsAsync();
            return;
        }
        await SaveToPathAsync(CurrentFile);
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        var path = await _fileDialogService.PickSaveFileAsync(
            CurrentFile is null ? "UI++.xml" : Path.GetFileName(CurrentFile));
        if (path is null) return;
        await SaveToPathAsync(path);
        CurrentFile = path;
    }

    // -------------------------------------------------------------------------

    private void LoadConfig(EditorConfig config)
    {
        GlobalSettings.LoadFrom(config.GlobalTraits, config.ConditionEngine, config.SchemaVersion);
        Software.LoadFrom(config.SoftwareList);
        ActionList.LoadActions(config.Actions);
    }

    private EditorConfig BuildConfig()
    {
        return new EditorConfig
        {
            GlobalTraits    = GlobalSettings.ToTraits(),
            ConditionEngine = GlobalSettings.ConditionEngine,
            SchemaVersion   = GlobalSettings.GetSchemaVersion(),
            SoftwareList    = Software.CollectSoftware(),
            Actions         = ActionList.CollectModels(),
        };
    }

    private async Task SaveToPathAsync(string path)
    {
        try
        {
            var config = BuildConfig();
            await _configService.SaveAsync(config, path);
            IsModified = false;
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to save file", ex.Message);
        }
    }

    public void MarkModified() => IsModified = true;

    /// <summary>Saves the current file (prompting for a path if unsaved). Returns true when the caller may proceed.</summary>
    public async Task<bool> TrySaveAsync()
    {
        string? path = CurrentFile;
        if (path is null)
        {
            path = await _fileDialogService.PickSaveFileAsync("UI++.xml");
            if (path is null) return false;
            CurrentFile = path;
        }
        await SaveToPathAsync(path);
        return !IsModified;
    }

    private async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (!IsModified) return true;
        if (App.MainWindow?.Content is not FrameworkElement root) return true;

        var dialog = new ContentDialog
        {
            Title           = "Unsaved Changes",
            Content         = "You have unsaved changes. Do you want to discard them?",
            PrimaryButtonText = "Discard",
            CloseButtonText   = "Cancel",
            XamlRoot        = root.XamlRoot,
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private static async Task ShowErrorAsync(string title, string message)
    {
        System.Diagnostics.Debug.WriteLine($"[ERROR] {title}: {message}");
        System.IO.File.AppendAllText(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "guisharp_error.log"),
            $"[{DateTime.Now:HH:mm:ss}] {title}: {message}{Environment.NewLine}");

        if (App.MainWindow?.Content is not FrameworkElement root) return;

        var dialog = new ContentDialog
        {
            Title          = title,
            Content        = message,
            CloseButtonText = "OK",
            XamlRoot       = root.XamlRoot,
        };
        await dialog.ShowAsync();
    }
}
