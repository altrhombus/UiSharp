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
/// Diagnostics are only useful if they reach the log an administrator actually
/// reads after a deployment goes wrong. These tests run real actions through the
/// real processor and assert on what was written.
/// </summary>
public class ConditionDiagnosticLoggingTests
{
    private sealed class CapturingLog : ICMLog
    {
        public List<(LogSeverity Severity, string Message)> Entries { get; } = [];

        public void Write(string msg, LogSeverity sev = LogSeverity.Info, string comp = "UIpp") =>
            Entries.Add((sev, msg));

        public IEnumerable<string> Warnings =>
            Entries.Where(e => e.Severity == LogSeverity.Warning).Select(e => e.Message);
    }

    private static (LocalTSEnv env, CapturingLog log) Run(string actionsXml)
    {
        var env = new LocalTSEnv();
        var log = new CapturingLog();

        var factory = new ActionFactory();
        factory.RegisterFromAssembly(typeof(ActionTSVar).Assembly);

        var actionsEl = XElement.Parse($"<Actions>{actionsXml}</Actions>");
        var data = new ActionData
        {
            ActionNode         = actionsEl,
            Conditions         = new NativeConditionEvaluator(),
            TsEnv              = env,
            Log                = log,
            GlobalDialogTraits = new DialogTraits(),
        };

        new ActionProcessor(factory, new NativeConditionEvaluator()).Run(actionsEl, data);
        return (env, log);
    }

    // ----------------------------------------------------------
    // Action-level conditions
    // ----------------------------------------------------------

    // GetObject reaches WMI and has no native equivalent, unlike
    // CreateObject("Scripting.FileSystemObject") which the compatibility shim now
    // handles without a script host.
    [Fact]
    public void ActionCondition_RequiringCom_IsSkippedButWarnsWithRemedy()
    {
        var (env, log) = Run("""
            <Action Type="TSVar" Variable="Ran"
                    Condition="GetObject('winmgmts:root\cimv2') = 1">yes</Action>
            """);

        // The action is skipped, exactly as before this change...
        Assert.True(string.IsNullOrEmpty(env.Get("Ran")));

        // ...but no longer silently.
        var warning = Assert.Single(log.Warnings, w => w.Contains("GetObject"));
        Assert.Contains("vbscript", warning);
    }

    // The compatibility shim evaluates successfully, so it must not raise a
    // warning — only an informational note naming the native replacement, so an
    // administrator can migrate the XML when they choose to.
    [Fact]
    public void ActionCondition_UsingCompatibilityShim_InformsButDoesNotWarn()
    {
        var (_, log) = Run("""
            <Action Type="TSVar" Variable="Ran"
                    Condition="CreateObject('Scripting.FileSystemObject').FolderExists('C:\Windows')">yes</Action>
            """);

        Assert.Empty(log.Warnings);
        Assert.Contains(log.Entries,
            e => e.Severity == LogSeverity.Info && e.Message.Contains("FolderExists(path)"));
    }

    [Fact]
    public void ActionCondition_WithTypo_Warns()
    {
        var (_, log) = Run("""
            <Action Type="TSVar" Variable="X" Condition="UCse('a') = 'A'">v</Action>
            """);

        Assert.Contains(log.Warnings, w => w.Contains("UCse"));
    }

    // A condition the native engine fully understands must not generate noise —
    // a warning on every skipped action would train admins to ignore the log.
    [Theory]
    [InlineData("'A' = 'B'")]
    [InlineData("'A' = 'A'")]
    [InlineData("InStr('WKS-001', 'SRV') > 0")]
    [InlineData("1 = 2")]
    public void SupportedCondition_LogsNoWarning(string condition)
    {
        var (_, log) = Run(
            $"""<Action Type="TSVar" Variable="X" Condition="{condition}">v</Action>""");

        Assert.Empty(log.Warnings);
    }

    [Fact]
    public void ActionWithNoCondition_LogsNoWarning()
    {
        var (env, log) = Run("""<Action Type="TSVar" Variable="X">v</Action>""");

        Assert.Equal("v", env.Get("X"));
        Assert.Empty(log.Warnings);
    }

    // ----------------------------------------------------------
    // Preflight and input-field conditions
    // ----------------------------------------------------------

    [Fact]
    public void PreflightCheckCondition_RequiringCom_Warns()
    {
        var log = new CapturingLog();
        var env = new LocalTSEnv();
        var cond = new NativeConditionEvaluator();

        var actionEl = XElement.Parse("""
            <Action Type="Preflight">
              <Check Text="Marker file present"
                     CheckCondition="GetObject('winmgmts:root\cimv2') = 1" />
            </Action>
            """);

        var checks  = PreflightEvaluator.ParseChecks(actionEl, env, cond, log);
        var results = PreflightEvaluator.Evaluate(checks, cond, env, log);

        // The check fails, and the log says the check was never really evaluated.
        Assert.Equal(PreflightStatus.Fail, Assert.Single(results).Status);
        var warning = Assert.Single(log.Warnings);
        Assert.Contains("Marker file present", warning);
        Assert.Contains("vbscript", warning);
    }

    [Fact]
    public void PreflightWarnCondition_RequiringCom_Warns()
    {
        var log = new CapturingLog();
        var env = new LocalTSEnv();
        var cond = new NativeConditionEvaluator();

        var actionEl = XElement.Parse("""
            <Action Type="Preflight">
              <Check Text="Memory" CheckCondition="2048 &gt;= 1024"
                     WarnCondition="GetObject('winmgmts:') = 'x'" />
            </Action>
            """);

        var checks  = PreflightEvaluator.ParseChecks(actionEl, env, cond, log);
        var results = PreflightEvaluator.Evaluate(checks, cond, env, log);

        Assert.Equal(PreflightStatus.Warn, Assert.Single(results).Status);
        Assert.Contains(log.Warnings, w => w.Contains("WarnCondition"));
    }

    [Fact]
    public void PreflightChecks_WithSupportedConditions_LogNoWarnings()
    {
        var log = new CapturingLog();
        var env = new LocalTSEnv();
        var cond = new NativeConditionEvaluator();

        var actionEl = XElement.Parse("""
            <Action Type="Preflight">
              <Check Text="Memory"  CheckCondition="2048 &gt;= 1024" WarnCondition="2048 &gt;= 4096" />
              <Check Text="Battery" CheckCondition="'False' = 'False'" />
            </Action>
            """);

        var checks  = PreflightEvaluator.ParseChecks(actionEl, env, cond, log);
        PreflightEvaluator.Evaluate(checks, cond, env, log);

        Assert.Empty(log.Warnings);
    }

    [Fact]
    public void InputFieldCondition_RequiringCom_Warns()
    {
        var log = new CapturingLog();
        var actionEl = XElement.Parse("""
            <Action Type="Input">
              <InputText Question="Name" Variable="Name"
                         Condition="GetObject('winmgmts:root\cimv2') = 1" />
            </Action>
            """);

        var specs = InputFieldParser.Parse(
            actionEl, new LocalTSEnv(), new NativeConditionEvaluator(), log);

        // Field is filtered out — and the log explains why that may be wrong.
        Assert.Empty(specs);
        Assert.Contains(log.Warnings, w => w.Contains("InputText"));
    }

    [Fact]
    public void ChoiceCondition_RequiringCom_Warns()
    {
        var log = new CapturingLog();
        var actionEl = XElement.Parse("""
            <Action Type="Input">
              <InputChoice Question="Pick" Variable="Pick">
                <Choice Option="A" Condition="Eval('1 = 1')" />
                <Choice Option="B" />
              </InputChoice>
            </Action>
            """);

        var specs = InputFieldParser.Parse(
            actionEl, new LocalTSEnv(), new NativeConditionEvaluator(), log);

        Assert.Single(specs);
        Assert.Contains(log.Warnings, w => w.Contains("<Choice>"));
    }

    // Parsers are callable without a log (gUI# does this); that must not throw.
    [Fact]
    public void Parsers_WithoutLog_DoNotThrow()
    {
        var actionEl = XElement.Parse("""
            <Action Type="Input">
              <InputText Question="Name" Variable="Name" Condition="GetObject('X')" />
            </Action>
            """);

        var specs = InputFieldParser.Parse(
            actionEl, new LocalTSEnv(), new NativeConditionEvaluator());

        Assert.Empty(specs);
    }
}
