namespace UIpp.Core.Logging;

public enum LogSeverity { Info, Warning, Error }

public interface ICMLog
{
    void Write(string message, LogSeverity severity = LogSeverity.Info,
               string component = LogFile.DefaultComponent);

    /// <summary>
    /// Where this log is being written, or null when it is not backed by a file.
    ///
    /// The original asks its log object the same question when saving items
    /// (Actions.cpp:996 calls pCMLog->Filename() and pCMLog->Path()), rather
    /// than reconstructing the path from the task-sequence environment. Default
    /// implementation so test doubles need not care.
    /// </summary>
    string? FilePath => null;
}
