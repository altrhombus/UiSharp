using UiSharp.Core.Actions;
using UiSharp.Core.Configuration;
using UiSharp.UI.Dialogs;

namespace UiSharp.UI.Actions;

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
        var branding   = UiImage.Load(Attr(XmlConstants.Attributes.Image), Data.Log, "branding image");

        using var dlg = new DlgInfoFullScreen(Data.GlobalDialogTraits, Data.TsEnv, title, subtitle,
                                               infoText, branding, showBack, showCancel, Data.Log);
        if (timeoutSec > 0)
            dlg.EnableTimeout(timeoutSec, DialogHelpers.MapTimeoutAction(timeoutAct));
        dlg.ShowDialog();
        return dlg.Result;
    }

}
