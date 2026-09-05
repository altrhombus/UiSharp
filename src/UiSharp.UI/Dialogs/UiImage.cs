using UiSharp.Core.Logging;

namespace UiSharp.UI.Dialogs;

/// <summary>
/// Loading the images a configuration points at.
///
/// A path may be a local file or an <c>http(s)</c> URL. The original downloads
/// URLs to a temporary file before loading them (DlgBase.cpp:391) and its own
/// sample configuration brands every dialog from a web server, so a port that
/// only reads local files loses the branding — silently, which is the part that
/// matters. Every outcome here is logged.
/// </summary>
public static class UiImage
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>
    /// Loads an image, or returns null and says in the log why not. Never throws:
    /// a dialog that cannot show its banner should still show its text.
    /// </summary>
    /// <param name="pathOrUrl">A local path or an http(s) URL. Null or empty means no image.</param>
    /// <param name="log">Where outcomes are reported.</param>
    /// <param name="purpose">
    /// What this image is for — "banner image", "icon" — so a log line names the
    /// attribute the operator has to go and fix.
    /// </param>
    /// <param name="fetch">
    /// Overrides the download, for tests. Given the URL, returns its bytes or
    /// throws.
    /// </param>
    public static Image? Load(
        string? pathOrUrl,
        ICMLog? log = null,
        string purpose = "image",
        Func<string, byte[]>? fetch = null)
    {
        if (string.IsNullOrWhiteSpace(pathOrUrl)) return null;

        var bytes = IsHttpUrl(pathOrUrl)
            ? Download(pathOrUrl, log, purpose, fetch)
            : ReadFile(pathOrUrl, log, purpose);

        if (bytes is null) return null;

        try
        {
            // Loaded through a stream rather than Image.FromFile, which holds the
            // file open for as long as the image lives — an open handle on a
            // downloaded temp file, or on a file the deployment wants to replace.
            //
            // GDI+ reads from the stream lazily, so the stream must outlive the
            // image. A MemoryStream over a byte[] holds nothing that needs
            // releasing, so letting the garbage collector take both is correct.
            var image = Image.FromStream(new MemoryStream(bytes));

            log?.Write($"Loaded the {purpose} from {pathOrUrl} " +
                       $"({image.Width}x{image.Height}, {DescribeFormat(image)}).");

            return image;
        }
        catch (Exception ex)
        {
            // The usual cause in WinPE: a boot image without the codec for this
            // format. Naming the format is what turns that into a fixable report.
            log?.Write(
                $"The {purpose} at {pathOrUrl} could not be decoded: {ex.Message}. " +
                "In a boot image this usually means the codec for that format is " +
                "not present; PNG and BMP are the safest choices.",
                LogSeverity.Warning);

            return null;
        }
    }

    /// <summary>
    /// Loads a window icon. Accepts a real <c>.ico</c>, and falls back to any
    /// other image format by converting it.
    /// </summary>
    public static Icon? LoadIcon(string? pathOrUrl, ICMLog? log = null, Func<string, byte[]>? fetch = null)
    {
        if (string.IsNullOrWhiteSpace(pathOrUrl)) return null;

        var bytes = IsHttpUrl(pathOrUrl)
            ? Download(pathOrUrl, log, "icon", fetch)
            : ReadFile(pathOrUrl, log, "icon");

        if (bytes is null) return null;

        try
        {
            var icon = new Icon(new MemoryStream(bytes));
            log?.Write($"Loaded the window icon from {pathOrUrl}.");
            return icon;
        }
        catch
        {
            // Not an .ico. The attribute is named Icon, but nothing stops a
            // configuration pointing it at a PNG, and refusing that would be a
            // worse answer than converting it.
        }

        using var image = Load(pathOrUrl, log, "window icon", fetch);
        if (image is null) return null;

        try
        {
            using var bitmap = new Bitmap(image);
            var handle = bitmap.GetHicon();

            try
            {
                // Cloned because the icon returned by FromHandle does not own
                // its handle, and the handle is destroyed below.
                using var borrowed = Icon.FromHandle(handle);
                return (Icon)borrowed.Clone();
            }
            finally
            {
                NativeMethods.DestroyIcon(handle);
            }
        }
        catch (Exception ex)
        {
            log?.Write($"The window icon at {pathOrUrl} could not be converted: {ex.Message}",
                LogSeverity.Warning);
            return null;
        }
    }

    /// <summary>
    /// The largest size no bigger than <paramref name="max"/> that keeps the
    /// image's proportions. An image already within bounds is left alone.
    /// </summary>
    public static Size Fit(Size image, Size max)
    {
        if (image.Width <= 0 || image.Height <= 0) return Size.Empty;
        if (max.Width   <= 0 || max.Height   <= 0) return Size.Empty;
        if (image.Width <= max.Width && image.Height <= max.Height) return image;

        var scale = Math.Min((double)max.Width / image.Width, (double)max.Height / image.Height);

        return new Size(
            Math.Max(1, (int)Math.Round(image.Width  * scale)),
            Math.Max(1, (int)Math.Round(image.Height * scale)));
    }

    /// <summary>
    /// A control showing the image at no more than <paramref name="max"/>.
    ///
    /// An oversized image is scaled by the control, not resampled into a new
    /// bitmap: resampling flattens an animated GIF to its first frame, and
    /// PictureBox animates a multi-frame image on its own.
    /// </summary>
    public static PictureBox Box(Image image, Size max)
    {
        var size = Fit(image.Size, max);

        return new PictureBox
        {
            Image    = image,
            Size     = size,
            SizeMode = size == image.Size
                ? PictureBoxSizeMode.Normal
                : PictureBoxSizeMode.Zoom,
        };
    }

    // -------------------------------------------------------------------------

    public static bool IsHttpUrl(string path) =>
        path.StartsWith("http://",  StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static byte[]? ReadFile(string path, ICMLog? log, string purpose)
    {
        try
        {
            if (File.Exists(path)) return File.ReadAllBytes(path);

            log?.Write($"The {purpose} '{path}' was not found.", LogSeverity.Warning);
            return null;
        }
        catch (Exception ex)
        {
            log?.Write($"The {purpose} '{path}' could not be read: {ex.Message}", LogSeverity.Warning);
            return null;
        }
    }

    private static byte[]? Download(string url, ICMLog? log, string purpose, Func<string, byte[]>? fetch)
    {
        try
        {
            // Blocking on purpose: this runs before the dialog is shown, with no
            // message loop to deadlock against.
            var bytes = fetch is not null
                ? fetch(url)
                : Http.GetByteArrayAsync(url).GetAwaiter().GetResult();

            log?.Write($"Downloaded the {purpose} from {url} ({bytes.Length} bytes).");
            return bytes;
        }
        catch (Exception ex)
        {
            // In WinPE this is usually the network not being up yet rather than
            // the URL being wrong, so it says which it cannot tell apart.
            log?.Write(
                $"The {purpose} could not be downloaded from {url}: {Innermost(ex).Message}. " +
                "The dialog will be shown without it.",
                LogSeverity.Warning);

            return null;
        }
    }

    private static Exception Innermost(Exception ex) =>
        ex.InnerException is null ? ex : Innermost(ex.InnerException);

    private static string DescribeFormat(Image image)
    {
        var name = image.RawFormat.ToString();

        // Frame count is the interesting part: it is the difference between a
        // still and something that will animate on screen.
        try
        {
            var frames = image.GetFrameCount(System.Drawing.Imaging.FrameDimension.Time);
            if (frames > 1) return $"{name}, {frames} frames";
        }
        catch { /* single-frame formats have no time dimension */ }

        return name;
    }

    private static class NativeMethods
    {
        // DllImport rather than LibraryImport: the generated marshalling code
        // needs AllowUnsafeBlocks, and one call on a dialog's icon is not worth
        // turning that on for the whole assembly.
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        [System.Runtime.InteropServices.DefaultDllImportSearchPaths(
            System.Runtime.InteropServices.DllImportSearchPath.System32)]
        internal static extern bool DestroyIcon(IntPtr handle);
    }
}
