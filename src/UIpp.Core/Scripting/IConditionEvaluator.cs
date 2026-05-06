namespace UIpp.Core.Scripting;

public interface IConditionEvaluator
{
    bool Evaluate(string expression, IReadOnlyDictionary<string, string> variables);
}
