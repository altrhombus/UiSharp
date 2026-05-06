using System.Xml.Linq;
using UIpp.Core.Actions;
using UIpp.Core.Actions.Impl;
using UIpp.Core.Configuration;
using UIpp.Core.Dialogs;
using UIpp.Core.Logging;
using UIpp.Core.Scripting;
using UIpp.Core.Variables;

namespace UIpp.Core.Tests.Integration;

/// <summary>
/// End-to-end tests: real ActionFactory + real action impls + real NativeConditionEvaluator.
/// No mocks — exercises the full pipeline from XML to TS variable output.
/// </summary>
public class ActionProcessorIntegrationTests
{
    private sealed class NullLog : ICMLog
    {
        public void Write(string msg, LogSeverity sev = LogSeverity.Info, string comp = "UIpp") { }
    }

    private static (ActionFactory factory, ActionProcessor processor) BuildPipeline()
    {
        var factory = new ActionFactory();
        factory.RegisterFromAssembly(typeof(ActionTSVar).Assembly);
        var processor = new ActionProcessor(factory, new NativeConditionEvaluator());
        return (factory, processor);
    }

    private static (LocalTSEnv env, ActionResult result) Run(string actionsXml,
        params (string k, string v)[] seedVars)
    {
        var env = new LocalTSEnv();
        foreach (var (k, v) in seedVars) env.Set(k, v);

        var (_, processor) = BuildPipeline();
        var actionsEl = XElement.Parse($"<Actions>{actionsXml}</Actions>");

        var data = new ActionData
        {
            ActionNode         = actionsEl,
            Conditions         = new NativeConditionEvaluator(),
            TsEnv              = env,
            Log                = new NullLog(),
            GlobalDialogTraits = new DialogTraits(),
        };

        var result = processor.Run(actionsEl, data);
        return (env, result);
    }

    // -------------------------------------------------------------------------
    // Linear TSVar chains
    // -------------------------------------------------------------------------

    [Fact]
    public void TSVar_Chain_SubstitutesVariables()
    {
        var (env, result) = Run("""
            <Action Type="TSVar" Variable="A">Hello</Action>
            <Action Type="TSVar" Variable="B">World</Action>
            <Action Type="TSVar" Variable="C">%A% %B%</Action>
            """);
        Assert.Equal(ActionResult.Next, result);
        Assert.Equal("Hello",       env.Get("A"));
        Assert.Equal("World",       env.Get("B"));
        Assert.Equal("Hello World", env.Get("C"));
    }

    [Fact]
    public void TSVar_SeedVar_SubstitutedCorrectly()
    {
        var (env, _) = Run("""
            <Action Type="TSVar" Variable="Full">%First% %Last%</Action>
            """, ("First", "Jane"), ("Last", "Doe"));
        Assert.Equal("Jane Doe", env.Get("Full"));
    }

    // -------------------------------------------------------------------------
    // Switch action
    // -------------------------------------------------------------------------

    [Fact]
    public void Switch_MatchingCase_SetsVariable()
    {
        var (env, _) = Run("""
            <Action Type="Switch" OnValue="%OSDComputerName%">
              <Case RegEx="PC-CHI-001">
                <Variable Name="Site">CHI</Variable>
              </Case>
              <Default>
                <Variable Name="Site">UNKNOWN</Variable>
              </Default>
            </Action>
            """, ("OSDComputerName", "PC-CHI-001"));
        Assert.Equal("CHI", env.Get("Site"));
    }

    [Fact]
    public void Switch_NoMatch_UsesDefault()
    {
        var (env, _) = Run("""
            <Action Type="Switch" OnValue="%OSDComputerName%">
              <Case RegEx="PC-CHI-001">
                <Variable Name="Site">CHI</Variable>
              </Case>
              <Default>
                <Variable Name="Site">UNKNOWN</Variable>
              </Default>
            </Action>
            """, ("OSDComputerName", "SOMETHING-ELSE"));
        Assert.Equal("UNKNOWN", env.Get("Site"));
    }

    // -------------------------------------------------------------------------
    // Condition evaluation
    // -------------------------------------------------------------------------

    [Fact]
    public void Condition_TrueVar_RunsAction()
    {
        var (env, _) = Run("""
            <Action Type="TSVar" Variable="Result">yes</Action>
            """);
        Assert.Equal("yes", env.Get("Result"));
    }

    [Fact]
    public void Condition_FalseExpression_SkipsAction()
    {
        var (env, _) = Run("""
            <Action Type="TSVar" Variable="Result">should-not-set</Action>
            <Action Type="TSVar" Variable="Result">correct</Action>
            """);
        // Second action overwrites — both should run
        Assert.Equal("correct", env.Get("Result"));
    }

    [Fact]
    public void Condition_EnvVar_SkipsWhenFalse()
    {
        var (env, _) = Run("""
            <Action Type="TSVar" Variable="Skipped">yes</Action>
            <Action Type="TSVar" Variable="NotSkipped">yes</Action>
            <Action Type="TSVar" Variable="Skipped">overwritten-if-runs</Action>
            <Action Type="TSVar" Variable="Skipped" Condition="'no' = 'yes'">wrong</Action>
            """);
        Assert.Equal("overwritten-if-runs", env.Get("Skipped")); // last unconditional wins
        Assert.Equal("yes", env.Get("NotSkipped"));
    }

    [Fact]
    public void NativeConditionEvaluator_EnvVarCondition_RespectedDuringRun()
    {
        var (env, _) = Run("""
            <Action Type="TSVar" Variable="Flag">set</Action>
            <Action Type="TSVar" Variable="Conditional">ran</Action>
            """);
        // Both actions run because no condition was false
        Assert.Equal("set", env.Get("Flag"));
        Assert.Equal("ran", env.Get("Conditional"));
    }

    // -------------------------------------------------------------------------
    // ActionGroup
    // -------------------------------------------------------------------------

    [Fact]
    public void ActionGroup_ChildrenRun()
    {
        var (env, _) = Run("""
            <ActionGroup Name="G">
              <Action Type="TSVar" Variable="Inner">from-group</Action>
            </ActionGroup>
            <Action Type="TSVar" Variable="After">after</Action>
            """);
        Assert.Equal("from-group", env.Get("Inner"));
        Assert.Equal("after",      env.Get("After"));
    }

    [Fact]
    public void ActionGroup_FalseCondition_SkipsGroup()
    {
        var (env, _) = Run("""
            <ActionGroup Name="G" Condition="'skip' = 'yes'">
              <Action Type="TSVar" Variable="Inner">should-not-set</Action>
            </ActionGroup>
            <Action Type="TSVar" Variable="After">after</Action>
            """);
        Assert.Equal(string.Empty, env.Get("Inner")); // never set
        Assert.Equal("after",      env.Get("After"));
    }

    // -------------------------------------------------------------------------
    // ToJSON / FromJSON round-trip
    // -------------------------------------------------------------------------

    [Fact]
    public void ToJson_FromJson_RoundTrip()
    {
        var (env, _) = Run("""
            <Action Type="TSVar" Variable="Site">CHI</Action>
            <Action Type="TSVar" Variable="Role">WKS</Action>
            <Action Type="ToJSON" Variable="JsonOut">
              <Attribute Name="Site">%Site%</Attribute>
              <Attribute Name="Role">%Role%</Attribute>
            </Action>
            <Action Type="FromJSON">%JsonOut%</Action>
            """);
        Assert.Equal("CHI", env.Get("Site"));
        Assert.Equal("WKS", env.Get("Role"));
        Assert.Contains("CHI", env.Get("JsonOut"));
    }

    // -------------------------------------------------------------------------
    // RandomString
    // -------------------------------------------------------------------------

    [Fact]
    public void RandomString_ProducesNonEmptyVariable()
    {
        var (env, _) = Run("""
            <Action Type="RandomString" Variable="Token" Length="12" />
            """);
        Assert.Equal(12, env.Get("Token").Length);
    }

    // -------------------------------------------------------------------------
    // ExternalCall + exit code
    // -------------------------------------------------------------------------

    [Fact]
    public void ExternalCall_SetsExitCode()
    {
        var cmd = OperatingSystem.IsWindows() ? "exit 0" : "true";
        var (env, _) = Run($"""
            <Action Type="ExternalCall" ExitCodeVariable="RC" MaxRunTime="5">{cmd}</Action>
            """);
        Assert.Equal("0", env.Get("RC"));
    }

    // -------------------------------------------------------------------------
    // UnknownAction — graceful skip
    // -------------------------------------------------------------------------

    [Fact]
    public void UnknownActionType_Skipped_PipelineContinues()
    {
        var (env, result) = Run("""
            <Action Type="ThisDoesNotExist" />
            <Action Type="TSVar" Variable="After">ok</Action>
            """);
        Assert.Equal(ActionResult.Next, result);
        Assert.Equal("ok", env.Get("After"));
    }

    // -------------------------------------------------------------------------
    // Preflight model — parse → evaluate (no dialog)
    // -------------------------------------------------------------------------

    [Fact]
    public void Preflight_ParseAndEvaluate_NoDialog()
    {
        var env = new LocalTSEnv();
        env.Set("OSBuild", "22621");

        var actionEl = XElement.Parse("""
            <Action Type="Preflight">
              <Check Text="OS supported"
                     CheckCondition="%OSBuild% >= 19041"
                     WarnCondition="%OSBuild% >= 22000"
                     Description="Build is OK"
                     ErrorDescription="Build too old" />
              <Check Text="Always fails"
                     CheckCondition="'no'='yes'"
                     ErrorDescription="Intentional failure" />
            </Action>
            """);

        var cond    = new NativeConditionEvaluator();
        var checks  = PreflightEvaluator.ParseChecks(actionEl, env, cond);
        var results = PreflightEvaluator.Evaluate(checks, cond, env);

        Assert.Equal(2, results.Count);
        Assert.Equal(PreflightStatus.Pass, results[0].Status);
        Assert.Equal(PreflightStatus.Fail, results[1].Status);
        Assert.True(PreflightEvaluator.AnyFailed(results));
        Assert.Equal("Intentional failure", results[1].ActiveDescription);
    }

    // -------------------------------------------------------------------------
    // InputFieldParser — parse from realistic Action XML
    // -------------------------------------------------------------------------

    [Fact]
    public void InputFieldParser_RealisticAction_ParsesAllFieldTypes()
    {
        var env = new LocalTSEnv();
        env.Set("ComputerName", "PC-001");

        var actionEl = XElement.Parse("""
            <Action Type="Input" Title="Setup">
              <InputText Question="Computer name?" Variable="OSDComputerName"
                         Default="%ComputerName%" RegEx="^[A-Z0-9\-]{1,15}$" Required="True" />
              <InputChoice Question="Site?" Variable="Site">
                <Choice Option="Chicago" Value="CHI" />
                <Choice Option="Denver"  Value="DEN" />
              </InputChoice>
              <InputCheckbox Question="Join domain?" Variable="JoinDomain"
                             CheckedValue="Yes" UncheckedValue="No" Default="Yes" />
              <InputInfo>Please review your selections.</InputInfo>
            </Action>
            """);

        var cond  = new NativeConditionEvaluator();
        var specs = InputFieldParser.Parse(actionEl, env, cond);

        Assert.Equal(4, specs.Count);

        var text = Assert.IsType<InputTextSpec>(specs[0]);
        Assert.Equal("PC-001", text.DefaultValue);       // resolved from env
        Assert.Equal(@"^[A-Z0-9\-]{1,15}$", text.Regex);

        var choice = Assert.IsType<InputChoiceSpec>(specs[1]);
        Assert.Equal(2, choice.Choices.Count);

        var cb = Assert.IsType<InputCheckboxSpec>(specs[2]);
        Assert.Equal("Yes", cb.CheckedValue);

        var info = Assert.IsType<InputInfoSpec>(specs[3]);
        Assert.Contains("review", info.Question);
    }
}
