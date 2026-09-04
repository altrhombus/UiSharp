using UiSharp.Core.Logging;

namespace UiSharp.Core.Tests.Logging;

public class CMTraceLogTests
{
    private static string TempPath() => Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    // Helper: write entries, dispose (flush), then return file contents.
    private static string WriteAndRead(string path, Action<CMTraceLog> act)
    {
        using (var log = new CMTraceLog(path))
            act(log);
        return File.ReadAllText(path);
    }

    [Fact]
    public void Write_CreatesFile()
    {
        var path = TempPath();
        try
        {
            using var log = new CMTraceLog(path);
            log.Write("test");
            Assert.True(File.Exists(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Write_ContainsMessage()
    {
        var path = TempPath();
        try { Assert.Contains("hello world", WriteAndRead(path, l => l.Write("hello world"))); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Write_HasCMTraceEnvelope()
    {
        var path = TempPath();
        try
        {
            var text = WriteAndRead(path, l => l.Write("msg"));
            Assert.Contains("<![LOG[", text);
            Assert.Contains("]LOG]!>", text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Write_InfoSeverity_TypeOne()
    {
        var path = TempPath();
        try { Assert.Contains("type=\"1\"", WriteAndRead(path, l => l.Write("msg", LogSeverity.Info))); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Write_WarningSeverity_TypeTwo()
    {
        var path = TempPath();
        try { Assert.Contains("type=\"2\"", WriteAndRead(path, l => l.Write("msg", LogSeverity.Warning))); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Write_ErrorSeverity_TypeThree()
    {
        var path = TempPath();
        try { Assert.Contains("type=\"3\"", WriteAndRead(path, l => l.Write("msg", LogSeverity.Error))); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Write_CustomComponent_AppearsInLog()
    {
        var path = TempPath();
        try { Assert.Contains("component=\"MyComp\"", WriteAndRead(path, l => l.Write("msg", LogSeverity.Info, "MyComp"))); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Write_MultipleMessages_AllPresent()
    {
        var path = TempPath();
        try
        {
            var text = WriteAndRead(path, l => { l.Write("first"); l.Write("second"); });
            Assert.Contains("first",  text);
            Assert.Contains("second", text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Write_AppendsToExistingFile()
    {
        var path = TempPath();
        try
        {
            using (var log1 = new CMTraceLog(path)) log1.Write("run1");
            using (var log2 = new CMTraceLog(path)) log2.Write("run2");
            var text = File.ReadAllText(path);
            Assert.Contains("run1", text);
            Assert.Contains("run2", text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Write_NewlineInMessage_Replaced()
    {
        var path = TempPath();
        try
        {
            var text = WriteAndRead(path, l => l.Write("line1\nline2"));
            var logContent = text[text.IndexOf("<![LOG[", StringComparison.Ordinal)..
                                  text.IndexOf("]LOG]!>", StringComparison.Ordinal)];
            Assert.DoesNotContain('\n', logContent);
        }
        finally { File.Delete(path); }
    }
}
