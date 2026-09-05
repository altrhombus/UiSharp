using UiSharp.Core.Dialogs;
using UiSharp.Core.Logging;
using UiSharp.Core.Variables;

namespace UiSharp.UI.Dialogs;

public sealed class DlgInfo : DlgBase
{
    private readonly Image? _bannerImage;
    private readonly Image? _infoImage;

    /// <param name="bannerImage">
    /// The <c>Image</c> attribute — branding across the top. Ownership passes to
    /// the dialog, which disposes it.
    /// </param>
    /// <param name="infoImage">
    /// The <c>InfoImage</c> attribute — centred below the text, as the original
    /// places it (DlgUserInfo.cpp:121). Ownership passes to the dialog.
    /// </param>
    public DlgInfo(
        DialogTraits traits,
        ITSEnv env,
        string? dlgTitle,
        string? dlgSubtitle,
        string infoText,
        Image? bannerImage,
        Image? infoImage,
        bool showBack,
        bool showCancel,
        ICMLog? log = null)
        : base(traits, env, dlgTitle, dlgSubtitle, log)
    {
        _bannerImage = bannerImage;
        _infoImage   = infoImage;

        BtnBack.Visible   = showBack;
        BtnCancel.Visible = showCancel;

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

        // Apply the HTML-like tag subset that C++ CXHTMLCtrl renders.
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
