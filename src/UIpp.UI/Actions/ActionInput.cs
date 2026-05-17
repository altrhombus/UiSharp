using UIpp.Core.Actions;
using UIpp.Core.Configuration;
using UIpp.Core.Dialogs;
using UIpp.Core.Scripting;
using UIpp.UI.Dialogs;

namespace UIpp.UI.Actions;

[ActionType(XmlConstants.ActionTypes.UserInput)]
public sealed class ActionInput(ActionData data) : ActionBase(data)
{
    public override bool IsGuiAction => true;

    public override ActionResult Go()
    {
        var title      = SubstAttr(XmlConstants.Attributes.Title)    is { Length: > 0 } t ? t : null;
        var subtitle   = SubstAttr(XmlConstants.Attributes.Subtitle) is { Length: > 0 } s ? s : null;
        var showBack   = BoolAttr(XmlConstants.Attributes.ShowBack);
        var showCancel = BoolAttr(XmlConstants.Attributes.ShowCancel);
        var timeoutSec = int.TryParse(Attr(XmlConstants.Attributes.Timeout), out var t2) ? t2 : 0;
        var timeoutAct = Attr(XmlConstants.Attributes.TimeoutAction);

        var fields = InputFieldParser.Parse(Data.ActionNode, Data.TsEnv, Data.Conditions);

        ActionResult result = ActionResult.Next;
        DlgInput? dlgRef    = null;

        var thread = new Thread(() =>
        {
            Application.EnableVisualStyles();
            using var dlg = new DlgInput(Data.GlobalDialogTraits, Data.TsEnv, title, subtitle, fields,
                                          showBack, showCancel);
            if (timeoutSec > 0)
                dlg.EnableTimeout(timeoutSec, MapTimeoutAction(timeoutAct));

            dlgRef = dlg;
            dlg.ShowDialog();
            result = dlg.Result;

            if (result == ActionResult.Next)
                dlg.CommitValues(Data.TsEnv);
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
