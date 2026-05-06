using System.Xml.Linq;
using UIpp.Core.Actions;
using UIpp.Core.Dialogs;
using UIpp.Core.Logging;
using UIpp.Core.Scripting;
using UIpp.Core.Variables;

namespace UIpp.Core.Tests.Actions;

public class ActionProcessorTests
{
    // ------------------------------------------------------------------
    // Test infrastructure
    // ------------------------------------------------------------------

    private sealed class NullLog : ICMLog
    {
        public void Write(string message, LogSeverity severity = LogSeverity.Info, string component = "UIpp") { }
    }

    private sealed class AlwaysTrueEvaluator : IConditionEvaluator
    {
        public bool Evaluate(string expression, IReadOnlyDictionary<string, string> variables) => true;
    }

    private static ActionData MakeData(XElement node) => new()
    {
        ActionNode          = node,
        Conditions          = new AlwaysTrueEvaluator(),
        TsEnv               = new LocalTSEnv(),
        Log                 = new NullLog(),
        GlobalDialogTraits  = new DialogTraits(),
    };

    private static ActionFactory FactoryWith(params (string type, ActionResult result, bool isGui)[] entries)
    {
        var factory = new ActionFactory();
        foreach (var (type, result, isGui) in entries)
        {
            var r = result; var g = isGui;
            factory.Register(type, data => new LambdaAction(data, () => r, g));
        }
        return factory;
    }

    private sealed class LambdaAction(ActionData data, Func<ActionResult> go, bool isGuiAction)
        : ActionBase(data)
    {
        public override ActionResult Go() => go();
        public override bool IsGuiAction => isGuiAction;
    }

    private static ActionProcessor Processor(ActionFactory factory) =>
        new(factory, new AlwaysTrueEvaluator());

    // Builds an <Actions> element from an indented XML string of child elements.
    private static XElement Actions(string inner) =>
        XElement.Parse($"<Actions>{inner}</Actions>");

    // ------------------------------------------------------------------
    // Basic linear flow
    // ------------------------------------------------------------------

    [Fact]
    public void EmptyActions_ReturnsNext()
    {
        var result = Processor(new ActionFactory()).Run(Actions(""), MakeData(new XElement("dummy")));
        Assert.Equal(ActionResult.Next, result);
    }

    [Fact]
    public void SingleNext_ReturnsNext()
    {
        var factory = FactoryWith(("Step", ActionResult.Next, false));
        var result  = Processor(factory).Run(Actions("""<Action Type="Step"/>"""), MakeData(new XElement("dummy")));
        Assert.Equal(ActionResult.Next, result);
    }

    [Fact]
    public void Cancel_StopsAndReturnsCancel()
    {
        var callLog = new List<string>();
        var factory = new ActionFactory();
        factory.Register("A", d => new LambdaAction(d, () => { callLog.Add("A"); return ActionResult.Cancel; }, false));
        factory.Register("B", d => new LambdaAction(d, () => { callLog.Add("B"); return ActionResult.Next;   }, false));

        Processor(factory).Run(
            Actions("""<Action Type="A"/><Action Type="B"/>"""),
            MakeData(new XElement("dummy")));

        Assert.Equal(["A"], callLog);
    }

    [Fact]
    public void UnknownAction_SkippedWithWarning()
    {
        // No actions registered → factory returns null → should skip, not throw.
        var result = Processor(new ActionFactory()).Run(
            Actions("""<Action Type="Missing"/>"""),
            MakeData(new XElement("dummy")));
        Assert.Equal(ActionResult.Next, result);
    }

    // ------------------------------------------------------------------
    // ActionGroup
    // ------------------------------------------------------------------

    [Fact]
    public void ActionGroup_ChildrenExecuted()
    {
        var callLog = new List<string>();
        var factory = new ActionFactory();
        factory.Register("X", d => new LambdaAction(d, () => { callLog.Add("X"); return ActionResult.Next; }, false));

        Processor(factory).Run(
            Actions("""<ActionGroup Name="G"><Action Type="X"/></ActionGroup>"""),
            MakeData(new XElement("dummy")));

        Assert.Equal(["X"], callLog);
    }

    [Fact]
    public void ActionGroup_SiblingAfterGroup_AlsoRuns()
    {
        var callLog = new List<string>();
        var factory = new ActionFactory();
        factory.Register("Inner", d => new LambdaAction(d, () => { callLog.Add("Inner"); return ActionResult.Next; }, false));
        factory.Register("After", d => new LambdaAction(d, () => { callLog.Add("After"); return ActionResult.Next; }, false));

        Processor(factory).Run(
            Actions("""
                <ActionGroup Name="G"><Action Type="Inner"/></ActionGroup>
                <Action Type="After"/>
            """),
            MakeData(new XElement("dummy")));

        Assert.Equal(["Inner", "After"], callLog);
    }

    // ------------------------------------------------------------------
    // Back navigation
    // ------------------------------------------------------------------

    [Fact]
    public void Back_AfterNoGuiAction_IsIgnored()
    {
        // Back with no GUI history → treated as Next (allowBack is false).
        var factory = FactoryWith(("B", ActionResult.Back, true));
        var result  = Processor(factory).Run(Actions("""<Action Type="B"/>"""), MakeData(new XElement("dummy")));
        Assert.Equal(ActionResult.Next, result);
    }

    [Fact]
    public void Back_ReturnsToGuiAction()
    {
        // A (GUI, Next) → B (GUI, Back) → A re-runs (Next) → done
        var runCount = new Dictionary<string, int> { ["A"] = 0, ["B"] = 0 };
        var factory  = new ActionFactory();

        factory.Register("A", d => new LambdaAction(d, () =>
        {
            runCount["A"]++;
            return ActionResult.Next;
        }, isGuiAction: true));

        factory.Register("B", d => new LambdaAction(d, () =>
        {
            runCount["B"]++;
            // Go back first time, then next
            return runCount["B"] == 1 ? ActionResult.Back : ActionResult.Next;
        }, isGuiAction: true));

        Processor(factory).Run(
            Actions("""<Action Type="A"/><Action Type="B"/>"""),
            MakeData(new XElement("dummy")));

        Assert.Equal(2, runCount["A"]); // ran twice (once forward, once via back)
        Assert.Equal(2, runCount["B"]); // ran twice (once back, once next)
    }

    // ------------------------------------------------------------------
    // Refresh navigation
    // ------------------------------------------------------------------

    [Fact]
    public void Refresh_FromFirstChildOfGroup_IsIgnored()
    {
        // First child of group: allowRefresh = false → Refresh treated as Next.
        var factory = FactoryWith(("R", ActionResult.Refresh, true));
        var result  = Processor(factory).Run(
            Actions("""<ActionGroup Name="G"><Action Type="R"/></ActionGroup>"""),
            MakeData(new XElement("dummy")));
        Assert.Equal(ActionResult.Next, result);
    }

    [Fact]
    public void Refresh_FromSecondChildOfGroup_RerunsGroup()
    {
        // Group has two actions: A (non-GUI), B (GUI, Refresh once then Next).
        // B is not first child → allowRefresh = true → refresh re-runs A then B.
        var runCount = new Dictionary<string, int> { ["A"] = 0, ["B"] = 0 };
        var factory  = new ActionFactory();

        factory.Register("A", d => new LambdaAction(d, () =>
        {
            runCount["A"]++;
            return ActionResult.Next;
        }, isGuiAction: false));

        factory.Register("B", d => new LambdaAction(d, () =>
        {
            runCount["B"]++;
            return runCount["B"] == 1 ? ActionResult.Refresh : ActionResult.Next;
        }, isGuiAction: true));

        Processor(factory).Run(
            Actions("""<ActionGroup Name="G"><Action Type="A"/><Action Type="B"/></ActionGroup>"""),
            MakeData(new XElement("dummy")));

        Assert.Equal(2, runCount["A"]);
        Assert.Equal(2, runCount["B"]);
    }

    // ------------------------------------------------------------------
    // Condition evaluation
    // ------------------------------------------------------------------

    [Fact]
    public void FalseCondition_SkipsAction()
    {
        var callLog = new List<string>();
        var factory = new ActionFactory();
        factory.Register("X", d => new LambdaAction(d, () => { callLog.Add("X"); return ActionResult.Next; }, false));

        // Use a native evaluator; condition "'A' = 'B'" is false → action skipped.
        var proc = new ActionProcessor(factory, new NativeConditionEvaluator());
        proc.Run(
            Actions("""<Action Type="X" Condition="'A' = 'B'"/>"""),
            MakeData(new XElement("dummy")));

        Assert.Empty(callLog);
    }

    [Fact]
    public void TrueCondition_RunsAction()
    {
        var callLog = new List<string>();
        var factory = new ActionFactory();
        factory.Register("X", d => new LambdaAction(d, () => { callLog.Add("X"); return ActionResult.Next; }, false));

        var proc = new ActionProcessor(factory, new NativeConditionEvaluator());
        proc.Run(
            Actions("""<Action Type="X" Condition="'A' = 'A'"/>"""),
            MakeData(new XElement("dummy")));

        Assert.Equal(["X"], callLog);
    }

    [Fact]
    public void FalseGroupCondition_SkipsEntireGroup()
    {
        var callLog = new List<string>();
        var factory = new ActionFactory();
        factory.Register("X", d => new LambdaAction(d, () => { callLog.Add("X"); return ActionResult.Next; }, false));

        var proc = new ActionProcessor(factory, new NativeConditionEvaluator());
        proc.Run(
            Actions("""<ActionGroup Name="G" Condition="'A' = 'B'"><Action Type="X"/></ActionGroup>"""),
            MakeData(new XElement("dummy")));

        Assert.Empty(callLog);
    }
}
