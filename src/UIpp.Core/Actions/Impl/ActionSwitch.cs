using System.Text.RegularExpressions;
using UIpp.Core.Configuration;

namespace UIpp.Core.Actions.Impl;

[ActionType(XmlConstants.ActionTypes.Switch)]
public sealed class ActionSwitch(ActionData data) : ActionBase(data)
{
    public override ActionResult Go()
    {
        var onValue = Data.TsEnv.Substitute(Attr(XmlConstants.Attributes.OnValue));

        foreach (var caseEl in Data.ActionNode.Elements(XmlConstants.Elements.Case))
        {
            if (!EvalCondition(caseEl)) continue;

            var pattern    = Attr(caseEl, XmlConstants.Attributes.RegEx, ".*");
            var ignoreCase = BoolAttr(caseEl, XmlConstants.Attributes.CaseInsensitive);
            var options    = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;

            if (!Regex.IsMatch(onValue, pattern, options)) continue;

            SetVariables(caseEl);
            return ActionResult.Next;
        }

        var defaultEl = Data.ActionNode.Element(XmlConstants.Elements.Default);
        if (defaultEl is not null) SetVariables(defaultEl);

        return ActionResult.Next;
    }

    private void SetVariables(System.Xml.Linq.XElement container)
    {
        foreach (var varEl in container.Elements(XmlConstants.Elements.Variable))
        {
            if (!EvalCondition(varEl)) continue;
            var name  = Attr(varEl, XmlConstants.Attributes.Name, XmlConstants.Defaults.Variable);
            var value = Data.TsEnv.Substitute(varEl.Value);
            Data.TsEnv.Set(name, value);
        }
    }
}
