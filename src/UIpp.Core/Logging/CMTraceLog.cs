using System.Text;

namespace UIpp.Core.Logging;

public sealed class CMTraceLog : ICMLog, IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new();
    private readonly int _tzOffsetMinutes;

    public CMTraceLog(string path)
    {
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
