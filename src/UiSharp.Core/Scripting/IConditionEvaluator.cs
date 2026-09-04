namespace UiSharp.Core.Scripting;

public interface IConditionEvaluator
{
    bool Evaluate(string expression, IReadOnlyDictionary<string, string> variables);

    // Evaluates and additionally reports any part of the expression the engine
    // could not handle faithfully.
    //
    // The default implementation assumes the engine handled everything, which is
    // correct for the VBScript host (a real script engine) and for test doubles.
    // NativeConditionEvaluator overrides it to report its blind spots — without
    // that signal an unsupported construct is indistinguishable from a false
    // condition, which would silently take the wrong branch during a deployment.
    ConditionResult TryEvaluate(string expression, IReadOnlyDictionary<string, string> variables) =>
        ConditionResult.Ok(Evaluate(expression, variables));

    // Evaluates an expression for its VALUE rather than its truth, mirroring the
    // way the original uses one CScriptHost for both (Actions.cpp:393):
    //
    //     if (!dontEval && SUCCEEDED(pScriptHost->Eval(variableValue, &r))
    //         && r.vt > 0 && ((_bstr_t)r).length() > 0)
    //         variableValue = ((_bstr_t)r).GetBSTR();
    //
    // Returns false when the expression could not be evaluated or produced
    // nothing, in which case the caller MUST keep the literal text — that
    // fallback is what makes plain values like "CTG" survive unchanged.
    //
    // The default implementation declines, which leaves callers with the literal
    // and is the correct conservative answer for engines that cannot do this.
    bool TryEvaluateValue(string expression, out string value)
    {
        value = string.Empty;
        return false;
    }
}
