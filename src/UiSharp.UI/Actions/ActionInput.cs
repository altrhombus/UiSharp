using UiSharp.Core.Actions;
using UiSharp.Core.Configuration;
using UiSharp.Core.Dialogs;
using UiSharp.Core.Scripting;
using UiSharp.UI.Dialogs;

namespace UiSharp.UI.Actions;

[ActionType(XmlConstants.ActionTypes.UserInput)]
public sealed class ActionInput(ActionData data) : ActionBase(data)
{
    public override bool IsGuiAction => true;

    public override ActionResult Go()
    {
        var title      = Attr(XmlConstants.Attributes.Title)    is { Length: > 0 } t ? t : null;
        var subtitle   = Attr(XmlConstants.Attributes.Subtitle) is { Length: > 0 } s ? s : null;
        var showBack   = BoolAttr(XmlConstants.Attributes.ShowBack);
        var showCancel = BoolAttr(XmlConstants.Attributes.ShowCancel);
        var timeoutSec = int.TryParse(Attr(XmlConstants.Attributes.Timeout), out var t2) ? t2 : 0;
        var timeoutAct = Attr(XmlConstants.Attributes.TimeoutAction);

        var fields = InputFieldParser.Parse(Data.ActionNode, Data.TsEnv, Data.Conditions, Data.Log);

        using var dlg = new DlgInput(Data.GlobalDialogTraits, Data.TsEnv, title, subtitle, fields,
                                      showBack, showCancel);
        if (timeoutSec > 0)
            dlg.EnableTimeout(timeoutSec, DialogHelpers.MapTimeoutAction(timeoutAct));
        dlg.ShowDialog();
        var result = dlg.Result;
        if (result == ActionResult.Next)
            dlg.CommitValues(Data.TsEnv);
        return result;
    }

}
