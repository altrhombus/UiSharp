using UIpp.Core.Logging;

namespace UIpp.Windows.Tests;

internal sealed class NullLog : ICMLog
{
    public static readonly NullLog Instance = new();
    public void Write(string message, LogSeverity severity = LogSeverity.Info, string component = "UIpp") { }
}
