namespace UiSharp.Core.Logging;

/// <summary>
/// Works out where the log goes, mirroring C++ <c>CCMLog::OpenLog</c>
/// (FTWCMLog/FTWCMLog.cpp:64):
///
/// <code>
///     if (!location.empty()) m_logPath.assign(location);
///     else                   m_logPath.assign(getenv("TEMP"));
///     m_logPath /= (m_componentName + L".log");
/// </code>
///
/// The location is a <b>directory</b> — in a task sequence it is
/// <c>_SMSTSLogPath</c> — and the component name plus <c>.log</c> is appended to
/// it. Treating that directory as a file path is what made the runtime throw at
/// startup inside every real task sequence.
/// </summary>
public static class LogFile
{
    /// <summary>
    /// Component name stamped on every CMTrace line and used for the log's file
    /// name, as in the original where both come from the same string.
    /// </summary>
    public const string DefaultComponent = "UiSharp";

    /// <summary>
    /// Resolves the full log file path from a directory that may be null, empty
    /// or whitespace. Falls back to the temp directory, as the original does
    /// when its location argument is empty.
    /// </summary>
    public static string ResolvePath(string? directory, string component = DefaultComponent)
    {
        var name = string.IsNullOrWhiteSpace(component) ? DefaultComponent : component;

        var dir = string.IsNullOrWhiteSpace(directory)
            ? Path.GetTempPath()
            : directory.Trim();

        return Path.Combine(dir, name + ".log");
    }
}
