using UiSharp.Core.Dialogs;
using UiSharp.Core.Variables;

namespace UiSharp.UI.Dialogs;

// Terminal error dialog — shows message, Cancel (dismiss) button only. No Next button.
// C++ always shows Cancel; ShowBack is optional; Next is never shown.
// When showRestart is true (WinPE + ShowCancel=false), Cancel becomes a "Restart" button
// whose click the caller handles by terminating winpeshl.exe.
public sealed class DlgErrorInfo : DlgBase
{
    public DlgErrorInfo(
        DialogTraits traits,
        ITSEnv env,
        string? dlgTitle,
        string infoText,
        bool showBack    = false,
        bool showRestart = false)
        : base(traits, env, dlgTitle ?? "Error")
    {
        BtnBack.Visible    = showBack;
        BtnRefresh.Visible = false;
        BtnNext.Visible    = false;   // No Next button; Cancel is the only dismiss
        AcceptButton       = BtnCancel;

        if (showRestart)
            BtnCancel.Text = "Restart";

        var rtb = new RichTextBox
        {
            Text        = infoText,
            ReadOnly    = true,
            BorderStyle = BorderStyle.None,
            BackColor   = Color.White,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
            Bounds      = new Rectangle(16, 12, ContentPanel.Width - 32, ContentPanel.Height - 24),
        };

        ContentPanel.Controls.Add(rtb);
    }
}
