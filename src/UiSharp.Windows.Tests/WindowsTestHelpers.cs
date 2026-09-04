using UiSharp.Core.Logging;

namespace UiSharp.Windows.Tests;

internal sealed class NullLog : ICMLog
{
    public static readonly NullLog Instance = new();
    public void Write(string message, LogSeverity severity = LogSeverity.Info, string component = LogFile.DefaultComponent) { }
}
