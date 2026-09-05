using UiSharp.Core.Variables;
using UiSharp.Windows.Variables;

namespace UiSharp.Windows.Tests.Variables;

/// <summary>
/// Variable files must round-trip, and every <see cref="ITSEnv"/> must agree on
/// the format.
///
/// They did not. <c>ConfigMgrTSEnv.SaveToFile</c> was an alias for
/// <c>DumpToFile</c>, so the shipping runtime wrote <c>name=value</c> while
/// <c>LocalTSEnv</c> wrote the JSON the README documents — the same interface
/// method, two formats, and tests only on the implementation that never ships.
///
/// Worse, the enumeration was wrong: inside a task sequence <c>Set</c> writes to
/// the COM object and never touches the local dictionary, so saving wrote an
/// empty file precisely where the feature is meant to work.
/// </summary>
public class VariableFileTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "uisharp_varfile_" + Guid.NewGuid().ToString("N"));

    public VariableFileTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    private string PathFor(string name) => Path.Combine(_dir, name);

    // -------------------------------------------------------------------------
    // Both environments write the same thing
    // -------------------------------------------------------------------------

    [Fact]
    public void BothEnvironmentsSaveTheSameFormat()
    {
        var local = new LocalTSEnv(_ => null);
        var cm = new ConfigMgrTSEnv();

        foreach (var env in new ITSEnv[] { local, cm })
        {
            env.Set("Name", "WKS-001");
            env.Set("Dept", "Fire");
        }

        var localPath = PathFor("local.dat");
        var cmPath = PathFor("cm.dat");

        local.SaveToFile(localPath);
        cm.SaveToFile(cmPath);

        Assert.Equal(File.ReadAllText(localPath), File.ReadAllText(cmPath));
    }

    [Fact]
    public void SaveWritesJsonAsTheReadmeDocuments()
    {
        var env = new ConfigMgrTSEnv();
        env.Set("Name", "WKS-001");

        var path = PathFor("vars.dat");
        env.SaveToFile(path);

        var text = File.ReadAllText(path).TrimStart();
        Assert.StartsWith("{", text);
        Assert.Contains("\"Name\"", text);
    }

    // -------------------------------------------------------------------------
    // Round trip
    // -------------------------------------------------------------------------

    [Fact]
    public void VariablesSurviveASaveAndLoad()
    {
        var path = PathFor("roundtrip.dat");

        var saver = new ConfigMgrTSEnv();
        saver.Set("Name", "WKS-001");
        saver.Set("Dept", "Fire");
        saver.SaveToFile(path);

        var loader = new ConfigMgrTSEnv();
        loader.LoadFromFile(path);

        Assert.Equal("WKS-001", loader.Get("Name"));
        Assert.Equal("Fire", loader.Get("Dept"));
    }

    [Fact]
    public void AFileWrittenByOneEnvironmentLoadsIntoTheOther()
    {
        var path = PathFor("cross.dat");

        var saver = new LocalTSEnv(_ => null);
        saver.Set("Name", "WKS-001");
        saver.SaveToFile(path);

        var loader = new ConfigMgrTSEnv();
        loader.LoadFromFile(path);

        Assert.Equal("WKS-001", loader.Get("Name"));
    }

    // The reason for JSON over one line per variable: a value containing a
    // newline silently corrupts a line-based file, and the reload then takes
    // the remainder as a new variable name.
    [Fact]
    public void AValueContainingNewlinesSurvives()
    {
        var path = PathFor("multiline.dat");

        var saver = new ConfigMgrTSEnv();
        saver.Set("Body", "first line\nsecond line");
        saver.Set("After", "intact");
        saver.SaveToFile(path);

        var loader = new ConfigMgrTSEnv();
        loader.LoadFromFile(path);

        Assert.Equal("first line\nsecond line", loader.Get("Body"));
        Assert.Equal("intact", loader.Get("After"));
    }

    [Fact]
    public void AValueContainingAnEqualsSignSurvives()
    {
        var path = PathFor("equals.dat");

        var saver = new ConfigMgrTSEnv();
        saver.Set("Query", "SELECT * FROM x WHERE a=1");
        saver.SaveToFile(path);

        var loader = new ConfigMgrTSEnv();
        loader.LoadFromFile(path);

        Assert.Equal("SELECT * FROM x WHERE a=1", loader.Get("Query"));
    }

    // -------------------------------------------------------------------------
    // Exclusions
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("XHWMemory")]
    [InlineData("_SMSTSAdvertID")]
    public void CollectedAndTaskSequenceVariablesAreNotSaved(string name)
    {
        // X names are facts about the machine and _ names belong to the task
        // sequence; neither is the operator's data to carry between runs.
        var path = PathFor("excluded.dat");

        var env = new ConfigMgrTSEnv();
        env.Set(name, "value");
        env.Set("Keep", "kept");
        env.SaveToFile(path);

        var text = File.ReadAllText(path);
        Assert.DoesNotContain(name, text);
        Assert.Contains("Keep", text);
    }

    [Fact]
    public void ExcludedNamesInAFileAreNotLoaded()
    {
        var path = PathFor("excluded-load.dat");
        File.WriteAllText(path, """{"XHWMemory":"8192","Keep":"kept"}""");

        var env = new ConfigMgrTSEnv();
        env.LoadFromFile(path);

        Assert.Equal("", env.Get("XHWMemory"));
        Assert.Equal("kept", env.Get("Keep"));
    }

    // -------------------------------------------------------------------------
    // The dump is a separate, human-readable thing
    // -------------------------------------------------------------------------

    [Fact]
    public void DumpWritesOneLinePerVariableForReading()
    {
        var path = PathFor("dump.txt");

        var env = new ConfigMgrTSEnv();
        env.Set("Name", "WKS-001");
        env.DumpToFile(path);

        Assert.Equal(["Name=WKS-001"], File.ReadAllLines(path));
    }

    // -------------------------------------------------------------------------
    // Nothing here may throw during a deployment
    // -------------------------------------------------------------------------

    [Fact]
    public void LoadingAMissingFileIsSilent()
    {
        var env = new ConfigMgrTSEnv();
        env.LoadFromFile(PathFor("does-not-exist.dat"));

        Assert.Equal("", env.Get("Anything"));
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("{ truncated")]
    [InlineData("")]
    public void LoadingADamagedFileIsSilent(string content)
    {
        // A damaged variable file must not stop a deployment, and there is no
        // channel to report it on from here.
        var path = PathFor("damaged.dat");
        File.WriteAllText(path, content);

        var env = new ConfigMgrTSEnv();
        env.LoadFromFile(path);

        Assert.Equal("", env.Get("Anything"));
    }

    [Fact]
    public void LoadIsTolerantOfAnEmptyJsonObject() =>
        Assert.Empty(VariableFile.Load(WriteTemp("{}")));

    private string WriteTemp(string content)
    {
        var path = PathFor(Guid.NewGuid().ToString("N") + ".dat");
        File.WriteAllText(path, content);
        return path;
    }
}
