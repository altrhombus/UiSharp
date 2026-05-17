using UIpp.Core.Dialogs;
using UIpp.Core.Variables;

namespace UIpp.UI.Dialogs;

public sealed class DlgInfo : DlgBase
{
    public DlgInfo(
        DialogTraits traits,
        ITSEnv env,
        string? dlgTitle,
        string? dlgSubtitle,
        string infoText,
        string? imagePath,
        bool showBack,
        bool showCancel)
        : base(traits, env, dlgTitle, dlgSubtitle)
    {
        BtnBack.Visible   = showBack;
        BtnCancel.Visible = showCancel;

        int topOffset = 12;

        if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
        {
            try
            {
                var pb = new PictureBox
                {
                    Image    = Image.FromFile(imagePath),
                    SizeMode = PictureBoxSizeMode.AutoSize,
                    Location = new Point(16, topOffset),
                };
                ContentPanel.Controls.Add(pb);
                topOffset += pb.Image.Height + 8;
            }
            catch { /* ignore missing/invalid image */ }
        }

        var rtb = new RichTextBox
        {
            ReadOnly    = true,
            BorderStyle = BorderStyle.None,
            BackColor   = Color.White,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
            Bounds      = new Rectangle(
                16, topOffset,
                ContentPanel.Width - 32,
                ContentPanel.Height - topOffset - 12),
        };

        // Apply the HTML-like tag subset that C++ CXHTMLCtrl renders.
        HtmlMarkupRenderer.Apply(rtb, infoText, rtb.Font, rtb.ForeColor);

        ContentPanel.Controls.Add(rtb);
    }
}
