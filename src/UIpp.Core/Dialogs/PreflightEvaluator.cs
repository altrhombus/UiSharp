using System.Xml.Linq;
using UIpp.Core.Configuration;
using UIpp.Core.Scripting;
using UIpp.Core.Variables;

namespace UIpp.Core.Dialogs;

public static class PreflightEvaluator
{
    private static readonly IReadOnlyDictionary<string, string> EmptyVars =
        new Dictionary<string, string>();

    // Parses <Check> children of the action node, skipping those whose Condition is false.
    public static IReadOnlyList<PreflightCheckSpec> ParseChecks(
        XElement actionNode, ITSEnv env, IConditionEvaluator conditions)
    {
        var result = new List<PreflightCheckSpec>();

        foreach (var el in actionNode.Elements(XmlConstants.Elements.PreflightCheck))
        {
            var condition = (string?)el.Attribute(XmlConstants.Attributes.Condition) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(condition) &&
                !conditions.Evaluate(env.Substitute(condition), EmptyVars))
                continue;

            result.Add(new PreflightCheckSpec
            {
                Text             = env.Substitute((string?)el.Attribute(XmlConstants.Attributes.Text)             ?? string.Empty),
                Description      = env.Substitute((string?)el.Attribute(XmlConstants.Attributes.Description)      ?? string.Empty),
                ErrorDescription = env.Substitute((string?)el.Attribute(XmlConstants.Attributes.ErrorDescription) ?? string.Empty),
                WarnDescription  = env.Substitute((string?)el.Attribute(XmlConstants.Attributes.WarnDescription)  ?? string.Empty),
                CheckCondition   =                 (string?)el.Attribute(XmlConstants.Attributes.CheckCondition)  ?? string.Empty,
                WarnCondition    =                 (string?)el.Attribute(XmlConstants.Attributes.WarnCondition)   ?? string.Empty,
            });
        }

        return result;
    }

    // Evaluates each check's CheckCondition and WarnCondition against the current env.
    public static IReadOnlyList<PreflightResult> Evaluate(
        IEnumerable<PreflightCheckSpec> checks,
        IConditionEvaluator conditions,
        ITSEnv env)
    {
        var results = new List<PreflightResult>();

        foreach (var check in checks)
        {
            var checkExpr   = env.Substitute(check.CheckCondition);
            bool checkPassed = string.IsNullOrWhiteSpace(checkExpr) ||
                               conditions.Evaluate(checkExpr, EmptyVars);

            PreflightStatus status;
            if (!checkPassed)
            {
                status = PreflightStatus.Fail;
            }
            else
            {
                var warnExpr    = env.Substitute(check.WarnCondition);
                bool warnPassed = string.IsNullOrWhiteSpace(warnExpr) ||
                                  conditions.Evaluate(warnExpr, EmptyVars);
                status = warnPassed ? PreflightStatus.Pass : PreflightStatus.Warn;
            }

            results.Add(new PreflightResult(check, status));
        }

        return results;
    }

    public static bool AnyFailed(IEnumerable<PreflightResult> results) =>
        results.Any(r => r.Status == PreflightStatus.Fail);

    public static bool AnyWarned(IEnumerable<PreflightResult> results) =>
        results.Any(r => r.Status == PreflightStatus.Warn);
}
