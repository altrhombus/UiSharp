using UiSharp.Core.Actions;
using UiSharp.Core.Configuration;
using UiSharp.UI.Dialogs;

namespace UiSharp.UI.Actions;

[ActionType(XmlConstants.ActionTypes.UserInfo)]
public sealed class ActionInfo(ActionData data) : ActionBase(data)
{
    public override bool IsGuiAction => true;

    public override ActionResult Go()
    {
        var title      = Attr(XmlConstants.Attributes.Title)    is { Length: > 0 } t ? t : null;
        var subtitle   = Attr(XmlConstants.Attributes.Subtitle) is { Length: > 0 } s ? s : null;
        // Info text is the inner text of the Action element (matches C++ child_value()).
        var infoText = Data.TsEnv.Substitute(Data.ActionNode.Value.Trim());
        // Loaded here rather than in the dialog so the action's log records what
        // happened. Both may be local paths or http(s) URLs.
        var banner     = UiImage.Load(Attr(XmlConstants.Attributes.Image),     Data.Log, "banner image");
        var infoImage  = UiImage.Load(Attr(XmlConstants.Attributes.InfoImage), Data.Log, "info image");
        var showBack   = BoolAttr(XmlConstants.Attributes.ShowBack);
        var showCancel = BoolAttr(XmlConstants.Attributes.ShowCancel);
        var timeoutSec = int.TryParse(Attr(XmlConstants.Attributes.Timeout), out var t2) ? t2 : 0;
        var timeoutAct = Attr(XmlConstants.Attributes.TimeoutAction);

        using var dlg = new DlgInfo(Data.GlobalDialogTraits, Data.TsEnv, title, subtitle, infoText,
                                    banner, infoImage, showBack, showCancel, Data.Log);
        if (timeoutSec > 0)
            dlg.EnableTimeout(timeoutSec, DialogHelpers.MapTimeoutAction(timeoutAct));
        dlg.ShowDialog();
        return dlg.Result;
    }

}
