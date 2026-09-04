namespace UIpp.Core.Logging;

/// <summary>
/// Discards everything written to it.
///
/// Exists so that failing to open the log file cannot stop the runtime: losing
/// the log during an OS deployment is bad, but dying before showing any UI is
/// far worse. Callers get a logger that always works rather than a null they
/// have to check.
/// </summary>
public sealed class NullLog : ICMLog
{
    public static readonly NullLog Instance = new();

    public string? FilePath => null;

    public void Write(string message, LogSeverity severity = LogSeverity.Info,
                      string component = LogFile.DefaultComponent)
    {
    }
}
