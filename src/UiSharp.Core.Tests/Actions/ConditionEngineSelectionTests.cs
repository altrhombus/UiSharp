using System.Xml.Linq;
using UiSharp.Core.Actions;
using UiSharp.Core.Actions.Impl;
using UiSharp.Core.Configuration;
using UiSharp.Core.Dialogs;
using UiSharp.Core.Logging;
using UiSharp.Core.Scripting;
using UiSharp.Core.Variables;

namespace UiSharp.Core.Tests.Actions;

/// <summary>
/// ConditionEngine is a whole-document setting on the root element.
///
/// It used to look per-action: ActionProcessor read the attribute off each
/// action and had an optional VBScript evaluator to honour it, but nothing ever
/// supplied that evaluator. The attribute was therefore ignored, the condition
/// went to the native engine, and — because the native engine fails closed on
/// constructs needing a script host — it evaluated false. A config asking for
/// VBScript silently took the wrong branch.
///
/// Per-action selection could not have worked anyway: ActionData.Conditions is
/// set once, so an action's own condition could use one engine while every
/// condition inside it used another.
/// </summary>
public class ConditionEngineSelectionTests
{
    private sealed class CapturingLog : ICMLog
    {
        public List<(LogSeverity Severity, string Message)> Entries { get; } = [];

        public void Write(string msg, LogSeverity sev = LogSeverity.Info,
                          string comp = LogFile.DefaultComponent) =>
            Entries.Add((sev, msg));

        public IEnumerable<string> Warnings =>
            Entries.Where(e => e.Severity == LogSeverity.Warning).Select(e => e.Message);
    }

    /// <summary>Records every expression it is asked about, and says yes to all.</summary>
    private sealed class RecordingEvaluator : IConditionEvaluator
    {
        public List<string> Seen { get; } = [];

        public bool Evaluate(string expression, IReadOnlyDictionary<string, string> variables)
        {
            Seen.Add(expression);
            return true;
        }
    }

    private static (LocalTSEnv Env, CapturingLog Log, RecordingEvaluator Engine) Run(string actionsXml)
    {
        var env    = new LocalTSEnv(_ => null);
        var log    = new CapturingLog();
        var engine = new RecordingEvaluator();

        var factory = new ActionFactory();
        factory.RegisterFromAssembly(typeof(ActionTSVar).Assembly);

        var actionsEl = XElement.Parse($"<Actions>{actionsXml}</Actions>");
        var data = new ActionData
        {
            ActionNode         = actionsEl,
            Conditions         = engine,
            TsEnv              = env,
            Log                = log,
            GlobalDialogTraits = new DialogTraits(),
        };

        new ActionProcessor(factory, engine).Run(actionsEl, data);
        return (env, log, engine);
    }

    // -------------------------------------------------------------------------
    // One engine, used for everything
    // -------------------------------------------------------------------------

    [Fact]
    public void TheConfiguredEngineEvaluatesEveryCondition()
    {
        var (env, _, engine) = Run("""
            <Action Type="TSVar" Variable="A" Condition="first">1</Action>
            <Action Type="TSVar" Variable="B" Condition="second">2</Action>
            """);

        Assert.Equal(["first", "second"], engine.Seen);
        Assert.Equal("1", env.Get("A"));
        Assert.Equal("2", env.Get("B"));
    }

    // -------------------------------------------------------------------------
    // A per-action attribute is reported, not obeyed and not ignored
    // -------------------------------------------------------------------------

    [Fact]
    public void PerActionConditionEngine_IsReported()
    {
        var (_, log, _) = Run("""
            <Action Type="TSVar" Variable="A" ConditionEngine="vbscript"
                    Condition="x">1</Action>
            """);

        var warning = Assert.Single(log.Warnings, w => w.Contains("ConditionEngine"));
        Assert.Contains("whole-document", warning);
        Assert.Contains("TSVar", warning);
    }

    [Fact]
    public void PerActionConditionEngine_DoesNotChangeTheEngineUsed()
    {
        // The document's engine still sees the condition; nothing is diverted
        // to a second evaluator that may not exist.
        var (_, _, engine) = Run("""
            <Action Type="TSVar" Variable="A" ConditionEngine="vbscript"
                    Condition="the condition">1</Action>
            """);

        Assert.Equal(["the condition"], engine.Seen);
    }

    [Fact]
    public void PerActionConditionEngine_OnAGroup_IsAlsoReported()
    {
        var (_, log, _) = Run("""
            <ActionGroup Name="G" ConditionEngine="vbscript">
              <Action Type="TSVar" Variable="A">1</Action>
            </ActionGroup>
            """);

        Assert.Contains(log.Warnings, w => w.Contains("ConditionEngine") && w.Contains("ActionGroup"));
    }

    [Fact]
    public void NoConditionEngineAttribute_IsSilent()
    {
        // The warning must only fire when a config actually asks for something
        // that will not happen.
        var (_, log, _) = Run("""<Action Type="TSVar" Variable="A" Condition="x">1</Action>""");

        Assert.DoesNotContain(log.Warnings, w => w.Contains("ConditionEngine"));
    }

    // Even asking for the engine that is already in use is worth reporting: the
    // attribute is in the wrong place, and saying nothing teaches the author
    // that per-action selection works.
    [Fact]
    public void PerActionConditionEngine_IsReportedEvenWhenItNamesTheNativeEngine()
    {
        var (_, log, _) = Run("""
            <Action Type="TSVar" Variable="A" ConditionEngine="native">1</Action>
            """);

        Assert.Contains(log.Warnings, w => w.Contains("ConditionEngine"));
    }

    // -------------------------------------------------------------------------
    // The document-level setting still parses
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("""<UIpp ConditionEngine="vbscript"><Actions /></UIpp>""", "vbscript")]
    [InlineData("""<UIpp ConditionEngine="native"><Actions /></UIpp>""", "native")]
    [InlineData("""<UIpp><Actions /></UIpp>""", "native")]
    public void RootConditionEngine_IsWhatTheRuntimeReads(string xml, string expected) =>
        Assert.Equal(expected, ConfigLoader.LoadFromXml(xml).ConditionEngine);
}
