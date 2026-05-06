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

        // Value comes from the element's text content (same as C++ inner-text pattern).
        var value = Data.TsEnv.Substitute(Data.ActionNode.Value);

        Data.TsEnv.Set(name, value);
        return ActionResult.Next;
    }
}
