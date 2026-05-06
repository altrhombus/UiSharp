using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUISharp.Services;
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
    private string? _currentFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private bool _isModified;

    public string WindowTitle =>
        _currentFile is null
            ? "gUI#"
            : $"{(IsModified ? "* " : string.Empty)}{Path.GetFileName(_currentFile)} — gUI#";

    public MainWindowViewModel(
        IConfigService configService,
        IFileDialogService fileDialogService,
        EditorViewModelFactory factory)
    {
        _configService     = configService;
        _fileDialogService = fileDialogService;
        ActionList         = new ActionListViewModel(factory);
    }

    [RelayCommand]
    private void New()
    {
        if (!ConfirmDiscardChanges()) return;
        LoadConfig(_configService.NewConfig());
        CurrentFile  = null;
        IsModified   = false;
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        if (!ConfirmDiscardChanges()) return;
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

    private bool ConfirmDiscardChanges()
    {
        // In v1, we always allow discarding — dialog confirmation added in polish phase.
        return true;
    }

    private static Task ShowErrorAsync(string title, string message)
    {
        // Placeholder — wired to a WinUI ContentDialog in the view layer.
        System.Diagnostics.Debug.WriteLine($"[ERROR] {title}: {message}");
        return Task.CompletedTask;
    }
}
