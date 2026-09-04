using UiSharp.Core.Actions;
using UiSharp.Core.Actions.Impl;

namespace UiSharp.Core.Tests.Actions.Impl;

public class ActionSaveItemsTests : IDisposable
{
    private readonly string _tempDir;

    public ActionSaveItemsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"UiSharpSaveItemsTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── TSVariables ────────────────────────────────────────────────────────

    [Fact]
    public void TSVariables_DefaultFilename_WritesVariablesFile()
    {
        var dest = Path.Combine(_tempDir, "out");
        var el   = ActionTestData.ActionEl(
            $"""<Action Type="SaveItems" Path="{dest}" Items="TSVariables" />""");
        var (env, _, data) = ActionTestData.Make(el);
        env.Set("ComputerName", "TESTPC");
        env.Set("XSomething",   "hidden"); // X-prefixed: should be excluded

        new ActionSaveItems(data).Go();

        var expected = Path.Combine(dest, "UI++ Variable Dump.txt");
        Assert.True(File.Exists(expected), "Default dump file was not created.");
        var lines = File.ReadAllLines(expected);
        Assert.Contains(lines, l => l.StartsWith("ComputerName=", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(lines, l => l.StartsWith("XSomething=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TSVariables_CustomFilename_UsesSpecifiedName()
    {
        var dest = Path.Combine(_tempDir, "custom");
        var el   = ActionTestData.ActionEl(
            $"""<Action Type="SaveItems" Path="{dest}" Items="TSVariables:myvars.txt" />""");
        var (env, _, data) = ActionTestData.Make(el);
        env.Set("Foo", "bar");

        new ActionSaveItems(data).Go();

        Assert.True(File.Exists(Path.Combine(dest, "myvars.txt")));
        Assert.False(File.Exists(Path.Combine(dest, "UI++ Variable Dump.txt")));
    }

    [Fact]
    public void TSVariables_PathCreatedIfNotExist()
    {
        var dest = Path.Combine(_tempDir, "nonexistent", "nested");
        var el   = ActionTestData.ActionEl(
            $"""<Action Type="SaveItems" Path="{dest}" Items="TSVariables" />""");
        var (_, _, data) = ActionTestData.Make(el);

        var result = new ActionSaveItems(data).Go();

        Assert.Equal(ActionResult.Next, result);
        Assert.True(Directory.Exists(dest));
    }

    [Fact]
    public void TSVariables_TokenCaseInsensitive()
    {
        var dest = Path.Combine(_tempDir, "ci");
        var el   = ActionTestData.ActionEl(
            $"""<Action Type="SaveItems" Path="{dest}" Items="tsvariables" />""");
        var (env, _, data) = ActionTestData.Make(el);
        env.Set("Foo", "bar");

        new ActionSaveItems(data).Go();

        Assert.True(File.Exists(Path.Combine(dest, "UI++ Variable Dump.txt")));
    }

    // ── UILOG ──────────────────────────────────────────────────────────────

    [Fact]
    public void UILOG_NullLogPath_DoesNotThrow()
    {
        // LocalTSEnv.LogPath is always null; action should silently skip.
        var dest = Path.Combine(_tempDir, "uilog_null");
        var el   = ActionTestData.ActionEl(
            $"""<Action Type="SaveItems" Path="{dest}" Items="UILOG" />""");
        var (_, _, data) = ActionTestData.Make(el);

        var ex = Record.Exception(() => new ActionSaveItems(data).Go());
        Assert.Null(ex);
    }

    // ── SMSTSLOG ──────────────────────────────────────────────────────────

    [Fact]
    public void SMSTSLOG_PathNotSet_LogsWarning()
    {
        var dest = Path.Combine(_tempDir, "smstslog_nopath");
        var el   = ActionTestData.ActionEl(
            $"""<Action Type="SaveItems" Path="{dest}" Items="SMSTSLOG" />""");
        var (_, log, data) = ActionTestData.Make(el);
        // _SMSTSLogPath not set → should log a warning

        new ActionSaveItems(data).Go();

        Assert.Contains(log.Messages, m => m.Contains("SMSTSLOG"));
    }

    [Fact]
    public void SMSTSLOG_CopiesSmstsDotLog()
    {
        // Create a fake ConfigMgr log dir with an smsts.log in it.
        var logDir = Path.Combine(_tempDir, "smslogdir");
        Directory.CreateDirectory(logDir);
        File.WriteAllText(Path.Combine(logDir, "smsts.log"), "log contents");
        File.WriteAllText(Path.Combine(logDir, "other.log"), "not copied");

        var dest = Path.Combine(_tempDir, "smstslog_dest");
        var el   = ActionTestData.ActionEl(
            $"""<Action Type="SaveItems" Path="{dest}" Items="SMSTSLOG" />""");
        var (env, _, data) = ActionTestData.Make(el);
        env.Set("_SMSTSLogPath", logDir);

        new ActionSaveItems(data).Go();

        Assert.True(File.Exists(Path.Combine(dest, "smsts.log")));
        Assert.False(File.Exists(Path.Combine(dest, "other.log")));
    }

    [Fact]
    public void SMSTSLOG_MultipleSmstsDotLog_AllCopied()
    {
        var logDir = Path.Combine(_tempDir, "smslogdir2");
        Directory.CreateDirectory(logDir);
        File.WriteAllText(Path.Combine(logDir, "smsts.log"),      "a");
        File.WriteAllText(Path.Combine(logDir, "smsts_1234.log"), "b");

        var dest = Path.Combine(_tempDir, "smstslog_multi");
        var el   = ActionTestData.ActionEl(
            $"""<Action Type="SaveItems" Path="{dest}" Items="SMSTSLOG" />""");
        var (env, _, data) = ActionTestData.Make(el);
        env.Set("_SMSTSLogPath", logDir);

        new ActionSaveItems(data).Go();

        Assert.True(File.Exists(Path.Combine(dest, "smsts.log")));
        Assert.True(File.Exists(Path.Combine(dest, "smsts_1234.log")));
    }

    // ── Glob / literal file copy ──────────────────────────────────────────

    [Fact]
    public void LiteralFile_CopiedToDestination()
    {
        var src = Path.Combine(_tempDir, "source.txt");
        File.WriteAllText(src, "hello");
        var dest = Path.Combine(_tempDir, "lit_dest");
        var el   = ActionTestData.ActionEl(
            $"""<Action Type="SaveItems" Path="{dest}" Items="{src}" />""");
        var (_, _, data) = ActionTestData.Make(el);

        new ActionSaveItems(data).Go();

        Assert.True(File.Exists(Path.Combine(dest, "source.txt")));
        Assert.Equal("hello", File.ReadAllText(Path.Combine(dest, "source.txt")));
    }

    [Fact]
    public void WildcardPattern_CopiesMatchingFiles()
    {
        var srcDir = Path.Combine(_tempDir, "srcdir");
        Directory.CreateDirectory(srcDir);
        File.WriteAllText(Path.Combine(srcDir, "a.log"), "1");
        File.WriteAllText(Path.Combine(srcDir, "b.log"), "2");
        File.WriteAllText(Path.Combine(srcDir, "c.txt"), "3");

        var dest    = Path.Combine(_tempDir, "glob_dest");
        var pattern = Path.Combine(srcDir, "*.log");
        var el      = ActionTestData.ActionEl(
            $"""<Action Type="SaveItems" Path="{dest}" Items="{pattern}" />""");
        var (_, _, data) = ActionTestData.Make(el);

        new ActionSaveItems(data).Go();

        Assert.True(File.Exists(Path.Combine(dest, "a.log")));
        Assert.True(File.Exists(Path.Combine(dest, "b.log")));
        Assert.False(File.Exists(Path.Combine(dest, "c.txt")));
    }

    [Fact]
    public void NonExistentSource_DoesNotThrow()
    {
        var dest = Path.Combine(_tempDir, "noex_dest");
        var el   = ActionTestData.ActionEl(
            $"""<Action Type="SaveItems" Path="{dest}" Items="C:\does\not\exist.txt" />""");
        var (_, _, data) = ActionTestData.Make(el);

        var ex = Record.Exception(() => new ActionSaveItems(data).Go());
        Assert.Null(ex);
    }

    // ── Multiple items ────────────────────────────────────────────────────

    [Fact]
    public void MultipleItems_CommaSeparated_AllProcessed()
    {
        var srcFile = Path.Combine(_tempDir, "multi_src.txt");
        File.WriteAllText(srcFile, "data");
        var dest = Path.Combine(_tempDir, "multi_dest");
        var el   = ActionTestData.ActionEl(
            $"""<Action Type="SaveItems" Path="{dest}" Items="TSVariables,{srcFile}" />""");
        var (env, _, data) = ActionTestData.Make(el);
        env.Set("Foo", "bar");

        new ActionSaveItems(data).Go();

        Assert.True(File.Exists(Path.Combine(dest, "UI++ Variable Dump.txt")));
        Assert.True(File.Exists(Path.Combine(dest, "multi_src.txt")));
    }

    [Fact]
    public void MultipleItems_SemicolonSeparated_AllProcessed()
    {
        var srcFile = Path.Combine(_tempDir, "semi_src.txt");
        File.WriteAllText(srcFile, "data");
        var dest = Path.Combine(_tempDir, "semi_dest");
        var el   = ActionTestData.ActionEl(
            $"""<Action Type="SaveItems" Path="{dest}" Items="TSVariables;{srcFile}" />""");
        var (env, _, data) = ActionTestData.Make(el);
        env.Set("Foo", "bar");

        new ActionSaveItems(data).Go();

        Assert.True(File.Exists(Path.Combine(dest, "UI++ Variable Dump.txt")));
        Assert.True(File.Exists(Path.Combine(dest, "semi_src.txt")));
    }

    // ── Guard conditions ──────────────────────────────────────────────────

    [Fact]
    public void EmptyPath_ReturnsNext_WithoutCreatingFiles()
    {
        var el = ActionTestData.ActionEl(
            """<Action Type="SaveItems" Path="" Items="TSVariables" />""");
        var (_, _, data) = ActionTestData.Make(el);

        var result = new ActionSaveItems(data).Go();

        Assert.Equal(ActionResult.Next, result);
    }

    [Fact]
    public void EmptyItems_ReturnsNext()
    {
        var dest = Path.Combine(_tempDir, "empty_items");
        var el   = ActionTestData.ActionEl(
            $"""<Action Type="SaveItems" Path="{dest}" Items="" />""");
        var (_, _, data) = ActionTestData.Make(el);

        var result = new ActionSaveItems(data).Go();

        Assert.Equal(ActionResult.Next, result);
        Assert.False(Directory.Exists(dest));
    }

    [Fact]
    public void Always_ReturnsNext()
    {
        var dest = Path.Combine(_tempDir, "always_next");
        var el   = ActionTestData.ActionEl(
            $"""<Action Type="SaveItems" Path="{dest}" Items="TSVariables" />""");
        var (_, _, data) = ActionTestData.Make(el);

        Assert.Equal(ActionResult.Next, new ActionSaveItems(data).Go());
    }
}
