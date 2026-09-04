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

        if (log is null) return result.Value;

        if (!result.IsReliable)
        {
            var remedy = result.Problems.Any(
                d => d.Kind == ConditionDiagnosticKind.RequiresComHost)
                ? " Set ConditionEngine=\"vbscript\" (or pass /conditionengine:vbscript) to evaluate it."
                : " Rewrite the condition using constructs the native engine supports.";

            log.Write(
                $"Condition in {context} was not fully evaluated and has been treated as " +
                $"{result.Value}. {result.DescribeProblems()}.{remedy}",
                LogSeverity.Warning);
        }

        // Advice is informational: the condition evaluated correctly, it is just
        // written against the COM compatibility surface. Logged at Info so it is
        // discoverable in CMTrace without looking like a fault.
        if (result.Advice.Any())
        {
            log.Write(
                $"Condition in {context} used a compatibility construct. " +
                $"{result.DescribeAdvice()}.",
                LogSeverity.Info);
        }

        return result.Value;
    }
}
