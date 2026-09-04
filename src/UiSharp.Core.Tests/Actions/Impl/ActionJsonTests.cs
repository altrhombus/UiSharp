using UiSharp.Core.Actions;
using UiSharp.Core.Actions.Impl;

namespace UiSharp.Core.Tests.Actions.Impl;

public class ActionToJsonTests
{
    [Fact]
    public void BuildsJsonFromAttributeChildren()
    {
        var el = ActionTestData.ActionEl("""
            <Action Type="ToJSON" Variable="Out">
              <Attribute Name="Site">CHI</Attribute>
              <Attribute Name="Role">Workstation</Attribute>
            </Action>
            """);
        var (env, _, data) = ActionTestData.Make(el);
        new ActionToJson(data).Go();

        var json = env.Get("Out");
        Assert.Contains("\"Site\"", json);
        Assert.Contains("\"CHI\"",  json);
        Assert.Contains("\"Role\"", json);
    }

    [Fact]
    public void DefaultVariable_IsJSONValue()
    {
        var el = ActionTestData.ActionEl("""<Action Type="ToJSON"><Attribute Name="k">v</Attribute></Action>""");
        var (env, _, data) = ActionTestData.Make(el);
        new ActionToJson(data).Go();
        Assert.True(env.Exists("JSONValue"));
    }

    [Fact]
    public void AttributeValue_VariableSubstituted()
    {
        var el = ActionTestData.ActionEl("""
            <Action Type="ToJSON" Variable="J">
              <Attribute Name="Name">%ComputerName%</Attribute>
            </Action>
            """);
        var (env, _, data) = ActionTestData.Make(el);
        env.Set("ComputerName", "PC001");
        new ActionToJson(data).Go();
        Assert.Contains("\"PC001\"", env.Get("J"));
    }

    [Fact]
    public void EmptyAttributes_ProducesEmptyObject()
    {
        var el = ActionTestData.ActionEl("""<Action Type="ToJSON" Variable="J" />""");
        var (env, _, data) = ActionTestData.Make(el);
        new ActionToJson(data).Go();
        Assert.Equal("{}", env.Get("J"));
    }
}

public class ActionFromJsonTests
{
    [Fact]
    public void SetsVariablesFromStringProperties()
    {
        var el = ActionTestData.ActionEl(
            """<Action Type="FromJSON">{"Site":"CHI","Role":"WKS"}</Action>""");
        var (env, _, data) = ActionTestData.Make(el);
        new ActionFromJson(data).Go();
        Assert.Equal("CHI", env.Get("Site"));
        Assert.Equal("WKS", env.Get("Role"));
    }

    [Fact]
    public void SkipsNonStringProperties_AndLogs()
    {
        var el = ActionTestData.ActionEl(
            """<Action Type="FromJSON">{"Str":"ok","Num":42}</Action>""");
        var (env, log, data) = ActionTestData.Make(el);
        new ActionFromJson(data).Go();
        Assert.Equal("ok", env.Get("Str"));
        Assert.False(env.Exists("Num"));
        Assert.Single(log.Messages); // warning logged
    }

    [Fact]
    public void InvalidJson_LogsWarningAndReturnsNext()
    {
        var el = ActionTestData.ActionEl("""<Action Type="FromJSON">not json</Action>""");
        var (_, log, data) = ActionTestData.Make(el);
        var result = new ActionFromJson(data).Go();
        Assert.Equal(ActionResult.Next, result);
        Assert.Single(log.Messages);
    }

    [Fact]
    public void EmptyContent_ReturnsNextWithoutSideEffects()
    {
        var el = ActionTestData.ActionEl("""<Action Type="FromJSON"></Action>""");
        var (_, _, data) = ActionTestData.Make(el);
        Assert.Equal(ActionResult.Next, new ActionFromJson(data).Go());
    }

    [Fact]
    public void RoundTrip_ToJsonThenFromJson()
    {
        // ToJSON
        var toEl = ActionTestData.ActionEl("""
            <Action Type="ToJSON" Variable="J">
              <Attribute Name="A">foo</Attribute>
              <Attribute Name="B">bar</Attribute>
            </Action>
            """);
        var (env, _, toData) = ActionTestData.Make(toEl);
        new ActionToJson(toData).Go();

        // FromJSON using the produced JSON
        var json  = env.Get("J");
        var fromEl = ActionTestData.ActionEl($"""<Action Type="FromJSON">{System.Security.SecurityElement.Escape(json)}</Action>""");
        var (env2, _, fromData) = ActionTestData.Make(fromEl);
        new ActionFromJson(fromData).Go();

        Assert.Equal("foo", env2.Get("A"));
        Assert.Equal("bar", env2.Get("B"));
    }
}
