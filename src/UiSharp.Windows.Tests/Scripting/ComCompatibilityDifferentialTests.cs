using UiSharp.Core.Scripting;
using UiSharp.Windows.Scripting;

namespace UiSharp.Windows.Tests.Scripting;

/// <summary>
/// Differential tests for the COM compatibility shim and the UiSharp-native
/// functions that replace it.
///
/// The shim exists so existing XML keeps working without the WinPE-Scripting
/// component — so every CreateObject expression it handles must agree with real
/// VBScript exactly. The native functions are the migration target and have no
/// VBScript counterpart; that asymmetry is asserted rather than glossed over,
/// because a config using them will not run under the original C++ UI++.
/// </summary>
public class ComCompatibilityDifferentialTests
{
    private static readonly NativeConditionEvaluator Native = new();

    private static bool Unavailable => !VBScriptConditionEvaluator.IsAvailable;

    private static bool VbCondition(string expr) =>
        EngineComparison.OnStaThread(
            () => new VBScriptConditionEvaluator().Evaluate(expr, EngineComparison.NoVars));

    private static string? VbValue(string expr) =>
        EngineComparison.OnStaThread(
            () => new VBScriptConditionEvaluator().TryEvaluateValue(expr, out var v) ? v : null);

    private static string? NativeValue(string expr) =>
        Native.TryEvaluateValue(expr, out var v) ? v : null;

    // -------------------------------------------------------------------------
    // The shim must be indistinguishable from the real thing
    // -------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(ComExpressions))]
    public void ComExpression_AgreesWithVBScript(string expression)
    {
        if (Unavailable) return;

        Assert.Equal(VbCondition(expression), Native.Evaluate(expression, EngineComparison.NoVars));
        Assert.Equal(VbValue(expression),     NativeValue(expression));
    }

    // The condition that actually appears in UI++5.xml. It used to be reported as
    // needing a script host; now it evaluates natively.
    [Fact]
    public void SampleConfigFileExistsCondition_EvaluatesNatively()
    {
        var expr = @"CreateObject(""Scripting.FileSystemObject"").FileExists(" +
                   @"""C:\Users\Jason\Desktop\ConfigMgr update notes.doc"")";

        var result = Native.TryEvaluate(expr, EngineComparison.NoVars);

        // Nothing blocked evaluation any more...
        Assert.True(result.IsReliable, result.DescribeProblems());
        // ...but the shim points at the modern replacement.
        Assert.Contains(result.Advice, a => a.Kind == ConditionDiagnosticKind.ComCompatibilityShim);

        if (!Unavailable) Assert.Equal(VbCondition(expr), result.Value);
    }

    // -------------------------------------------------------------------------
    // Native replacements must agree with the COM form they replace
    // -------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(NativeEquivalentPairs))]
    public void NativeFunction_MatchesTheComFormItReplaces(string native, string com)
    {
        // Same answer either way, which is what makes migrating safe.
        Assert.Equal(NativeValue(com), NativeValue(native));

        // And the COM form still matches real VBScript, so the chain holds:
        // vbscript == shim == native.
        if (!Unavailable) Assert.Equal(VbValue(com), NativeValue(native));
    }

    [Theory]
    [MemberData(nameof(NativeExpressions))]
    public void NativeFunction_IsNotVBScript(string native)
    {
        if (Unavailable) return;

        // These are UiSharp extensions. VBScript has never heard of them, so it
        // must decline — that is the documented cost of migrating off the shim.
        Assert.Null(VbValue(native));
    }

    [Theory]
    [MemberData(nameof(NativeExpressions))]
    public void NativeFunction_ReportsNoProblemsAndNoAdvice(string native)
    {
        var result = Native.TryEvaluate(native, EngineComparison.NoVars);

        Assert.True(result.IsReliable, result.DescribeProblems());
        // Already modern, so there is nothing to advise.
        Assert.Empty(result.Advice);
    }

    // -------------------------------------------------------------------------
    // What still needs a script host
    // -------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(ScriptHostExpressions))]
    public void ScriptHostConstruct_IsDeclinedNatively(string expression)
    {
        var result = Native.TryEvaluate(expression, EngineComparison.NoVars);

        // Blocking, so the condition is false rather than a wrong answer.
        Assert.False(result.IsReliable);
        Assert.False(result.Value);
        Assert.Null(NativeValue(expression));
    }

    [Fact]
    public void UnknownProgId_NamesTheProgIdItCannotProvide()
    {
        var result = Native.TryEvaluate(
            @"CreateObject(""Scripting.Dictionary"")", EngineComparison.NoVars);

        Assert.Contains(result.Problems,
            d => d.Kind == ConditionDiagnosticKind.RequiresComHost &&
                 d.Detail.Contains("Scripting.Dictionary"));
    }

    [Fact]
    public void UnimplementedMember_NamesTheObjectAndMember()
    {
        var result = Native.TryEvaluate(
            @"CreateObject(""WScript.Shell"").RegRead(""HKLM\Software"")",
            EngineComparison.NoVars);

        var problem = Assert.Single(result.Problems,
            d => d.Kind == ConditionDiagnosticKind.RequiresComHost);

        Assert.Contains("WScript.Shell", problem.Detail);
        Assert.Contains("RegRead", problem.Detail);
    }

    // -------------------------------------------------------------------------
    // Advisory diagnostics must never change the answer
    // -------------------------------------------------------------------------

    [Fact]
    public void CompatibilityAdvice_DoesNotFailTheCondition()
    {
        // A true condition that goes through the shim must stay true. If the
        // advisory were treated as blocking, every config using CreateObject
        // would start evaluating false.
        var expr = @"CreateObject(""Scripting.FileSystemObject"").FolderExists(""C:\Windows"")";
        var result = Native.TryEvaluate(expr, EngineComparison.NoVars);

        Assert.True(result.Value);
        Assert.True(result.IsReliable);
        Assert.NotEmpty(result.Advice);
    }

    [Fact]
    public void CompatibilityAdvice_NamesTheNativeReplacement()
    {
        var result = Native.TryEvaluate(
            @"CreateObject(""Scripting.FileSystemObject"").FileExists(""C:\x"")",
            EngineComparison.NoVars);

        var advice = Assert.Single(result.Advice);
        Assert.Contains("FileExists(path)", advice.Detail);
    }

    // -------------------------------------------------------------------------

    public static IEnumerable<object[]> ComExpressions() =>
        EngineCorpus.ComCompatibility.Select(e => new object[] { e });

    public static IEnumerable<object[]> NativeEquivalentPairs() =>
        EngineCorpus.NativeEquivalents.Select(p => new object[] { p.Native, p.Com });

    public static IEnumerable<object[]> NativeExpressions() =>
        EngineCorpus.NativeEquivalents.Select(p => new object[] { p.Native });

    public static IEnumerable<object[]> ScriptHostExpressions() =>
        EngineCorpus.RequireScriptHost.Select(e => new object[] { e });
}
