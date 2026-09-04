using System.Text;

namespace UIpp.Core.Logging;

public sealed class CMTraceLog : ICMLog, IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new();
    private readonly int _tzOffsetMinutes;

    public string? FilePath { get; }

    /// <summary>
    /// Opens a log in <paramref name="directory"/>, never throwing. Returns a
    /// <see cref="NullLog"/> when the file cannot be opened — an unwritable log
    /// directory must not stop a deployment, and the caller has nowhere to
    /// report the failure to anyway.
    /// </summary>
    /// <param name="failure">
    /// Why opening failed, for reporting once some other channel exists.
    /// </param>
    public static ICMLog TryOpen(string? directory, out string? failure,
                                 string component = LogFile.DefaultComponent)
    {
        failure = null;

        // The task-sequence directory first, then the temp directory, so a
        // read-only or missing SMSTS log path still leaves a usable log.
        string[] candidates = string.IsNullOrWhiteSpace(directory)
            ? [LogFile.ResolvePath(null, component)]
            : [LogFile.ResolvePath(directory, component), LogFile.ResolvePath(null, component)];

        foreach (var candidate in candidates)
        {
            try
            {
                return new CMTraceLog(candidate);
            }
            catch (Exception ex)
            {
                failure ??= $"could not open '{candidate}': {ex.Message}";
            }
        }

        return NullLog.Instance;
    }

    public CMTraceLog(string path)
    {
        FilePath = path;

        // FileShare.ReadWrite so CMTrace.exe (and tests) can read the log while it's open.
        var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writer = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true,
        };
        // CMTrace convention: positive = minutes west of UTC (opposite of .NET sign)
        _tzOffsetMinutes = -(int)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes;
    }

    public void Write(string message, LogSeverity severity = LogSeverity.Info, string component = "UIpp")
    {
        var now  = DateTime.Now;
        var type = severity switch
        {
            LogSeverity.Warning => 2,
            LogSeverity.Error   => 3,
            _                   => 1,
        };

        var line = $"<![LOG[{message.Replace('\n', ' ')}]LOG]!>" +
                   // _tzOffsetMinutes is negative for UTC+ zones (CMTrace convention: positive = west of UTC).
                   // The :+0;-0;+0 format emits the sign explicitly for all values, e.g. -120 for UTC+2.
                   $"<time=\"{now:HH:mm:ss.fff}{_tzOffsetMinutes:+0;-0;+0}\"" +
                   $" date=\"{now:MM-dd-yyyy}\"" +
                   $" component=\"{component}\"" +
                   $" context=\"\"" +
                   $" type=\"{type}\"" +
                   $" thread=\"{Environment.CurrentManagedThreadId}\"" +
                   $" file=\"\">";

        lock (_lock)
            _writer.WriteLine(line);
    }

    public void Dispose() => _writer.Dispose();
}
