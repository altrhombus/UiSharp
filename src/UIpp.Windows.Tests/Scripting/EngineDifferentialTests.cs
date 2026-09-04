using UIpp.Core.Scripting;
using UIpp.Windows.Scripting;

namespace UIpp.Windows.Tests.Scripting;

/// <summary>
/// Differential tests: every expression is evaluated by the native engine and by
/// the real VBScript host (vbscript.dll), and the two must agree.
///
/// This is the evidence base for retiring the VBScript engine. The native engine
/// is already the default, so any disagreement is a case where a config that
/// worked under C++ UI++ would behave differently under UiSharp — which for a
/// drop-in replacement is a bug, not a preference.
///
/// Four disagreements were found and fixed this way: string comparison and
/// InStr/Replace were case-insensitive where VBScript compares binary; True and
/// False parsed as truthy identifiers rather than keywords; and expressions the
/// engine could not evaluate returned their truthy parse leftovers instead of
/// failing closed.
///
/// Skipped rather than failed when vbscript.dll is absent, so the suite still
/// runs on a machine without the Scripting component.
/// </summary>
public class EngineDifferentialTests
{
    private static readonly NativeConditionEvaluator Native = new();

    private static bool Unavailable => !VBScriptConditionEvaluator.IsAvailable;

    // Each VBScript evaluation gets a fresh engine on an STA thread, matching how
    // UIpp.exe (an [STAThread] entry point) uses it.
    private static bool VbCondition(string expr) =>
        EngineComparison.OnStaThread(
            () => new VBScriptConditionEvaluator().Evaluate(expr, EngineComparison.NoVars));

    private static string? VbValue(string expr) =>
        EngineComparison.OnStaThread(
            () => new VBScriptConditionEvaluator().TryEvaluateValue(expr, out var v) ? v : null);

    private static string? NativeValue(string expr) =>
        Native.TryEvaluateValue(expr, out var v) ? v : null;

    // -------------------------------------------------------------------------
    // Conditions
    // -------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(DeterministicExpressions))]
    public void Condition_AgreesWithVBScript(string expression)
    {
        if (Unavailable) return;

        var vb = VbCondition(expression);
        var na = Native.Evaluate(expression, EngineComparison.NoVars);

        Assert.Equal(vb, na);
    }

    // -------------------------------------------------------------------------
    // Values
    // -------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(DeterministicExpressions))]
    public void Value_AgreesWithVBScript(string expression)
    {
        if (Unavailable) return;

        var vb = VbValue(expression);
        var na = NativeValue(expression);

        Assert.Equal(vb, na);
    }

    // -------------------------------------------------------------------------
    // Text that is not an expression must be declined by BOTH engines, because
    // that is what preserves plain values like "Adobe Reader DC 2019" verbatim.
    // -------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(NonExpressions))]
    public void PlainText_IsDeclinedByBothEngines(string expression)
    {
        if (Unavailable) return;

        Assert.Null(VbValue(expression));
        Assert.Null(NativeValue(expression));
    }

    [Theory]
    [MemberData(nameof(NonExpressions))]
    public void PlainText_IsFalseAsAConditionInBothEngines(string expression)
    {
        if (Unavailable) return;

        Assert.False(VbCondition(expression));
        Assert.False(Native.Evaluate(expression, EngineComparison.NoVars));
    }

    // -------------------------------------------------------------------------
    // Runtime errors — VBScript raises, so both must decline and read false.
    // -------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(RuntimeErrorExpressions))]
    public void RuntimeError_IsDeclinedByBothEngines(string expression)
    {
        if (Unavailable) return;

        Assert.Null(VbValue(expression));
        Assert.Null(NativeValue(expression));
    }

    // -------------------------------------------------------------------------
    // Clock- and locale-dependent functions cannot be compared by value, but
    // both engines must at least agree on whether they produce one.
    // -------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(NonDeterministicExpressions))]
    public void DateAndTimeFunctions_BothProduceAValue(string expression)
    {
        if (Unavailable) return;

        Assert.NotNull(VbValue(expression));
        Assert.NotNull(NativeValue(expression));
    }

    // -------------------------------------------------------------------------
    // A guard on the harness itself: if VBScript were silently unavailable these
    // tests would all trivially pass, so make that visible.
    // -------------------------------------------------------------------------

    [Fact]
    public void VBScriptEngine_IsAvailableOnThisMachine()
    {
        Assert.True(VBScriptConditionEvaluator.IsAvailable,
            "vbscript.dll is not registered, so the differential tests did not " +
            "actually compare anything. They are skipped rather than failed, but " +
            "no parity evidence was produced on this run.");
    }

    // -------------------------------------------------------------------------

    public static IEnumerable<object[]> DeterministicExpressions() =>
        EngineCorpus.Deterministic.Select(e => new object[] { e });

    public static IEnumerable<object[]> NonExpressions() =>
        EngineCorpus.NotExpressions.Select(e => new object[] { e });

    public static IEnumerable<object[]> RuntimeErrorExpressions() =>
        EngineCorpus.RuntimeErrors.Select(e => new object[] { e });

    public static IEnumerable<object[]> NonDeterministicExpressions() =>
        EngineCorpus.NonDeterministic.Select(e => new object[] { e });
}
