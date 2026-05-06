namespace UIpp.Core.Logging;

public enum LogSeverity { Info, Warning, Error }

public interface ICMLog
{
    void Write(string message, LogSeverity severity = LogSeverity.Info, string component = "UIpp");
}
