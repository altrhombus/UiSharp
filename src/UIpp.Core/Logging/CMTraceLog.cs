using System.Text;

namespace UIpp.Core.Logging;

public sealed class CMTraceLog : ICMLog, IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new();
    private readonly int _tzOffsetMinutes;

    public CMTraceLog(string path)
    {
        _writer = new StreamWriter(path, append: true, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
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
                   $"<time=\"{now:HH:mm:ss.fff}+{_tzOffsetMinutes}\"" +
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
