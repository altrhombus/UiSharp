using UIpp.Core.Configuration;

namespace UIpp.Core.Actions.Impl;

[ActionType(XmlConstants.ActionTypes.Vars)]
public sealed class ActionVars(ActionData data) : ActionBase(data)
{
    public override ActionResult Go()
    {
        var direction = Attr(XmlConstants.Attributes.Direction, XmlConstants.Values.DirectionSave);
        var filename  = Data.TsEnv.Substitute(Attr(XmlConstants.Attributes.Filename, XmlConstants.Defaults.Filename));

        if (direction.Equals(XmlConstants.Values.DirectionLoad, StringComparison.OrdinalIgnoreCase))
            Data.TsEnv.LoadFromFile(filename);
        else
            Data.TsEnv.SaveToFile(filename);

        return ActionResult.Next;
    }
}
