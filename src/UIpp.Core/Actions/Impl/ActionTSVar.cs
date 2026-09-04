using UIpp.Core.Configuration;

namespace UIpp.Core.Actions.Impl;

[ActionType(XmlConstants.ActionTypes.TSVar)]
public sealed class ActionTSVar(ActionData data) : ActionBase(data)
{
    public override ActionResult Go()
    {
        // Variable name: prefer Variable attr, fall back to Name, then default.
        var raw  = Attr(XmlConstants.Attributes.Variable);
        if (raw.Length == 0) raw = Attr(XmlConstants.Attributes.Name, XmlConstants.Defaults.Variable);
        var name = Data.TsEnv.Substitute(raw);

        // Value comes from the element's text content (same as C++ inner-text pattern),
        // then is evaluated as an expression unless DontEval says otherwise. C++
        // defaults DontEval to FALSE here (Actions.cpp:389), so evaluation is the
        // norm: <Action Type="TSVar" ...>"%Volume%"</Action> yields C: rather than
        // the quoted literal.
        var value = Data.TsEnv.Substitute(Data.ActionNode.Value);
        value = EvalValue(value, XmlConstants.Attributes.DontEval, dontEvalDefault: false);

        Data.TsEnv.Set(name, value);
        return ActionResult.Next;
    }
}
