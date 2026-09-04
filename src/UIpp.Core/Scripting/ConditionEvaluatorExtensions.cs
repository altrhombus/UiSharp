using UIpp.Core.Logging;

namespace UIpp.Core.Scripting;

// The single place where condition diagnostics become log entries, so every call
// site reports the engine's blind spots identically.
public static class ConditionEvaluatorExtensions
{
    private static readonly IReadOnlyDictionary<string, string> NoVars =
        new Dictionary<string, string>();

    /// <summary>
    /// Evaluates a condition and logs a warning for anything the engine could not
    /// handle faithfully.
    /// </summary>
    /// <param name="context">
    /// Where the condition came from — an action type or element name. Appears in
    /// the log so the offending line in the config can be found.
    /// </param>
    public static bool EvaluateLogged(
        this IConditionEvaluator evaluator,
        string expression,
        ICMLog? log,
        string context)
    {
        var result = evaluator.TryEvaluate(expression, NoVars);

        if (log is not null && !result.IsReliable)
        {
            var advice = result.Diagnostics.Any(
                d => d.Kind == ConditionDiagnosticKind.RequiresComHost)
                ? " Set ConditionEngine=\"vbscript\" (or pass /conditionengine:vbscript) to evaluate it."
                : " Rewrite the condition using constructs the native engine supports.";

            log.Write(
                $"Condition in {context} was not fully evaluated and has been treated as " +
                $"{result.Value}. {result.Describe()}.{advice}",
                LogSeverity.Warning);
        }

        return result.Value;
    }
}
