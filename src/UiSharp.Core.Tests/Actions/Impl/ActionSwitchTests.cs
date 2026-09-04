using UiSharp.Core.Actions;
using UiSharp.Core.Actions.Impl;

namespace UiSharp.Core.Tests.Actions.Impl;

public class ActionSwitchTests
{
    private ActionResult Run(string xml, params (string k, string v)[] vars)
    {
        var el = ActionTestData.ActionEl(xml);
        var (env, _, data) = ActionTestData.Make(el);
        foreach (var (k, v) in vars) env.Set(k, v);
        return new ActionSwitch(data).Go();
    }

    [Fact]
    public void MatchingCase_SetsVariables()
    {
        var el  = ActionTestData.ActionEl("""
            <Action Type="Switch" OnValue="LAPTOP">
              <Case RegEx="LAPTOP">
                <Variable Name="ChassisType">Laptop</Variable>
              </Case>
              <Case RegEx="DESKTOP">
                <Variable Name="ChassisType">Desktop</Variable>
              </Case>
            </Action>
            """);
        var (env, _, data) = ActionTestData.Make(el);
        new ActionSwitch(data).Go();
        Assert.Equal("Laptop", env.Get("ChassisType"));
    }

    [Fact]
    public void NoMatch_UsesDefault()
    {
        var el = ActionTestData.ActionEl("""
            <Action Type="Switch" OnValue="UNKNOWN">
              <Case RegEx="LAPTOP"><Variable Name="T">Laptop</Variable></Case>
              <Default><Variable Name="T">Other</Variable></Default>
            </Action>
            """);
        var (env, _, data) = ActionTestData.Make(el);
        new ActionSwitch(data).Go();
        Assert.Equal("Other", env.Get("T"));
    }

    [Fact]
    public void CaseInsensitiveMatch()
    {
        var el = ActionTestData.ActionEl("""
            <Action Type="Switch" OnValue="laptop">
              <Case RegEx="LAPTOP" CaseInsensitive="True">
                <Variable Name="R">yes</Variable>
              </Case>
            </Action>
            """);
        var (env, _, data) = ActionTestData.Make(el);
        new ActionSwitch(data).Go();
        Assert.Equal("yes", env.Get("R"));
    }

    [Fact]
    public void OnValue_VariableSubstituted()
    {
        var el = ActionTestData.ActionEl("""
            <Action Type="Switch" OnValue="%HWType%">
              <Case RegEx="Laptop"><Variable Name="R">laptop</Variable></Case>
            </Action>
            """);
        var (env, _, data) = ActionTestData.Make(el);
        env.Set("HWType", "Laptop");
        new ActionSwitch(data).Go();
        Assert.Equal("laptop", env.Get("R"));
    }

    [Fact]
    public void FirstMatchWins_SecondCaseIgnored()
    {
        var el = ActionTestData.ActionEl("""
            <Action Type="Switch" OnValue="ABC">
              <Case RegEx=".*"><Variable Name="R">first</Variable></Case>
              <Case RegEx=".*"><Variable Name="R">second</Variable></Case>
            </Action>
            """);
        var (env, _, data) = ActionTestData.Make(el);
        new ActionSwitch(data).Go();
        Assert.Equal("first", env.Get("R"));
    }

    [Fact]
    public void Returns_Next()
    {
        Assert.Equal(ActionResult.Next,
            Run("""<Action Type="Switch" OnValue="X" />"""));
    }
}
