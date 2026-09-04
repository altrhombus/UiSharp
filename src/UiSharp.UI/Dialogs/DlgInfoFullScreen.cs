using UiSharp.Core.Dialogs;
using UiSharp.Core.Variables;

namespace UiSharp.UI.Dialogs;

// Full-screen info dialog — no border, maximized, covers taskbar.
public sealed class DlgInfoFullScreen : DlgBase
{
    public DlgInfoFullScreen(
        DialogTraits traits,
        ITSEnv env,
        string? dlgTitle,
        string? dlgSubtitle,
        string infoText,
        bool showBack,
        bool showCancel)
        : base(traits, env, dlgTitle, dlgSubtitle)
    {
        FormBorderStyle = FormBorderStyle.None;
        WindowState     = FormWindowState.Maximized;
        BtnBack.Visible   = showBack;
        BtnCancel.Visible = showCancel;

        var rtb = new RichTextBox
        {
            Text        = infoText,
            ReadOnly    = true,
            BorderStyle = BorderStyle.None,
            BackColor   = Color.White,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
            Dock        = DockStyle.Fill,
        };

        ContentPanel.Controls.Add(rtb);
    }
}
