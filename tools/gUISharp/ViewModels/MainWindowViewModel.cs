using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UiSharp.Editor.Services;
using UiSharp.Editor.Views;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UiSharp.Core.Configuration;
using UiSharp.Editing;

namespace UiSharp.Editor.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IConfigService _configService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IGitService? _gitService;

    // ── Undo / Redo ──────────────────────────────────────────────────────────

    private readonly IUndoService _undoService;
    private readonly DispatcherQueueTimer _snapshotTimer;
    private AppStateSnapshot? _lastSnapshot;
    private bool _isUndoRedoing;

    private AppStateSnapshot CaptureSnapshot() => new(
        GlobalSettings.CurrentXmlText,
        Software.CurrentXmlText,
        ActionList.CurrentXmlText);

    private void OnAnyDirtied(object? sender, EventArgs e)
    {
        if (_isUndoRedoing) return;
        _snapshotTimer.Stop();
        _snapshotTimer.Start();
    }

    private void CommitSnapshot()
    {
        if (_lastSnapshot is null) return;
        _undoService.Push(_lastSnapshot);
        _lastSnapshot = CaptureSnapshot();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private void ApplySnapshot(AppStateSnapshot snapshot)
    {
        _isUndoRedoing = true;
        GlobalSettings.OnXmlEdited(snapshot.GlobalSettingsXml);
        Software.OnXmlEdited(snapshot.SoftwareXml);
        ActionList.OnXmlEdited(snapshot.ActionsXml);
        _isUndoRedoing = false;
    }

    private bool CanUndoAction() => _undoService.CanUndo;
    private bool CanRedoAction() => _undoService.CanRedo;

    [RelayCommand(CanExecute = nameof(CanUndoAction))]
    private void Undo()
    {
        var current  = _lastSnapshot ?? CaptureSnapshot();
        var previous = _undoService.TryUndo(current);
        if (previous is null) return;
        _lastSnapshot = previous;
        ApplySnapshot(previous);
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRedoAction))]
    private void Redo()
    {
        var current = _lastSnapshot ?? CaptureSnapshot();
        var next    = _undoService.TryRedo(current);
        if (next is null) return;
        _lastSnapshot = next;
        ApplySnapshot(next);
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    public ActionListViewModel     ActionList     { get; }
    public GlobalSettingsViewModel GlobalSettings { get; } = new();
    public SoftwareViewModel       Software       { get; } = new();

    // ── Recent files ─────────────────────────────────────────────────────────

    public ObservableCollection<string> RecentFiles { get; } = new();

    private static int MaxRecentFiles => App.UserSettings?.Settings.RecentFilesLimit ?? 10;

    private void LoadRecentFiles()
    {
        var stored = App.UserSettings?.Settings.RecentFiles ?? new();
        foreach (var line in stored.Where(File.Exists).Take(MaxRecentFiles))
            RecentFiles.Add(line);
    }

    private void AddToRecentFiles(string path)
    {
        RecentFiles.Remove(path);
        RecentFiles.Insert(0, path);
        while (RecentFiles.Count > MaxRecentFiles)
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
        PersistRecentFiles();
    }

    private void PersistRecentFiles()
    {
        if (App.UserSettings is not { } svc) return;
        svc.Settings.RecentFiles = new(RecentFiles);
        svc.Save();
    }

    [RelayCommand]
    private void RemoveRecentFile(string path)
    {
        RecentFiles.Remove(path);
        PersistRecentFiles();
    }

    [RelayCommand]
    private void ClearRecentFiles()
    {
        RecentFiles.Clear();
        PersistRecentFiles();
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
            await RefreshGitStateAsync();
        }
        catch (Exception ex)
        {
            RecentFiles.Remove(path);
            await ShowErrorAsync("Failed to open file", ex.Message);
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    [NotifyCanExecuteChangedFor(nameof(DiscardChangesCommand))]
    [NotifyCanExecuteChangedFor(nameof(CommitCommand))]
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

    public int ConfigVersion { get; private set; }

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
        GlobalSettings.MarkClean();
    }

    [ObservableProperty]
    public partial bool IsFileLoaded { get; set; }

    public bool HasRecentFiles => RecentFiles.Count > 0;

    public string WindowTitle =>
        CurrentFile is null
            ? "gUI# — Visual editor for UI++ configurations"
            : $"{Path.GetFileName(CurrentFile)}{(IsModified ? " •" : string.Empty)} — gUI#";

    // ── Git integration ───────────────────────────────────────────────────────

    public GitPageViewModel Git { get; } = new();

    private async Task RefreshGitStateAsync()
    {
        if (_gitService is null || CurrentFile is null)
        {
            Git.Update(null, null, []);
            return;
        }

        var info  = await _gitService.GetRepoInfoAsync(CurrentFile);
        var graph = info is not null
            ? await _gitService.GetGraphAsync(CurrentFile)
            : (IReadOnlyList<GitGraphLine>)[];
        Git.Update(info, CurrentFile, graph);
    }

    [RelayCommand]
    private async Task RefreshGit() => await RefreshGitStateAsync();

    private bool CanDiscardChanges() => Git.IsGitRepo && Git.HasChanges && CurrentFile is not null;
    private bool CanCommit()         => Git.IsGitRepo && Git.HasChanges && CurrentFile is not null;

    [RelayCommand(CanExecute = nameof(CanDiscardChanges))]
    private async Task DiscardChanges()
    {
        if (_gitService is null || CurrentFile is null) return;
        if (App.MainWindow?.Content is not FrameworkElement root) return;

        var dialog = new ContentDialog
        {
            Title               = "Discard Changes",
            Content             = $"Restore {Path.GetFileName(CurrentFile)} to its last committed state? All unsaved and uncommitted changes will be lost.",
            PrimaryButtonText   = "Discard",
            CloseButtonText     = "Cancel",
            DefaultButton       = ContentDialogButton.Close,
            XamlRoot            = root.XamlRoot,
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        try
        {
            await _gitService.DiscardFileAsync(CurrentFile);
            var config = await _configService.LoadAsync(CurrentFile);
            LoadConfig(config);
            ClearModified();
            await RefreshGitStateAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Discard failed", ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanCommit))]
    private async Task Commit()
    {
        if (_gitService is null || CurrentFile is null || Git.RepoRoot is null) return;
        if (App.MainWindow?.Content is not FrameworkElement root) return;

        if (IsModified)
        {
            if (!await TrySaveAsync()) return;
        }

        var commitDialog = new Views.CommitDialog(CurrentFile) { XamlRoot = root.XamlRoot };
        if (await commitDialog.ShowAsync() != ContentDialogResult.Primary) return;

        try
        {
            await _gitService.StageFileAsync(CurrentFile);
            await _gitService.CommitAsync(Git.RepoRoot, commitDialog.CommitMessage);
            await RefreshGitStateAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Commit failed", ex.Message);
        }
    }

    public MainWindowViewModel(
        IConfigService configService,
        IFileDialogService fileDialogService,
        EditorViewModelFactory factory,
        IUndoService? undoService = null,
        IGitService? gitService   = null)
    {
        _configService     = configService;
        _fileDialogService = fileDialogService;
        _undoService       = undoService ?? new UndoService();
        _gitService        = gitService;
        ActionList         = new ActionListViewModel(factory);
        ActionList.Dirtied     += (_, _) => ActionsModified        = true;
        GlobalSettings.Dirtied += (_, _) => GlobalSettingsModified = true;
        Software.Dirtied       += (_, _) => SoftwareModified       = true;

        ActionList.BecameClean     += (_, _) => ActionsModified        = false;
        GlobalSettings.BecameClean += (_, _) => GlobalSettingsModified = false;
        Software.BecameClean       += (_, _) => SoftwareModified       = false;

        ActionList.Dirtied     += OnAnyDirtied;
        GlobalSettings.Dirtied += OnAnyDirtied;
        Software.Dirtied       += OnAnyDirtied;

        var queue = DispatcherQueue.GetForCurrentThread();
        _snapshotTimer = queue.CreateTimer();
        _snapshotTimer.Interval    = TimeSpan.FromMilliseconds(500);
        _snapshotTimer.IsRepeating = false;
        _snapshotTimer.Tick       += (_, _) => CommitSnapshot();

        Git.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(GitPageViewModel.IsGitRepo) or nameof(GitPageViewModel.HasChanges))
            {
                DiscardChangesCommand.NotifyCanExecuteChanged();
                CommitCommand.NotifyCanExecuteChanged();
            }
        };

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
        await RefreshGitStateAsync();
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
            await RefreshGitStateAsync();
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
        ConfigVersion++;
        _undoService.Clear();
        _lastSnapshot = CaptureSnapshot();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
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
            await RefreshGitStateAsync();
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

    public void NavigateToActions()  => NavigationRequested?.Invoke("Actions");
    public void NavigateToSoftware() => NavigationRequested?.Invoke("Software");

    public string? PendingVariableFilter { get; private set; }

    public void NavigateToVariables(string? filter = null)
    {
        PendingVariableFilter = filter;
        NavigationRequested?.Invoke("Variables");
    }

    public void ClearPendingVariableFilter() => PendingVariableFilter = null;

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
