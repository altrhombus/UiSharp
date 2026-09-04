using UiSharp.Editor.Services;
using Microsoft.UI.Xaml;

namespace UiSharp.Editor.ViewModels;

public sealed class GitGraphLineViewModel
{
    public string GraphText     { get; }
    public string? Hash         { get; }
    public string? Subject      { get; }
    public string? Author       { get; }
    public string? RelDate      { get; }
    public bool IsCommit        => Hash is not null;
    public Visibility CommitVisibility    => IsCommit ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ConnectorVisibility => IsCommit ? Visibility.Collapsed : Visibility.Visible;

    public GitGraphLineViewModel(GitGraphLine line)
    {
        // Replace ASCII commit marker with a solid circle for a cleaner look.
        GraphText = line.GraphPrefix.Replace("*", "●");
        Hash      = line.Hash;
        Subject   = line.Subject;
        Author    = line.Author;
        RelDate   = line.RelDate;
    }
}
