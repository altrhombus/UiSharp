using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUISharp.Services;
using GUISharp.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UIpp.Core.Configuration;

namespace GUISharp.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IConfigService _configService;
    private readonly IFileDialogService _fileDialogService;

    public ActionListViewModel     ActionList     { get; }
    public GlobalSettingsViewModel GlobalSettings { get; } = new();
    public SoftwareViewModel       Software       { get; } = new();

    // ── Recent files ─────────────────────────────────────────────────────────

    public ObservableCollection<string> RecentFiles { get; } = new();

    private static readonly string RecentFilesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "gUISharp", "recent_files.txt");

    private const int MaxRecentFiles = 5;

    private void LoadRecentFiles()
    {
        try
        {
            if (!File.Exists(RecentFilesPath)) return;
            foreach (var line in File.ReadAllLines(RecentFilesPath)
                                     .Where(File.Exists)
                                     .Take(MaxRecentFiles))
                RecentFiles.Add(line);
        }
        catch { /* non-fatal */ }
    }

    private void AddToRecentFiles(string path)
    {
        RecentFiles.Remove(path);
        RecentFiles.Insert(0, path);
        while (RecentFiles.Count > MaxRecentFiles)
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RecentFilesPath)!);
            File.WriteAllLines(RecentFilesPath, RecentFiles);
        }
        catch { /* non-fatal */ }
    }

    [RelayCommand]
    private async Task OpenRecentAsync(string path)
    {
        if (!await ConfirmDiscardChangesAsync()) return;
        try
        {
            var config = await _configService.LoadAsync(path);
            LoadConfig(config);
            CurrentFile = path;
            ClearModified();
            AddToRecentFiles(path);
        }
        catch (Exception ex)
        {
            RecentFiles.Remove(path);
            await ShowErrorAsync("Failed to open file", ex.Message);
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    public partial string? CurrentFile { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModified))]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    [NotifyPropertyChangedFor(nameof(ActionsModifiedVisibility))]
    public partial bool ActionsModified { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModified))]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    [NotifyPropertyChangedFor(nameof(GlobalSettingsModifiedVisibility))]
    public partial bool GlobalSettingsModified { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModified))]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    [NotifyPropertyChangedFor(nameof(SoftwareModifiedVisibility))]
    public partial bool SoftwareModified { get; set; }

    public bool IsModified => ActionsModified || GlobalSettingsModified || SoftwareModified;

    public Visibility ActionsModifiedVisibility =>
        ActionsModified ? Visibility.Visible : Visibility.Collapsed;
    public Visibility GlobalSettingsModifiedVisibility =>
        GlobalSettingsModified ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SoftwareModifiedVisibility =>
        SoftwareModified ? Visibility.Visible : Visibility.Collapsed;

    public void ClearModified()
    {
        ActionsModified        = false;
        GlobalSettingsModified = false;
        SoftwareModified       = false;
        ActionList.MarkAllActionsClean();
        Software.MarkAllItemsClean();
    }

    [ObservableProperty]
    public partial bool IsFileLoaded { get; set; }

    public bool HasRecentFiles => RecentFiles.Count > 0;

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
        ActionList.Dirtied     += (_, _) => ActionsModified        = true;
        GlobalSettings.Dirtied += (_, _) => GlobalSettingsModified = true;
        Software.Dirtied       += (_, _) => SoftwareModified       = true;
        LoadRecentFiles();
        RecentFiles.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasRecentFiles));
    }

    [RelayCommand]
    private async Task New()
    {
        if (!await ConfirmDiscardChangesAsync()) return;
        if (App.MainWindow?.Content is not FrameworkElement root) return;

        var wizard = new NewConfigWizardDialog { XamlRoot = root.XamlRoot };
        if (await wizard.ShowAsync() != ContentDialogResult.Primary) return;

        try
        {
            LoadConfig(_configService.LoadFromXml(wizard.GetTemplateXml()));
        }
        catch
        {
            LoadConfig(_configService.NewConfig());
        }
        CurrentFile = null;
        ClearModified();
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
            ClearModified();
            AddToRecentFiles(path);
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
        GlobalSettings.LoadFrom(config.GlobalTraits, config.ConditionEngine, config.SchemaVersion,
                                config.DocumentComment);
        Software.LoadFrom(config.SoftwareList, config.SoftwareElement);
        ActionList.LoadActions(config.Actions);
        IsFileLoaded = true;
    }

    private EditorConfig BuildConfig()
    {
        return new EditorConfig
        {
            GlobalTraits     = GlobalSettings.ToTraits(),
            ConditionEngine  = GlobalSettings.ConditionEngine,
            SchemaVersion    = GlobalSettings.GetSchemaVersion(),
            DocumentComment  = GlobalSettings.Comment,
            SoftwareList     = Software.CollectSoftware(),
            SoftwareComments = Software.GetSoftwareComments(),
            Actions          = ActionList.CollectModels(),
        };
    }

    private async Task SaveToPathAsync(string path)
    {
        try
        {
            var config = BuildConfig();
            await _configService.SaveAsync(config, path);
            ClearModified();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to save file", ex.Message);
        }
    }

    public void MarkModified() => ActionsModified = GlobalSettingsModified = SoftwareModified = true;

    // ── Navigation ────────────────────────────────────────────────────────────

    public event Action<string>? NavigationRequested;

    public void NavigateToAction(ActionNodeViewModel node)
    {
        ActionList.SelectAction(node);
        NavigationRequested?.Invoke("Actions");
    }

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
            Title               = "Unsaved Changes",
            Content             = "Do you want to save your changes before continuing?",
            PrimaryButtonText   = "Save",
            SecondaryButtonText = "Don't Save",
            CloseButtonText     = "Cancel",
            DefaultButton       = ContentDialogButton.Primary,
            XamlRoot            = root.XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
            return await TrySaveAsync();
        return result == ContentDialogResult.Secondary;
    }

    private static async Task ShowErrorAsync(string title, string message)
    {
        var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "guisharp_error.log");
        System.Diagnostics.Debug.WriteLine($"[ERROR] {title}: {message}");
        System.IO.File.AppendAllText(logPath,
            $"[{DateTime.Now:HH:mm:ss}] {title}: {message}{Environment.NewLine}");

        if (App.MainWindow?.Content is not FrameworkElement root) return;

        var dialog = new ContentDialog
        {
            Title           = title,
            Content         = $"{message}\n\nDetails written to: {logPath}",
            CloseButtonText = "OK",
            XamlRoot        = root.XamlRoot,
        };
        await dialog.ShowAsync();
    }
}
