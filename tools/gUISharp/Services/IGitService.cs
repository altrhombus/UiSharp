namespace UiSharp.Editor.Services;

public sealed record GitRepoInfo(string RepoRoot, string Branch, bool HasChanges);

// One line of `git log --graph` output — either a commit line (Hash is not null) or a graph connector line.
public sealed record GitGraphLine(string GraphPrefix, string? Hash, string? Subject, string? Author, string? RelDate);

public interface IGitService
{
    Task<GitRepoInfo?> GetRepoInfoAsync(string filePath);
    Task<IReadOnlyList<GitGraphLine>> GetGraphAsync(string filePath, int maxCount = 50);
    Task DiscardFileAsync(string filePath);
    Task StageFileAsync(string filePath);
    Task CommitAsync(string repoRoot, string message);
}
