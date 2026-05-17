using System.Diagnostics;
using UIpp.Core.Actions;
using UIpp.Core.Configuration;
using UIpp.UI.Dialogs;

namespace UIpp.UI.Actions;

[ActionType(XmlConstants.ActionTypes.ErrorInfo)]
public sealed class ActionErrorInfo(ActionData data) : ActionBase(data)
{
    // C++ returns false: "never get past it and therefore can never come back to it anyway"
    public override bool IsGuiAction => false;

    public override ActionResult Go()
    {
        var title       = SubstAttr(XmlConstants.Attributes.Title) is { Length: > 0 } t ? t : null;
        var infoText    = Data.TsEnv.Substitute(Data.ActionNode.Value.Trim());
        var showBack    = BoolAttr(XmlConstants.Attributes.ShowBack);
        var showCancel  = BoolAttr(XmlConstants.Attributes.ShowCancel);
        // C++: if (!includeCancel && inWinPE) dlg.ShowRestartButton()
        var showRestart = Data.InWinPE && !showCancel;

        ActionResult dialogResult;
        using (var dlg = new DlgErrorInfo(Data.GlobalDialogTraits, Data.TsEnv, title, infoText, showBack, showRestart))
        {
            dlg.ShowDialog();
            dialogResult = dlg.Result;
        }

        // C++: if (!includeCancel && result == ERROR_CANCELLED && inWinPE) → terminate winpeshl.exe
        if (showRestart && dialogResult == ActionResult.Cancel)
            TerminateWinPeShell();

        return ActionResult.Cancel;
    }

    private static void TerminateWinPeShell()
    {
        foreach (var p in Process.GetProcessesByName("winpeshl"))
        {
            try { p.Kill(); } catch { }
            p.Dispose();
        }
    }
}
