using UIpp.Core.Actions.Impl;

namespace UIpp.Core.Tests.Actions.Impl;

public class ActionVarsTests
{
    [Fact]
    public void Save_ThenLoad_RoundTrip()
    {
        var path = Path.GetTempFileName();
        try
        {
            // Save
            var saveEl = ActionTestData.ActionEl(
                $"""<Action Type="Vars" Direction="Save" Filename="{path}" />""");
            var (env, _, saveData) = ActionTestData.Make(saveEl);
            env.Set("UserSite", "CHI");
            new ActionVars(saveData).Go();

            // Load into fresh env
            var loadEl = ActionTestData.ActionEl(
                $"""<Action Type="Vars" Direction="Load" Filename="{path}" />""");
            var (env2, _, loadData) = ActionTestData.Make(loadEl);
            new ActionVars(loadData).Go();

            Assert.Equal("CHI", env2.Get("UserSite"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void DefaultDirection_IsSave()
    {
        var path = Path.GetTempFileName();
        try
        {
            var el = ActionTestData.ActionEl($"""<Action Type="Vars" Filename="{path}" />""");
            var (env, _, data) = ActionTestData.Make(el);
            env.Set("A", "1");
            new ActionVars(data).Go();
            Assert.True(new FileInfo(path).Length > 0);
        }
        finally { File.Delete(path); }
    }
}

public class ActionRandomStringTests
{
    [Fact]
    public void GeneratesString_OfCorrectLength()
    {
        var el = ActionTestData.ActionEl("""<Action Type="RandomString" Length="10" Variable="RS" />""");
        var (env, _, data) = ActionTestData.Make(el);
        new ActionRandomString(data).Go();
        Assert.Equal(10, env.Get("RS").Length);
    }

    [Fact]
    public void GeneratedString_OnlyContainsAllowedChars()
    {
        const string allowed = "ABC123";
        var el = ActionTestData.ActionEl(
            $"""<Action Type="RandomString" AllowedChars="{allowed}" Length="20" Variable="RS" />""");
        var (env, _, data) = ActionTestData.Make(el);
        new ActionRandomString(data).Go();
        Assert.All(env.Get("RS"), c => Assert.Contains(c, allowed));
    }

    [Fact]
    public void DefaultLength_IsSix()
    {
        var el = ActionTestData.ActionEl("""<Action Type="RandomString" Variable="RS" />""");
        var (env, _, data) = ActionTestData.Make(el);
        new ActionRandomString(data).Go();
        Assert.Equal(6, env.Get("RS").Length);
    }

    [Fact]
    public void OutOfRangeLength_UsesDefault()
    {
        var el = ActionTestData.ActionEl("""<Action Type="RandomString" Length="999" Variable="RS" />""");
        var (env, _, data) = ActionTestData.Make(el);
        new ActionRandomString(data).Go();
        Assert.Equal(6, env.Get("RS").Length);
    }

    [Fact]
    public void TwoRuns_ProduceDifferentValues()
    {
        // Probabilistic: 36^6 ≈ 2 billion combinations; collision chance < 1 in a billion.
        var el  = ActionTestData.ActionEl("""<Action Type="RandomString" Length="8" Variable="RS" />""");
        var (env1, _, d1) = ActionTestData.Make(el);
        var (env2, _, d2) = ActionTestData.Make(el);
        new ActionRandomString(d1).Go();
        new ActionRandomString(d2).Go();
        Assert.NotEqual(env1.Get("RS"), env2.Get("RS"));
    }
}
