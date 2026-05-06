using UIpp.Core.Variables;

namespace UIpp.Core.Tests.Variables;

public class LocalTSEnvTests
{
    private static LocalTSEnv Make(params (string k, string v)[] vars)
    {
        var env = new LocalTSEnv();
        foreach (var (k, v) in vars) env.Set(k, v);
        return env;
    }

    [Fact]
    public void Get_ReturnsEmptyForMissing()
    {
        var env = new LocalTSEnv();
        Assert.Equal(string.Empty, env.Get("MISSING"));
    }

    [Fact]
    public void Set_Get_RoundTrip()
    {
        var env = Make(("MyVar", "Hello"));
        Assert.Equal("Hello", env.Get("MyVar"));
    }

    [Fact]
    public void SetULong_StoredAsString()
    {
        var env = new LocalTSEnv();
        env.Set("Num", 42UL);
        Assert.Equal("42", env.Get("Num"));
    }

    [Fact]
    public void Exists_TrueAndFalse()
    {
        var env = Make(("A", "1"));
        Assert.True(env.Exists("A"));
        Assert.False(env.Exists("B"));
    }

    [Fact]
    public void InTS_AlwaysFalse() => Assert.False(new LocalTSEnv().InTS);

    [Fact]
    public void Substitute_NoPercent_ReturnsSame()
    {
        var env = new LocalTSEnv();
        Assert.Equal("hello world", env.Substitute("hello world"));
    }

    [Fact]
    public void Substitute_KnownVar_Replaced()
    {
        var env = Make(("Name", "VALUE"));
        Assert.Equal("prefix-VALUE-suffix", env.Substitute("prefix-%Name%-suffix"));
    }

    [Fact]
    public void Substitute_CaseInsensitive()
    {
        var env = Make(("myvar", "X"));
        Assert.Equal("X", env.Substitute("%MYVAR%"));
    }

    [Fact]
    public void Substitute_UnknownVar_LeftAsIs()
    {
        var env = new LocalTSEnv();
        Assert.Equal("%UNKNOWN%", env.Substitute("%UNKNOWN%"));
    }

    [Fact]
    public void Substitute_MultipleVars()
    {
        var env = Make(("A", "1"), ("B", "2"));
        Assert.Equal("1+2", env.Substitute("%A%+%B%"));
    }

    [Fact]
    public void SaveLoad_RoundTrip()
    {
        var path = Path.GetTempFileName();
        try
        {
            var env = Make(("UserChoice", "Laptop"), ("XAutoVar", "skip"), ("_Internal", "skip"));
            env.SaveToFile(path);

            var env2 = new LocalTSEnv();
            env2.LoadFromFile(path);

            Assert.Equal("Laptop", env2.Get("UserChoice"));
            Assert.Equal(string.Empty, env2.Get("XAutoVar"));
            Assert.Equal(string.Empty, env2.Get("_Internal"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void DumpToFile_WritesPlainText()
    {
        var path = Path.GetTempFileName();
        try
        {
            var env = Make(("Site", "CHI"), ("XSkip", "no"));
            env.DumpToFile(path);
            var text = File.ReadAllText(path);
            Assert.Contains("Site=CHI", text);
            Assert.DoesNotContain("XSkip", text);
        }
        finally { File.Delete(path); }
    }
}
