using System.Diagnostics;

namespace GUISharp.Services;

public sealed class GitService : IGitService
{
    private static async Task<(string output, string error, int exitCode)> RunGitAsync(string workingDir, params string[] args)
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
        catch { return (string.Empty, string.Empty, -1); }

        if (proc is null) return (string.Empty, string.Empty, -1);

        // Read stdout and stderr concurrently to avoid deadlock if either buffer fills.
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        await Task.WhenAll(stdoutTask, stderrTask);
        await proc.WaitForExitAsync();
        return (stdoutTask.Result.Trim(), stderrTask.Result.Trim(), proc.ExitCode);
    }

    public async Task<GitRepoInfo?> GetRepoInfoAsync(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(dir)) return null;

        var (rawRoot, _, rootExit) = await RunGitAsync(dir, "rev-parse", "--show-toplevel");
        if (rootExit != 0) return null;

        var repoRoot = rawRoot.Replace('/', Path.DirectorySeparatorChar);

        var (branch, _, _) = await RunGitAsync(repoRoot, "branch", "--show-current");
        if (string.IsNullOrEmpty(branch))
        {
            var (hash, _, _) = await RunGitAsync(repoRoot, "rev-parse", "--short", "HEAD");
            branch = string.IsNullOrEmpty(hash) ? "detached HEAD" : $"HEAD ({hash})";
        }

        var relativePath = Path.GetRelativePath(repoRoot, filePath);
        var (status, _, _) = await RunGitAsync(repoRoot, "status", "--porcelain", "--", relativePath);
        var hasChanges     = !string.IsNullOrWhiteSpace(status);

        return new GitRepoInfo(repoRoot, branch, hasChanges);
    }

    public async Task<IReadOnlyList<GitGraphLine>> GetGraphAsync(string filePath, int maxCount = 50)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(dir)) return [];

        // Marker must not plausibly appear in commit messages.
        const string marker = "\x01COMMIT\x01";
        const string sep    = "\x02";
        var fmt = $"{marker}%h{sep}%s{sep}%an{sep}%ar";

        var (output, _, exit) = await RunGitAsync(dir,
            "log", "--graph", "--topo-order", $"-n{maxCount}",
            $"--pretty=format:{fmt}", "--", filePath);
        if (exit != 0 || string.IsNullOrWhiteSpace(output)) return [];

        var result = new List<GitGraphLine>();
        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.TrimEnd();
            var mi   = line.IndexOf(marker, StringComparison.Ordinal);
            if (mi >= 0)
            {
                var graphPart = line[..mi].TrimEnd();
                var data      = line[(mi + marker.Length)..].Split(sep, 4);
                result.Add(new GitGraphLine(graphPart,
                    data.Length > 0 ? data[0] : null,
                    data.Length > 1 ? data[1] : null,
                    data.Length > 2 ? data[2] : null,
                    data.Length > 3 ? data[3] : null));
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                result.Add(new GitGraphLine(line, null, null, null, null));
            }
        }
        return result;
    }

    public async Task DiscardFileAsync(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath) ?? filePath;
        var (rawRoot, _, rootExit) = await RunGitAsync(dir, "rev-parse", "--show-toplevel");
        if (rootExit != 0)
            throw new InvalidOperationException("Could not locate git repository root.");
        var repoRoot     = rawRoot.Replace('/', Path.DirectorySeparatorChar);
        var relativePath = Path.GetRelativePath(repoRoot, filePath).Replace('\\', '/');
        var (_, err, exit) = await RunGitAsync(repoRoot, "restore", "--", relativePath);
        if (exit != 0)
            throw new InvalidOperationException(string.IsNullOrEmpty(err)
                ? $"git restore exited with code {exit}"
                : err);
    }

    public async Task StageFileAsync(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath) ?? filePath;
        var (_, err, exit) = await RunGitAsync(dir, "add", "--", filePath);
        if (exit != 0)
            throw new InvalidOperationException(string.IsNullOrEmpty(err)
                ? $"git add exited with code {exit}"
                : err);
    }

    public async Task CommitAsync(string repoRoot, string message)
    {
        var (_, err, exit) = await RunGitAsync(repoRoot, "commit", "-m", message);
        if (exit != 0)
            throw new InvalidOperationException(string.IsNullOrEmpty(err)
                ? $"git commit exited with code {exit}"
                : err);
    }

}
