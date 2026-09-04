using UIpp.Core.Logging;

namespace UIpp.Core.Tests.Logging;

/// <summary>
/// Regression tests for the log path handling.
///
/// The runtime used to pass <c>_SMSTSLogPath</c> — a DIRECTORY — straight to
/// <c>new FileStream(path, FileMode.Append, …)</c>, which throws
/// UnauthorizedAccessException on a directory. That happened three lines into
/// Main with no try/catch, so the executable died at startup inside every real
/// task sequence: no dialog, no log, nothing to diagnose. These tests exist so
/// that cannot come back.
/// </summary>
public class LogFileTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "uisharp_logtests_" + Guid.NewGuid().ToString("N"));

    public LogFileTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    // -------------------------------------------------------------------------
    // Path resolution
    // -------------------------------------------------------------------------

    [Fact]
    public void ResolvePath_AppendsComponentNameToTheDirectory()
    {
        // Matches C++ CCMLog::OpenLog: m_logPath /= (m_componentName + L".log")
        Assert.Equal(
            Path.Combine(@"X:\SMSTSLog", "UiSharp.log"),
            LogFile.ResolvePath(@"X:\SMSTSLog"));
    }

    [Fact]
    public void ResolvePath_UsesTheComponentNameGiven() =>
        Assert.Equal(
            Path.Combine(@"X:\logs", "Custom.log"),
            LogFile.ResolvePath(@"X:\logs", "Custom"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolvePath_FallsBackToTempWhenNoDirectory(string? directory)
    {
        // The original does the same when its location argument is empty.
        var resolved = LogFile.ResolvePath(directory);

        Assert.Equal(Path.Combine(Path.GetTempPath(), "UiSharp.log"), resolved);
    }

    [Fact]
    public void ResolvePath_NeverReturnsADirectory()
    {
        // The property that was violated: the result must be a file path.
        var resolved = LogFile.ResolvePath(_dir);

        Assert.False(Directory.Exists(resolved));
        Assert.Equal("UiSharp.log", Path.GetFileName(resolved));
    }

    // -------------------------------------------------------------------------
    // Opening the log — the actual bug
    // -------------------------------------------------------------------------

    [Fact]
    public void TryOpen_GivenADirectory_Succeeds()
    {
        // This is the exact shape of the production failure: what arrives is a
        // directory that exists, as _SMSTSLogPath always is.
        var log = CMTraceLog.TryOpen(_dir, out var failure);
        using var disposable = log as IDisposable;

        Assert.Null(failure);
        Assert.IsType<CMTraceLog>(log);
        Assert.Equal(Path.Combine(_dir, "UiSharp.log"), log.FilePath);

        log.Write("hello");

        Assert.True(File.Exists(Path.Combine(_dir, "UiSharp.log")));
    }

    [Fact]
    public void TryOpen_WritesContentThatCMTraceCanRead()
    {
        var log = CMTraceLog.TryOpen(_dir, out _);
        using var disposable = log as IDisposable;

        log.Write("a message", LogSeverity.Warning);

        // Read the way CMTrace.exe does — sharing read AND write — because the
        // log is still open for writing. File.ReadAllText asks for FileShare.Read
        // and is refused, which is itself the behaviour that keeps CMTrace able
        // to tail the log live.
        var text = ReadWhileOpen(Path.Combine(_dir, "UiSharp.log"));

        Assert.Contains("<![LOG[a message]LOG]!>", text);
        Assert.Contains("type=\"2\"", text);
        Assert.Contains($"component=\"{LogFile.DefaultComponent}\"", text);
    }

    private static string ReadWhileOpen(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);
        return reader.ReadToEnd();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryOpen_WithNoDirectory_UsesTemp(string? directory)
    {
        var log = CMTraceLog.TryOpen(directory, out var failure);
        using var disposable = log as IDisposable;

        Assert.Null(failure);
        Assert.Equal(Path.Combine(Path.GetTempPath(), "UiSharp.log"), log.FilePath);
    }

    [Fact]
    public void TryOpen_WithAnUnusableDirectory_FallsBackToTempAndReportsWhy()
    {
        // A path that cannot be a directory, e.g. a log path pointing at a file.
        var file = Path.Combine(_dir, "not-a-directory.txt");
        File.WriteAllText(file, "x");

        var log = CMTraceLog.TryOpen(file, out var failure);
        using var disposable = log as IDisposable;

        // Fell back rather than throwing...
        Assert.Equal(Path.Combine(Path.GetTempPath(), "UiSharp.log"), log.FilePath);
        // ...and said what went wrong, so it can be logged once a log exists.
        Assert.NotNull(failure);
        Assert.Contains("not-a-directory.txt", failure);
    }

    [Fact]
    public void TryOpen_NeverThrows()
    {
        // Whatever arrives, startup must survive it.
        string?[] hostile =
        [
            null, "", "   ",
            "\0invalid",
            new string('x', 400),
            @"\\?\nonexistent-unc-share\logs",
        ];

        foreach (var directory in hostile)
        {
            var log = CMTraceLog.TryOpen(directory, out _);
            using var disposable = log as IDisposable;

            Assert.NotNull(log);
            log.Write("must not throw");
        }
    }

    // -------------------------------------------------------------------------
    // NullLog
    // -------------------------------------------------------------------------

    [Fact]
    public void NullLog_SwallowsWritesAndHasNoFile()
    {
        ICMLog log = NullLog.Instance;

        log.Write("ignored", LogSeverity.Error);

        Assert.Null(log.FilePath);
    }

    [Fact]
    public void ICMLog_FilePath_DefaultsToNullForImplementorsThatDoNotCare()
    {
        ICMLog log = new MinimalLog();
        Assert.Null(log.FilePath);
    }

    private sealed class MinimalLog : ICMLog
    {
        public void Write(string message, LogSeverity severity = LogSeverity.Info,
                          string component = LogFile.DefaultComponent)
        { }
    }
}
