using UIpp.Core.Actions;
using UIpp.Core.Actions.Impl;

namespace UIpp.Core.Tests.Actions.Impl;

public class ActionTSVarTests
{
    [Fact]
    public void SetsVariable_FromElementContent()
    {
        var el = ActionTestData.ActionEl("""<Action Type="TSVar" Variable="Site">CHI</Action>""");
        var (env, _, data) = ActionTestData.Make(el);
        new ActionTSVar(data).Go();
        Assert.Equal("CHI", env.Get("Site"));
    }

    [Fact]
    public void SetsVariable_FromNameAttribute()
    {
        var el = ActionTestData.ActionEl("""<Action Type="TSVar" Name="Color">Blue</Action>""");
        var (env, _, data) = ActionTestData.Make(el);
        new ActionTSVar(data).Go();
        Assert.Equal("Blue", env.Get("Color"));
    }

    [Fact]
    public void Value_VariableSubstituted()
    {
        var el = ActionTestData.ActionEl("""<Action Type="TSVar" Variable="Full">%First%-%Last%</Action>""");
        var (env, _, data) = ActionTestData.Make(el);
        env.Set("First", "John");
        env.Set("Last",  "Doe");
        new ActionTSVar(data).Go();
        Assert.Equal("John-Doe", env.Get("Full"));
    }

    [Fact]
    public void VariableName_Substituted()
    {
        var el = ActionTestData.ActionEl("""<Action Type="TSVar" Variable="%DynName%">Value</Action>""");
        var (env, _, data) = ActionTestData.Make(el);
        env.Set("DynName", "TargetVar");
        new ActionTSVar(data).Go();
        Assert.Equal("Value", env.Get("TargetVar"));
    }

    [Fact]
    public void Returns_Next()
    {
        var el = ActionTestData.ActionEl("""<Action Type="TSVar" Variable="X">v</Action>""");
        var (_, _, data) = ActionTestData.Make(el);
        Assert.Equal(ActionResult.Next, new ActionTSVar(data).Go());
    }
}
