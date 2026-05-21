using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using Microsoft.UI.Xaml;

namespace GUISharp.ViewModels;

public sealed partial class GitPageViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavVisibility))]
    [NotifyPropertyChangedFor(nameof(HasChangesVisibility))]
    [NotifyPropertyChangedFor(nameof(NoChangesVisibility))]
    public partial bool IsGitRepo { get; set; }

    [ObservableProperty]
    public partial string Branch { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChangesVisibility))]
    [NotifyPropertyChangedFor(nameof(NoChangesVisibility))]
    public partial bool HasChanges { get; set; }

    [ObservableProperty]
    public partial string RelativePath { get; set; } = string.Empty;

    // Computed visibility helpers (no converters needed in Window x:Bind)
    public Visibility NavVisibility            => IsGitRepo  ? Visibility.Visible : Visibility.Collapsed;
    public Visibility HasChangesVisibility     => HasChanges ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NoChangesVisibility      => HasChanges ? Visibility.Collapsed : Visibility.Visible;
    public Visibility IsHistoryEmptyVisibility => Graph.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public ObservableCollection<GitGraphLineViewModel> Graph { get; } = new();

    public GitPageViewModel()
    {
        Graph.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsHistoryEmptyVisibility));
    }

    // Used by MainWindowViewModel for the commit command
    internal string? RepoRoot { get; private set; }

    internal void Update(GitRepoInfo? info, string? filePath, IReadOnlyList<GitGraphLine> graph)
    {
        if (info is null)
        {
            IsGitRepo    = false;
            Branch       = string.Empty;
            HasChanges   = false;
            RelativePath = string.Empty;
            RepoRoot     = null;
            Graph.Clear();
            return;
        }

        RepoRoot     = info.RepoRoot;
        IsGitRepo    = true;
        Branch       = info.Branch;
        HasChanges   = info.HasChanges;
        RelativePath = filePath is not null
            ? Path.GetRelativePath(info.RepoRoot, filePath).Replace('\\', '/')
            : string.Empty;

        Graph.Clear();
        foreach (var line in graph)
            Graph.Add(new GitGraphLineViewModel(line));
    }
}
