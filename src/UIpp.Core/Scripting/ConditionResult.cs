namespace UIpp.Core.Scripting;

// Why a condition could not be evaluated faithfully by the engine that ran it.
public enum ConditionDiagnosticKind
{
    // Called a function the engine does not implement. Often a typo in the config,
    // but may also be a VBScript built-in the native engine has not replicated.
    UnknownFunction,

    // A construct the engine recognises but cannot represent — e.g. Split(),
    // which returns an array, or member access on an object.
    UnsupportedConstruct,

    // Requires the VBScript COM host: GetObject, Eval, Execute, or a ProgID with
    // no native equivalent.
    RequiresComHost,

    // The engine stopped before consuming the whole expression, so part of the
    // condition was silently ignored.
    TrailingInput,

    // The expression parsed but could not be computed — division by zero, or
    // arithmetic on something that is not a number. VBScript raises a runtime
    // error for these, so reporting one keeps the two engines in step.
    EvaluationError,

    // The expression was evaluated correctly through the COM compatibility
    // shim, and a UiSharp-native equivalent exists. Advisory only — nothing is
    // wrong with the config, but it is written against the old surface.
    ComCompatibilityShim,
}

/// <summary>
/// Whether a diagnostic prevented evaluation or is merely advice.
/// </summary>
public enum ConditionDiagnosticSeverity
{
    /// <summary>
    /// The engine could not evaluate this faithfully. The condition is false and
    /// no value is produced, matching VBScript raising an error.
    /// </summary>
    Blocking,

    /// <summary>
    /// The expression evaluated correctly; this is guidance, such as a pointer to
    /// a native replacement for a compatibility construct. Must never change the
    /// result, or configs relying on the shim would break.
    /// </summary>
    Advisory,
}

public sealed record ConditionDiagnostic(
    ConditionDiagnosticKind Kind,
    string Detail,
    ConditionDiagnosticSeverity Severity = ConditionDiagnosticSeverity.Blocking)
{
    public bool IsBlocking => Severity == ConditionDiagnosticSeverity.Blocking;

    public override string ToString() => $"{Kind}: {Detail}";
}

// The outcome of evaluating a condition, plus anything the engine could not honour.
//
// A condition with blocking diagnostics still yields a Value, but that value is
// not meaningful — the engine substitutes an empty string for whatever it could
// not evaluate. Callers should surface diagnostics rather than trusting Value.
public readonly record struct ConditionResult(
    bool Value,
    IReadOnlyList<ConditionDiagnostic> Diagnostics)
{
    public static ConditionResult Ok(bool value) => new(value, []);

    /// <summary>
    /// True when nothing prevented evaluation. Advisory diagnostics do not make a
    /// result unreliable — the expression was computed correctly.
    /// </summary>
    public bool IsReliable => !Diagnostics.Any(d => d.IsBlocking);

    /// <summary>Advisory diagnostics only — migration guidance, not problems.</summary>
    public IEnumerable<ConditionDiagnostic> Advice =>
        Diagnostics.Where(d => !d.IsBlocking);

    public IEnumerable<ConditionDiagnostic> Problems =>
        Diagnostics.Where(d => d.IsBlocking);

    // One-line summary for logs. Empty when there is nothing to report.
    public string Describe() => Diagnostics.Count == 0
        ? string.Empty
        : string.Join("; ", Diagnostics.Select(d => d.ToString()));

    public string DescribeProblems() =>
        string.Join("; ", Problems.Select(d => d.ToString()));

    public string DescribeAdvice() =>
        string.Join("; ", Advice.Select(d => d.ToString()));
}
