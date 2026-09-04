using UIpp.Core.Actions;
using UIpp.Core.Configuration;
using UIpp.UI.Dialogs;

namespace UIpp.UI.Actions;

[ActionType(XmlConstants.ActionTypes.UserInfoFull)]
public sealed class ActionInfoFullScreen(ActionData data) : ActionBase(data)
{
    public override bool IsGuiAction => true;

    public override ActionResult Go()
    {
        var title      = Attr(XmlConstants.Attributes.Title)    is { Length: > 0 } t ? t : null;
        var subtitle   = Attr(XmlConstants.Attributes.Subtitle) is { Length: > 0 } s ? s : null;
        var infoText = Data.TsEnv.Substitute(Data.ActionNode.Value.Trim());
        var showBack   = BoolAttr(XmlConstants.Attributes.ShowBack);
        var showCancel = BoolAttr(XmlConstants.Attributes.ShowCancel);
        var timeoutSec = int.TryParse(Attr(XmlConstants.Attributes.Timeout), out var t2) ? t2 : 0;
        var timeoutAct = Attr(XmlConstants.Attributes.TimeoutAction);

        using var dlg = new DlgInfoFullScreen(Data.GlobalDialogTraits, Data.TsEnv, title, subtitle,
                                               infoText, showBack, showCancel);
        if (timeoutSec > 0)
            dlg.EnableTimeout(timeoutSec, DialogHelpers.MapTimeoutAction(timeoutAct));
        dlg.ShowDialog();
        return dlg.Result;
    }

}
