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

    // Requires the VBScript COM host: CreateObject, GetObject, Eval, Execute.
    RequiresComHost,

    // The engine stopped before consuming the whole expression, so part of the
    // condition was silently ignored.
    TrailingInput,

    // The expression parsed but could not be computed — division by zero, or
    // arithmetic on something that is not a number. VBScript raises a runtime
    // error for these, so reporting one keeps the two engines in step.
    EvaluationError,
}

public sealed record ConditionDiagnostic(ConditionDiagnosticKind Kind, string Detail)
{
    public override string ToString() => $"{Kind}: {Detail}";
}

// The outcome of evaluating a condition, plus anything the engine could not honour.
//
// A condition with diagnostics still yields a Value, but that value is a guess —
// the native engine substitutes an empty string for whatever it could not evaluate,
// which is falsy. Callers should surface diagnostics rather than trusting Value.
public readonly record struct ConditionResult(
    bool Value,
    IReadOnlyList<ConditionDiagnostic> Diagnostics)
{
    public static ConditionResult Ok(bool value) => new(value, []);

    // True when the engine handled every part of the expression.
    public bool IsReliable => Diagnostics.Count == 0;

    // One-line summary for logs. Empty when there is nothing to report.
    public string Describe() => Diagnostics.Count == 0
        ? string.Empty
        : string.Join("; ", Diagnostics.Select(d => d.ToString()));
}
