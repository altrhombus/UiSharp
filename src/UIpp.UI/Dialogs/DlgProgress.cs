using UIpp.Core.Dialogs;

namespace UIpp.UI.Dialogs;

// Modeless progress dialog shown during ExternalCall execution.
// Displayed on its own STA thread via Application.Run(); closed by BeginInvoke from the caller.
public sealed class DlgProgress : Form
{
    public DlgProgress(DialogTraits traits, string? title)
    {
        Text            = title ?? "Please Wait";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        ShowInTaskbar   = false;
        ControlBox      = false;
        TopMost         = true;
        ClientSize      = new Size(360, 84);
        StartPosition   = FormStartPosition.CenterScreen;
        BackColor       = Color.White;

        Controls.Add(new Label
        {
            Text      = title ?? "Please wait...",
            AutoSize  = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = new Font(traits.FontFace, 9f),
            Bounds    = new Rectangle(12, 10, 336, 22),
        });

        Controls.Add(new ProgressBar
        {
            Style                 = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30,
            Bounds                = new Rectangle(12, 44, 336, 24),
        });
    }
}
