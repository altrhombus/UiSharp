using UiSharp.Core.Configuration;

namespace UiSharp.Core.Actions.Impl;

// Writes numbered TS variables from a list of software references,
// mirroring the C++ CActionTSVarList pattern for pre-populating AppTree selections.
[ActionType(XmlConstants.ActionTypes.TSVarList)]
public sealed class ActionTSVarList(ActionData data) : ActionBase(data)
{
    public override ActionResult Go()
    {
        if (Data.Software is null) return ActionResult.Next;

        var appBase = Attr(XmlConstants.Attributes.AppVarBase);
        var pkgBase = Attr(XmlConstants.Attributes.PackageVarBase);

        var items = Data.ActionNode
            .Elements(XmlConstants.Elements.SoftwareListRef)
            .Where(el => EvalCondition(el))
            .Select(el => Attr(el, XmlConstants.Attributes.Id))
            .Where(id => id.Length > 0 && Data.Software.ContainsKey(id))
            .Select(id => Data.Software[id])
            .OrderBy(sw => sw.OrderIndex)
            .ToList();

        int appIdx = 1, pkgIdx = 1;
        foreach (var sw in items)
        {
            if (sw.Type == "Application" && appBase.Length > 0)
                Data.TsEnv.Set($"{appBase}{appIdx++:D2}", sw.GetVariableValue());
            else if (sw.Type == "Package" && pkgBase.Length > 0)
                Data.TsEnv.Set($"{pkgBase}{pkgIdx++:D3}", sw.GetVariableValue());
        }

        return ActionResult.Next;
    }
}
