using UiSharp.Core.Dialogs;
using UiSharp.Core.Logging;
using UiSharp.Core.Variables;

namespace UiSharp.UI.Dialogs;

// Terminal error dialog — shows message, Cancel (dismiss) button only. No Next button.
// C++ always shows Cancel; ShowBack is optional; Next is never shown.
// When showRestart is true (WinPE + ShowCancel=false), Cancel becomes a "Restart" button
// whose click the caller handles by terminating winpeshl.exe.
//
// The original builds this from the same dialog class as Info (CDlgUserInfo), so
// it takes the same two images and renders the same markup.
public sealed class DlgErrorInfo : DlgBase
{
    private readonly Image? _bannerImage;
    private readonly Image? _infoImage;

    public DlgErrorInfo(
        DialogTraits traits,
        ITSEnv env,
        string? dlgTitle,
        string infoText,
        Image? bannerImage = null,
        Image? infoImage   = null,
        bool showBack      = false,
        bool showRestart   = false,
        ICMLog? log        = null)
        : base(traits, env, dlgTitle ?? "Error", null, log)
    {
        _bannerImage = bannerImage;
        _infoImage   = infoImage;

        BtnBack.Visible    = showBack;
        BtnRefresh.Visible = false;
        BtnNext.Visible    = false;   // No Next button; Cancel is the only dismiss
        AcceptButton       = BtnCancel;

        if (showRestart)
            BtnCancel.Text = "Restart";

        var (top, bottom) = ImageLayout.Place(ContentPanel, bannerImage, infoImage);

        var rtb = new RichTextBox
        {
            ReadOnly    = true,
            BorderStyle = BorderStyle.None,
            BackColor   = Color.White,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
            Bounds      = new Rectangle(
                16, top,
                ContentPanel.Width - 32,
                Math.Max(0, bottom - top)),
        };

        // The same markup subset Info renders: the original passes both through
        // CXHTMLCtrl, and an error is the last place to start showing raw tags.
        HtmlMarkupRenderer.Apply(rtb, infoText, rtb.Font, rtb.ForeColor);

        ContentPanel.Controls.Add(rtb);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _bannerImage?.Dispose();
            _infoImage?.Dispose();
        }
        base.Dispose(disposing);
    }
}
