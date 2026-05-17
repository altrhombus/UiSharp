using UIpp.Core.Actions;
using UIpp.Core.Configuration;
using UIpp.Core.Dialogs;
using UIpp.UI.Dialogs;

namespace UIpp.UI.Actions;

[ActionType(XmlConstants.ActionTypes.Preflight)]
public sealed class ActionPreflight(ActionData data) : ActionBase(data)
{
    public override bool IsGuiAction => true;

    public override ActionResult Go()
    {
        var title            = SubstAttr(XmlConstants.Attributes.Title)    is { Length: > 0 } t ? t : null;
        var subtitle         = SubstAttr(XmlConstants.Attributes.Subtitle) is { Length: > 0 } s ? s : null;
        var showBack       = BoolAttr(XmlConstants.Attributes.ShowBack);
        var showCancel     = BoolAttr(XmlConstants.Attributes.ShowCancel);
        var showOnFailOnly = BoolAttr(XmlConstants.Attributes.ShowOnFailureOnly);
        var timeoutSec     = int.TryParse(Attr(XmlConstants.Attributes.Timeout), out var t2) ? t2 : 0;
        var timeoutAct     = Attr(XmlConstants.Attributes.TimeoutAction);

        var checks  = PreflightEvaluator.ParseChecks(Data.ActionNode, Data.TsEnv, Data.Conditions);
        var results = PreflightEvaluator.Evaluate(checks, Data.Conditions, Data.TsEnv);
        var anyFail = PreflightEvaluator.AnyFailed(results);

        // If ShowOnFailureOnly and no failures, skip dialog and continue.
        if (showOnFailOnly && !anyFail)
            return ActionResult.Next;

        ActionResult result = ActionResult.Next;

        var thread = new Thread(() =>
        {
            Application.EnableVisualStyles();
            using var dlg = new DlgPreflight(Data.GlobalDialogTraits, Data.TsEnv, title, subtitle,
                                              results, showBack, showCancel, anyFail);
            if (timeoutSec > 0)
                dlg.EnableTimeout(timeoutSec, DialogHelpers.MapTimeoutAction(timeoutAct));
            dlg.ShowDialog();
            result = dlg.Result;
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        // If user clicked Next but checks failed, return Cancel so the processor exits.
        if (result == ActionResult.Next && anyFail)
            return ActionResult.Cancel;

        return result;
    }

}
