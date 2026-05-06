using System.Text.Json;
using UIpp.Core.Configuration;

namespace UIpp.Core.Actions.Impl;

[ActionType(XmlConstants.ActionTypes.ToJson)]
public sealed class ActionToJson(ActionData data) : ActionBase(data)
{
    public override ActionResult Go()
    {
        var variable = Attr(XmlConstants.Attributes.Variable, XmlConstants.Defaults.JsonVariable);

        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var attrEl in Data.ActionNode.Elements(XmlConstants.Elements.Attribute))
        {
            if (!EvalCondition(attrEl)) continue;
            var name  = Attr(attrEl, XmlConstants.Attributes.Name);
            var value = Data.TsEnv.Substitute(attrEl.Value);
            if (name.Length > 0)
                dict[name] = value;
        }

        Data.TsEnv.Set(variable, JsonSerializer.Serialize(dict));
        return ActionResult.Next;
    }
}
