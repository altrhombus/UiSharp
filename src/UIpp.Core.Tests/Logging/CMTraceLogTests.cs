using UIpp.Core.Logging;

namespace UIpp.Core.Tests.Logging;

public class CMTraceLogTests
{
    private static string TempPath() => Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

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
        try
        {
            using var log = new CMTraceLog(path);
            log.Write("hello world");
            Assert.Contains("hello world", File.ReadAllText(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Write_HasCMTraceEnvelope()
    {
        var path = TempPath();
        try
        {
            using var log = new CMTraceLog(path);
            log.Write("msg");
            var text = File.ReadAllText(path);
            Assert.Contains("<![LOG[", text);
            Assert.Contains("]LOG]!>", text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Write_InfoSeverity_TypeOne()
    {
        var path = TempPath();
        try
        {
            using var log = new CMTraceLog(path);
            log.Write("msg", LogSeverity.Info);
            Assert.Contains("type=\"1\"", File.ReadAllText(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Write_WarningSeverity_TypeTwo()
    {
        var path = TempPath();
        try
        {
            using var log = new CMTraceLog(path);
            log.Write("msg", LogSeverity.Warning);
            Assert.Contains("type=\"2\"", File.ReadAllText(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Write_ErrorSeverity_TypeThree()
    {
        var path = TempPath();
        try
        {
            using var log = new CMTraceLog(path);
            log.Write("msg", LogSeverity.Error);
            Assert.Contains("type=\"3\"", File.ReadAllText(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Write_CustomComponent_AppearsInLog()
    {
        var path = TempPath();
        try
        {
            using var log = new CMTraceLog(path);
            log.Write("msg", LogSeverity.Info, "MyComp");
            Assert.Contains("component=\"MyComp\"", File.ReadAllText(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Write_MultipleMessages_AllPresent()
    {
        var path = TempPath();
        try
        {
            using var log = new CMTraceLog(path);
            log.Write("first");
            log.Write("second");
            var text = File.ReadAllText(path);
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
            using var log = new CMTraceLog(path);
            log.Write("line1\nline2");
            var text = File.ReadAllText(path);
            // newline inside the LOG[...] section must not appear — should be replaced by space
            var logContent = text[text.IndexOf("<![LOG[", StringComparison.Ordinal)..
                                  text.IndexOf("]LOG]!>", StringComparison.Ordinal)];
            Assert.DoesNotContain('\n', logContent);
        }
        finally { File.Delete(path); }
    }
}
