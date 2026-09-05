namespace UiSharp.UI.Dialogs;

/// <summary>
/// Where the two images on an information dialog go, and how much room is left
/// for the text between them.
///
/// Shared by <see cref="DlgInfo"/> and <see cref="DlgErrorInfo"/> because the
/// original serves both from one dialog class (CDlgUserInfo), so they had better
/// not drift apart.
/// </summary>
internal static class ImageLayout
{
    private const int Margin = 16;
    private const int Gap    = 8;

    /// <summary>
    /// Adds whichever images are present to the panel and returns the vertical
    /// band left over for the text.
    /// </summary>
    /// <returns>
    /// The first free y coordinate below the banner, and the first y coordinate
    /// occupied by the info image (or the bottom margin when there is none).
    /// </returns>
    public static (int Top, int Bottom) Place(Panel panel, Image? banner, Image? info)
    {
        var top    = 12;
        var bottom = panel.Height - 12;

        // Neither image may take more than a third of the panel: the text is
        // what the operator is being asked to read, and an image at native size
        // on a 1024x768 boot image can leave no room for it at all.
        var max = new Size(panel.Width - Margin * 2, panel.Height / 3);

        if (banner is not null)
        {
            var box = UiImage.Box(banner, max);
            box.Location = new Point(Margin, top);
            panel.Controls.Add(box);
            top += box.Height + Gap;
        }

        if (info is not null)
        {
            var box = UiImage.Box(info, max);

            // Centred against the bottom, as the original places it.
            box.Location = new Point(
                Math.Max(Margin, (panel.Width - box.Width) / 2),
                bottom - box.Height);

            panel.Controls.Add(box);
            bottom = box.Top - Gap;
        }

        return (top, Math.Max(top, bottom));
    }
}
