namespace GUISharp.Services;

public sealed record GitRepoInfo(string RepoRoot, string Branch, bool HasChanges);
public sealed record GitCommit(string Hash, string Subject, string Author, string RelativeDate);

public interface IGitService
{
    Task<GitRepoInfo?> GetRepoInfoAsync(string filePath);
    Task<IReadOnlyList<GitCommit>> GetFileLogAsync(string filePath, int maxCount = 15);
    Task DiscardFileAsync(string filePath);
    Task StageFileAsync(string filePath);
    Task CommitAsync(string repoRoot, string message);
}
