using UiSharp.Core.Dialogs;
using UiSharp.Core.Logging;
using UiSharp.Core.Variables;

namespace UiSharp.UI.Dialogs;

// Full-screen info dialog — no border, maximized, covers taskbar.
public sealed class DlgInfoFullScreen : DlgBase
{
    private readonly Image? _brandingImage;

    /// <param name="brandingImage">
    /// The <c>Image</c> attribute. The original centres it near the top of the
    /// screen and caps it at 90% of the width and 45% of the height
    /// (DlgUserInfoFullScreen.cpp:143). Ownership passes to the dialog.
    /// </param>
    public DlgInfoFullScreen(
        DialogTraits traits,
        ITSEnv env,
        string? dlgTitle,
        string? dlgSubtitle,
        string infoText,
        Image? brandingImage,
        bool showBack,
        bool showCancel,
        ICMLog? log = null)
        : base(traits, env, dlgTitle, dlgSubtitle, log)
    {
        _brandingImage = brandingImage;

        FormBorderStyle = FormBorderStyle.None;
        WindowState     = FormWindowState.Maximized;
        BtnBack.Visible   = showBack;
        BtnCancel.Visible = showCancel;

        // The base lays everything out at the fixed dialog size, so a maximized
        // form was covering the screen with a 760x520 dialog painted in one
        // corner and bare form everywhere else. Docking makes the panels follow
        // the screen — which is also what lets the branding image below be
        // measured against the screen rather than against a dialog that is no
        // longer the right size.
        //
        // ContentPanel is first in the collection and therefore docks last, so
        // Fill takes what the other two leave.
        Sidebar.Dock      = DockStyle.Left;
        ButtonBar.Dock    = DockStyle.Bottom;
        ContentPanel.Dock = DockStyle.Fill;

        // The buttons are positioned from the right edge of the designed width;
        // once the bar is as wide as the screen they have to move with it.
        ButtonBar.Resize += (_, _) =>
        {
            var right = ButtonBar.Width - 8;
            foreach (var btn in new[] { BtnNext, BtnCancel, BtnRefresh })
            {
                btn.Left = right - btn.Width;
                right -= btn.Width + 8;
            }
        };

        var rtb = new RichTextBox
        {
            Text        = infoText,
            ReadOnly    = true,
            BorderStyle = BorderStyle.None,
            BackColor   = Color.White,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
            Dock        = DockStyle.Fill,
        };

        if (brandingImage is not null)
        {
            // Docked rather than positioned, because the panel is still at its
            // designed size here and only takes the screen's size once the form
            // is maximized. Anything measured now would be measured too small.
            var host = new Panel { Dock = DockStyle.Top, Height = 1 };
            var box  = new PictureBox();

            host.Controls.Add(box);
            ContentPanel.Controls.Add(rtb);
            ContentPanel.Controls.Add(host);

            var laying = false;

            // Measured against the host, which is the band the image actually
            // occupies. Measuring against the content panel instead put the
            // image off centre, because the panel's padding means the two are
            // not the same width.
            void Reflow()
            {
                if (laying) return;

                var available = host.ClientSize.Width;
                var screen    = ContentPanel.ClientSize.Height;

                if (available <= 0 || screen <= 0) return;

                laying = true;
                try
                {
                    // 90% of the width and 45% of the height, as the original
                    // caps it (DlgUserInfoFullScreen.cpp:154).
                    var size = UiImage.Fit(
                        brandingImage.Size,
                        new Size((int)(available * 0.9), (int)(screen * 0.45)));

                    box.Size     = size;
                    box.SizeMode = size == brandingImage.Size
                        ? PictureBoxSizeMode.Normal
                        : PictureBoxSizeMode.Zoom;

                    box.Image    = brandingImage;

                    // A tenth of the way down, centred, as the original places it.
                    box.Location = new Point(
                        Math.Max(0, (available - size.Width) / 2),
                        (int)(screen * 0.1));

                    host.Height = box.Bottom + 16;
                }
                finally { laying = false; }
            }

            // Setting the host's height inside its own Resize handler is why
            // Reflow guards against re-entering.
            host.Resize         += (_, _) => Reflow();
            ContentPanel.Resize += (_, _) => Reflow();

            Reflow();
        }
        else
        {
            ContentPanel.Controls.Add(rtb);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _brandingImage?.Dispose();
        base.Dispose(disposing);
    }
}
