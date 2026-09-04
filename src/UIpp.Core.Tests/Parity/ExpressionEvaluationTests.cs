using System.Xml.Linq;
using UIpp.Core.Actions;
using UIpp.Core.Actions.Impl;
using UIpp.Core.Configuration;
using UIpp.Core.Dialogs;
using UIpp.Core.Logging;
using UIpp.Core.Scripting;
using UIpp.Core.Variables;

namespace UIpp.Core.Tests.Parity;

/// <summary>
/// Pins expression evaluation and the <c>DontEval</c> attribute to C++ UI++.
///
/// The original evaluates values through CScriptHost::Eval and keeps the result
/// only when the call succeeds and returns something non-empty
/// (Actions.cpp:393):
///
///     if (!dontEval &amp;&amp; SUCCEEDED(pScriptHost->Eval(variableValue, &amp;r))
///         &amp;&amp; r.vt > 0 &amp;&amp; ((_bstr_t)r).length() > 0)
///         variableValue = ((_bstr_t)r).GetBSTR();
///
/// The two DontEval defaults differ and that is not a typo: TSVar values and
/// Switch case variables default to FALSE (evaluate), while the Switch's own
/// OnValue defaults to TRUE (do not evaluate).
/// </summary>
public class ExpressionEvaluationTests
{
    private sealed class NullLog : ICMLog
    {
        public void Write(string msg, LogSeverity sev = LogSeverity.Info, string comp = "UIpp") { }
    }

    private static LocalTSEnv RunActions(string actionsXml, params (string k, string v)[] seed)
    {
        var env = new LocalTSEnv(_ => null);
        foreach (var (k, v) in seed) env.Set(k, v);

        var factory = new ActionFactory();
        factory.RegisterFromAssembly(typeof(ActionTSVar).Assembly);

        var actionsEl = XElement.Parse($"<Actions>{actionsXml}</Actions>");
        var data = new ActionData
        {
            ActionNode         = actionsEl,
            Conditions         = new NativeConditionEvaluator(),
            TsEnv              = env,
            Log                = new NullLog(),
            GlobalDialogTraits = new DialogTraits(),
        };

        new ActionProcessor(factory, new NativeConditionEvaluator()).Run(actionsEl, data);
        return env;
    }

    private static string? EvalValue(string expression) =>
        new NativeConditionEvaluator().TryEvaluateValue(expression, out var v) ? v : null;

    // -------------------------------------------------------------------------
    // TSVar — DontEval defaults to false, so values are evaluated
    // -------------------------------------------------------------------------

    [Fact]
    public void TSVar_QuotedValue_LosesItsQuotes()
    {
        // The case from UI++/UI++ (Logical Disks Snippet).xml:
        //   <Action Type="TSVar" Name="OSDTargetSystemDrive">"%VolumeChoice%"</Action>
        // The quotes make it a VBScript string literal, so the variable receives
        // C: — not "C:" with the quote characters still attached, which would
        // have broken the deployment.
        var env = RunActions(
            """<Action Type="TSVar" Name="Target">"%VolumeChoice%"</Action>""",
            ("VolumeChoice", "C:"));

        Assert.Equal("C:", env.Get("Target"));
    }

    [Fact]
    public void TSVar_Arithmetic_IsEvaluated()
    {
        var env = RunActions("""<Action Type="TSVar" Variable="Sum">1 + 1</Action>""");
        Assert.Equal("2", env.Get("Sum"));
    }

    [Fact]
    public void TSVar_FunctionCallAndConcatenation_AreEvaluated()
    {
        // Adapted from UI++.xml, which builds initials with Left(...) & Left(...).
        var env = RunActions(
            // '&' must be written &amp; in XML, as the real configs do.
            """<Action Type="TSVar" Variable="Initials">Left("%Domain%",2) &amp; Left("%User%",2)</Action>""",
            ("Domain", "CORP"), ("User", "abrown"));

        Assert.Equal("COab", env.Get("Initials"));
    }

    [Fact]
    public void TSVar_PlainText_SurvivesUnevaluated()
    {
        // "CTG" is not a valid VBScript expression, Eval fails, and the original
        // keeps the literal. This fallback is what stops evaluation from
        // mangling ordinary values.
        var env = RunActions("""<Action Type="TSVar" Variable="Suffix">CTG</Action>""");
        Assert.Equal("CTG", env.Get("Suffix"));
    }

    [Fact]
    public void TSVar_TextWithSpaces_SurvivesUnevaluated()
    {
        var env = RunActions(
            """<Action Type="TSVar" Variable="Msg">Please choose a volume</Action>""");
        Assert.Equal("Please choose a volume", env.Get("Msg"));
    }

    [Fact]
    public void TSVar_DontEvalTrue_KeepsTheLiteral()
    {
        // UI++2.xml sets DontEval="True" precisely because evaluation is the
        // default. Without it, a bare %SystemName% substituting to a name like
        // WKS-001 would be evaluated as the subtraction WKS - 001.
        var env = RunActions(
            """<Action Type="TSVar" Variable="Name" DontEval="True">"quoted"</Action>""");

        Assert.Equal("\"quoted\"", env.Get("Name"));
    }

    [Fact]
    public void TSVar_EmptyResult_KeepsTheLiteral()
    {
        // C++ requires the VARIANT to be non-empty before adopting it, so an
        // expression evaluating to "" leaves the original text in place.
        var env = RunActions("""<Action Type="TSVar" Variable="V">""</Action>""");
        Assert.Equal("\"\"", env.Get("V"));
    }

    // -------------------------------------------------------------------------
    // Switch — OnValue defaults to NOT evaluating
    // -------------------------------------------------------------------------

    [Fact]
    public void Switch_OnValue_IsNotEvaluatedByDefault()
    {
        // No DontEval attribute, so the switch value stays literal and the
        // regex is matched against the function-call text, which matches no case.
        var env = RunActions(
            """
            <Action Type="Switch" OnValue="Trim(&quot;%Gateway%&quot;)">
              <Case RegEx="^10\.0\.50\.1$"><Variable Name="Zone">Pacific</Variable></Case>
              <Default><Variable Name="Zone">Eastern</Variable></Default>
            </Action>
            """,
            ("Gateway", "10.0.50.1"));

        Assert.Equal("Eastern", env.Get("Zone"));
    }

    [Fact]
    public void Switch_OnValue_IsEvaluatedWhenDontEvalIsFalse()
    {
        // UI++3.xml opts in with DontEval="False", so Trim(...) runs and the
        // Case matches.
        var env = RunActions(
            """
            <Action Type="Switch" OnValue="Trim(&quot;%Gateway%&quot;)" DontEval="False">
              <Case RegEx="^10\.0\.50\.1$"><Variable Name="Zone">Pacific</Variable></Case>
              <Default><Variable Name="Zone">Eastern</Variable></Default>
            </Action>
            """,
            ("Gateway", " 10.0.50.1 "));

        Assert.Equal("Pacific", env.Get("Zone"));
    }

    [Fact]
    public void Switch_CaseVariable_IsEvaluatedByDefault()
    {
        var env = RunActions(
            """
            <Action Type="Switch" OnValue="abc">
              <Case RegEx="abc"><Variable Name="Out">"quoted"</Variable></Case>
            </Action>
            """);

        Assert.Equal("quoted", env.Get("Out"));
    }

    [Fact]
    public void Switch_CaseVariable_DontEvalTrue_KeepsTheLiteral()
    {
        var env = RunActions(
            """
            <Action Type="Switch" OnValue="abc">
              <Case RegEx="abc"><Variable Name="Out" DontEval="True">"quoted"</Variable></Case>
            </Action>
            """);

        Assert.Equal("\"quoted\"", env.Get("Out"));
    }

    // -------------------------------------------------------------------------
    // The value evaluator itself
    // -------------------------------------------------------------------------

    [Theory]
    // Arithmetic, with VBScript's precedence
    [InlineData("1 + 1", "2")]
    [InlineData("10 - 4", "6")]
    [InlineData("3 * 4", "12")]
    [InlineData("10 / 4", "2.5")]
    [InlineData("10 \\ 4", "2")]          // integer division
    [InlineData("10 Mod 3", "1")]
    [InlineData("2 ^ 10", "1024")]
    [InlineData("2 + 3 * 4", "14")]       // '*' binds tighter than '+'
    [InlineData("(2 + 3) * 4", "20")]
    [InlineData("-5 + 2", "-3")]          // unary minus
    [InlineData("2 ^ -1", "0.5")]         // '^' is right-associative through unary
    // Concatenation
    [InlineData("\"a\" & \"b\"", "ab")]
    [InlineData("1 & 2", "12")]           // '&' coerces to text
    [InlineData("\"1\" + \"2\"", "12")]   // '+' concatenates two strings
    [InlineData("\"1\" + 2", "3")]        // ... but adds when either side is a number
    // Functions
    [InlineData("UCase(\"abc\")", "ABC")]
    [InlineData("Trim(\"  x  \")", "x")]
    [InlineData("Len(\"abcd\")", "4")]
    [InlineData("Round(3.14159, 2)", "3.14")]
    [InlineData("Left(\"abcdef\", 3)", "abc")]
    // String literals — the case that matters most in practice
    [InlineData("\"C:\"", "C:")]
    [InlineData("\"already text\"", "already text")]
    // Comparisons still yield VBScript's textual booleans
    [InlineData("1 = 1", "True")]
    [InlineData("1 = 2", "False")]
    public void Expression_EvaluatesTo(string expression, string expected) =>
        Assert.Equal(expected, EvalValue(expression));

    [Theory]
    // Not expressions at all — the engine must decline so the literal survives
    [InlineData("Please choose a volume")]
    [InlineData("Adobe Reader DC 2019")]
    [InlineData("root\\cimv2")]
    // Genuine runtime errors, which VBScript also raises
    [InlineData("1 / 0")]
    [InlineData("1 Mod 0")]
    [InlineData("\"abc\" - 1")]
    // Needs a script host
    [InlineData("GetObject(\"winmgmts:\")")]
    [InlineData("Eval(\"1 = 1\")")]
    // A supported object, but an object reference is not something a
    // task-sequence variable can hold, so the engine declines.
    [InlineData("CreateObject(\"Scripting.FileSystemObject\")")]
    // Empty results are rejected, matching the VARIANT length check
    [InlineData("\"\"")]
    public void Expression_IsDeclined(string expression) =>
        Assert.Null(EvalValue(expression));

    [Fact]
    public void Expression_DeclinedForWhitespace()
    {
        Assert.Null(EvalValue(""));
        Assert.Null(EvalValue("   "));
    }

    // A bare identifier is an undefined variable to VBScript, which errors and
    // leaves the literal. The native engine returns the identifier text, so the
    // caller ends up with the same string either way.
    [Fact]
    public void BareIdentifier_YieldsTheSameTextEitherWay()
    {
        Assert.Equal("CTG", EvalValue("CTG"));
    }
}
