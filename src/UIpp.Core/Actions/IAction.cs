using System.Xml.Linq;
using UIpp.Core.Configuration;
using UIpp.Core.Scripting;

namespace UIpp.Core.Actions;

public interface IAction
{
    ActionResult Go();
    bool IsGuiAction { get; }
}

public abstract class ActionBase(ActionData data) : IAction
{
    protected readonly ActionData Data = data;

    protected static readonly IReadOnlyDictionary<string, string> NoVars =
        new Dictionary<string, string>();

    public abstract ActionResult Go();
    public virtual bool IsGuiAction => false;

    // Attribute helpers -------------------------------------------------------
    //
    // These mirror C++ GetXMLAttribute (UI++/Actions/IAction.cpp:21), which is the
    // contract every action in the original reads attributes through:
    //
    //     attributeValue = node.attribute(attrName).value();
    //     if (attributeValue.GetLength() > 0 && raw == false)
    //         return CTSEnv::Instance().VariableSubstitute(attributeValue);
    //     else if (attributeValue.GetLength() > 0) return attributeValue;
    //     else if (defaultValue != NULL && ...) return defaultValue;
    //     else return _T("");
    //
    // Three behaviours follow from that and are deliberately reproduced here:
    //   1. Variables ARE substituted, because `raw` defaults to false. Only
    //      CheckCondition and WarnCondition are ever read raw in the original
    //      (InteractiveActions.cpp:222,230); Condition is read raw and then
    //      substituted at evaluation time, which comes to the same thing.
    //   2. The default value is NOT substituted — it is a compile-time constant
    //      in the original, never a variable reference.
    //   3. Emptiness is judged on the RAW value, so an attribute that is present
    //      but empty falls back to the default rather than yielding "".

    /// <summary>
    /// Reads an attribute and substitutes variables in its value. This is the
    /// default because it is what the original does for every attribute.
    /// </summary>
    protected string Attr(XElement el, string name, string? def = null)
    {
        var raw = RawAttr(el, name);
        return raw.Length > 0 ? Data.TsEnv.Substitute(raw) : def ?? string.Empty;
    }

    protected string Attr(string name, string? def = null) =>
        Attr(Data.ActionNode, name, def);

    /// <summary>
    /// Reads an attribute without substituting variables — the equivalent of
    /// passing <c>raw=true</c> in the original. Correct only for values that are
    /// substituted later by whoever consumes them, such as condition
    /// expressions; using it anywhere else diverges from C++ UI++.
    /// </summary>
    protected static string RawAttr(XElement el, string name) =>
        (string?)el.Attribute(name) ?? string.Empty;

    protected string RawAttr(string name) => RawAttr(Data.ActionNode, name);

    // C++ tests truthiness with FTW::IsTrue(GetXMLAttribute(...)), so the value
    // is substituted before the comparison and Required="%Flag%" works.
    protected bool BoolAttr(XElement el, string name, bool def = false)
    {
        var v = Attr(el, name);
        if (string.IsNullOrWhiteSpace(v)) return def;
        return v.Equals("true", StringComparison.OrdinalIgnoreCase)
            || v.Equals("yes",  StringComparison.OrdinalIgnoreCase)
            || v == "1";
    }

    protected bool BoolAttr(string name, bool def = false) =>
        BoolAttr(Data.ActionNode, name, def);

    // Expression helpers ------------------------------------------------------

    /// <summary>
    /// Evaluates <paramref name="value"/> as an expression and returns the result,
    /// or the input unchanged when the engine declines.
    /// </summary>
    /// <remarks>
    /// Mirrors the C++ pattern repeated for TSVar values, Switch OnValue and
    /// Switch case variables (Actions.cpp:393, 761, 845):
    ///
    ///     if (!dontEval && SUCCEEDED(Eval(v, &amp;r)) && r.vt > 0 && len > 0)
    ///         v = r;
    ///
    /// Keeping the literal on failure is what lets plain text like "CTG" pass
    /// through untouched while "%Var%" in quotes resolves to its contents.
    /// </remarks>
    protected string EvalValue(string value, string dontEvalAttribute, bool dontEvalDefault)
    {
        if (BoolAttr(dontEvalAttribute, dontEvalDefault)) return value;
        return EvalValue(value);
    }

    protected string EvalValue(string value) =>
        Data.Conditions.TryEvaluateValue(value, out var evaluated) ? evaluated : value;

    // Condition helpers -------------------------------------------------------

    // Condition is read raw and substituted inside EvalCondition, matching
    // C++ CActionHelper::EvalCondition (ActionHelper.cpp:83).
    protected bool EvalCondition(XElement el) =>
        EvalCondition(RawAttr(el, XmlConstants.Attributes.Condition));

    protected bool EvalCondition(string? condition)
    {
        if (string.IsNullOrWhiteSpace(condition)) return true;
        return Data.Conditions.EvaluateLogged(
            Data.TsEnv.Substitute(condition), Data.Log,
            $"action '{Data.ActionNode.Attribute(XmlConstants.Attributes.Type)?.Value ?? Data.ActionNode.Name.LocalName}'");
    }
}
