namespace GUISharp.Services;

public sealed record GitRepoInfo(string RepoRoot, string Branch, bool HasChanges);

public interface IGitService
{
    Task<GitRepoInfo?> GetRepoInfoAsync(string filePath);
    Task DiscardFileAsync(string filePath);
    Task StageFileAsync(string filePath);
    Task CommitAsync(string repoRoot, string message);
}
