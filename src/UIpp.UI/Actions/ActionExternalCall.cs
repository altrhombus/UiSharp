using System.Diagnostics;
using UIpp.Core.Actions;
using UIpp.Core.Configuration;
using UIpp.Core.Logging;
using UIpp.UI.Dialogs;

namespace UIpp.UI.Actions;

// UIpp.UI version of ExternalCall — identical to the Core version but shows DlgProgress
// while the process runs.  Registered last in Program.cs so it overwrites the Core
// registration in the factory, while Core tests continue to use the Core class directly.
[ActionType(XmlConstants.ActionTypes.ExternalCall)]
public sealed class ActionExternalCall(ActionData data) : ActionBase(data)
{
    public override ActionResult Go()
    {
        var commandLine = Data.TsEnv.Substitute(Data.ActionNode.Value.Trim());
        var exitVar     = Attr(XmlConstants.Attributes.ExitCodeVariable);
        var title       = Attr(XmlConstants.Attributes.Title);

        if (!int.TryParse(Attr(XmlConstants.Attributes.MaxRunTime,
                XmlConstants.Defaults.MaxRunTime.ToString()), out var maxRunTime))
            maxRunTime = XmlConstants.Defaults.MaxRunTime;

        if (string.IsNullOrWhiteSpace(commandLine)) return ActionResult.Next;

        // Show modeless progress dialog on a dedicated STA thread.
        DlgProgress? progressDlg = null;
        using var dlgShown = new ManualResetEventSlim();

        var uiThread = new Thread(() =>
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            using var dlg = new DlgProgress(Data.GlobalDialogTraits, title);
            dlg.Shown += (_, _) => { progressDlg = dlg; dlgShown.Set(); };
            System.Windows.Forms.Application.Run(dlg);
        });
        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.IsBackground = true;
        uiThread.Start();
        dlgShown.Wait(5000);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName        = "cmd.exe",
                Arguments       = $"/c {commandLine}",
                UseShellExecute = false,
                CreateNoWindow  = true,
            };

            using var proc = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start process.");

            var completed = proc.WaitForExit(maxRunTime * 1000);

            if (!completed)
            {
                proc.Kill(entireProcessTree: true);
                Data.Log.Write(
                    $"ExternalCall: process exceeded {maxRunTime}s — terminated.",
                    LogSeverity.Warning);
            }

            if (!string.IsNullOrWhiteSpace(exitVar))
                Data.TsEnv.Set(exitVar, completed ? proc.ExitCode.ToString() : "-1");
        }
        catch (Exception ex)
        {
            Data.Log.Write($"ExternalCall: {ex.Message}", LogSeverity.Error);
        }
        finally
        {
            var dlg = progressDlg;
            if (dlg is not null && dlg.IsHandleCreated && !dlg.IsDisposed)
                dlg.BeginInvoke(() => dlg.Close());
            uiThread.Join(3000);
        }

        return ActionResult.Next;
    }
}
