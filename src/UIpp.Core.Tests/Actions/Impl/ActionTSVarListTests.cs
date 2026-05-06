using UIpp.Core.Actions;
using UIpp.Core.Actions.Impl;
using UIpp.Core.Dialogs;
using UIpp.Core.Logging;
using UIpp.Core.Scripting;
using UIpp.Core.Software;
using UIpp.Core.Variables;

namespace UIpp.Core.Tests.Actions.Impl;

public class ActionTSVarListTests
{
    private static ActionData MakeWithSoftware(
        System.Xml.Linq.XElement node,
        IReadOnlyDictionary<string, ISoftware> software)
    {
        var env = new LocalTSEnv();
        return new()
        {
            ActionNode         = node,
            Conditions         = new NativeConditionEvaluator(),
            TsEnv              = env,
            Log                = new NullLog(),
            GlobalDialogTraits = new DialogTraits(),
            Software           = software,
        };
    }

    [Fact]
    public void WritesNumberedApplicationVariables()
    {
        var el = ActionTestData.ActionEl("""
            <Action Type="TSVarList" ApplicationVariableBase="XApps">
              <SoftwareListRef Id="A1" />
              <SoftwareListRef Id="A2" />
            </Action>
            """);
        var sw = new Dictionary<string, ISoftware>(StringComparer.OrdinalIgnoreCase)
        {
            ["A1"] = new Application("A1", "App One", "", "CM App 1", "", "", 0),
            ["A2"] = new Application("A2", "App Two", "", "CM App 2", "", "", 1),
        };
        var data = MakeWithSoftware(el, sw);
        new ActionTSVarList(data).Go();

        var env = (LocalTSEnv)data.TsEnv;
        Assert.Equal("CM App 1", env.Get("XApps01"));
        Assert.Equal("CM App 2", env.Get("XApps02"));
    }

    [Fact]
    public void WritesNumberedPackageVariables()
    {
        var el = ActionTestData.ActionEl("""
            <Action Type="TSVarList" PackageVariableBase="XPkgs">
              <SoftwareListRef Id="P1" />
            </Action>
            """);
        var sw = new Dictionary<string, ISoftware>(StringComparer.OrdinalIgnoreCase)
        {
            ["P1"] = new Package("P1", "Pkg One", "", "ABC00001", "Install", "", "", 0),
        };
        var data = MakeWithSoftware(el, sw);
        new ActionTSVarList(data).Go();

        Assert.Equal("ABC00001", ((LocalTSEnv)data.TsEnv).Get("XPkgs001"));
    }

    [Fact]
    public void NullSoftwareMap_ReturnsNextSafely()
    {
        var el = ActionTestData.ActionEl("""<Action Type="TSVarList" ApplicationVariableBase="X" />""");
        var (_, _, data) = ActionTestData.Make(el);
        Assert.Equal(ActionResult.Next, new ActionTSVarList(data).Go());
    }

    [Fact]
    public void UnknownId_Skipped()
    {
        var el = ActionTestData.ActionEl("""
            <Action Type="TSVarList" ApplicationVariableBase="X">
              <SoftwareListRef Id="Missing" />
            </Action>
            """);
        var sw = new Dictionary<string, ISoftware>();
        var data = MakeWithSoftware(el, sw);
        new ActionTSVarList(data).Go();
        Assert.False(((LocalTSEnv)data.TsEnv).Exists("X01"));
    }
}
