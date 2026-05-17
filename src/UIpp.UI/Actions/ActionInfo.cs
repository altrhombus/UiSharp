using UIpp.Core.Actions;
using UIpp.Core.Configuration;
using UIpp.UI.Dialogs;

namespace UIpp.UI.Actions;

[ActionType(XmlConstants.ActionTypes.UserInfo)]
public sealed class ActionInfo(ActionData data) : ActionBase(data)
{
    public override bool IsGuiAction => true;

    public override ActionResult Go()
    {
        var title      = SubstAttr(XmlConstants.Attributes.Title)    is { Length: > 0 } t ? t : null;
        var subtitle   = SubstAttr(XmlConstants.Attributes.Subtitle) is { Length: > 0 } s ? s : null;
        // Info text is the inner text of the Action element (matches C++ child_value()).
        var infoText = Data.TsEnv.Substitute(Data.ActionNode.Value.Trim());
        var imagePath  = SubstAttr(XmlConstants.Attributes.Image)    is { Length: > 0 } i ? i : null;
        var showBack   = BoolAttr(XmlConstants.Attributes.ShowBack);
        var showCancel = BoolAttr(XmlConstants.Attributes.ShowCancel);
        var timeoutSec = int.TryParse(Attr(XmlConstants.Attributes.Timeout), out var t2) ? t2 : 0;
        var timeoutAct = Attr(XmlConstants.Attributes.TimeoutAction);

        ActionResult result = ActionResult.Next;

        var thread = new Thread(() =>
        {
            Application.EnableVisualStyles();
            using var dlg = new DlgInfo(Data.GlobalDialogTraits, Data.TsEnv, title, subtitle, infoText,
                                        imagePath, showBack, showCancel);
            if (timeoutSec > 0)
                dlg.EnableTimeout(timeoutSec, MapTimeoutAction(timeoutAct));

            dlg.ShowDialog();
            result = dlg.Result;
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return result;
    }

    private static ActionResult MapTimeoutAction(string act) => act.ToLowerInvariant() switch
    {
        "cancel" => ActionResult.Cancel,
        "back"   => ActionResult.Back,
        _        => ActionResult.Next,
    };
}
