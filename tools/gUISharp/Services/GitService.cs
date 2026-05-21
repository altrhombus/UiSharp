using System.Diagnostics;

namespace GUISharp.Services;

public sealed class GitService : IGitService
{
    private static async Task<(string output, int exitCode)> RunGitAsync(string workingDir, params string[] args)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory       = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        Process? proc;
        try   { proc = Process.Start(psi); }
        catch { return (string.Empty, -1); }

        if (proc is null) return (string.Empty, -1);

        var output = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();
        return (output.Trim(), proc.ExitCode);
    }

    public async Task<GitRepoInfo?> GetRepoInfoAsync(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(dir)) return null;

        var (rawRoot, rootExit) = await RunGitAsync(dir, "rev-parse", "--show-toplevel");
        if (rootExit != 0) return null;

        var repoRoot = rawRoot.Replace('/', Path.DirectorySeparatorChar);

        var (branch, _) = await RunGitAsync(repoRoot, "branch", "--show-current");
        if (string.IsNullOrEmpty(branch))
        {
            var (hash, _) = await RunGitAsync(repoRoot, "rev-parse", "--short", "HEAD");
            branch = string.IsNullOrEmpty(hash) ? "detached HEAD" : $"HEAD ({hash})";
        }

        var relativePath = Path.GetRelativePath(repoRoot, filePath);
        var (status, _)  = await RunGitAsync(repoRoot, "status", "--porcelain", "--", relativePath);
        var hasChanges   = !string.IsNullOrWhiteSpace(status);

        return new GitRepoInfo(repoRoot, branch, hasChanges);
    }

    public async Task<IReadOnlyList<GitCommit>> GetFileLogAsync(string filePath, int maxCount = 15)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(dir)) return [];

        // Record separator unlikely to appear in any field.
        const string rs = "|||RS|||";
        var fmt = $"%h%n%s%n%an%n%ar%n{rs}";

        var (output, exit) = await RunGitAsync(dir,
            "log", $"-n{maxCount}", $"--pretty=format:{fmt}", "--", filePath);
        if (exit != 0 || string.IsNullOrWhiteSpace(output)) return [];

        var results = new List<GitCommit>();
        foreach (var entry in output.Split(rs, StringSplitOptions.RemoveEmptyEntries))
        {
            var lines = entry.Trim().Split('\n');
            if (lines.Length >= 4)
                results.Add(new GitCommit(lines[0].Trim(), lines[1].Trim(),
                                          lines[2].Trim(), lines[3].Trim()));
        }
        return results;
    }

    public async Task DiscardFileAsync(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath) ?? filePath;
        var (_, exit) = await RunGitAsync(dir, "restore", "--", filePath);
        if (exit != 0)
            throw new InvalidOperationException($"git restore exited with code {exit}");
    }

    public async Task StageFileAsync(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath) ?? filePath;
        var (_, exit) = await RunGitAsync(dir, "add", "--", filePath);
        if (exit != 0)
            throw new InvalidOperationException($"git add exited with code {exit}");
    }

    public async Task CommitAsync(string repoRoot, string message)
    {
        var (_, exit) = await RunGitAsync(repoRoot, "commit", "-m", message);
        if (exit != 0)
            throw new InvalidOperationException($"git commit exited with code {exit}");
    }
}
