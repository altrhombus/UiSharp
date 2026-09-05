using UiSharp.Core.Logging;
using UiSharp.UI.Dialogs;

namespace UiSharp.UI.Tests;

public class UiImageTests
{
    private sealed class RecordingLog : ICMLog
    {
        public List<(string Message, LogSeverity Severity)> Lines { get; } = [];

        public void Write(string message, LogSeverity severity = LogSeverity.Info,
                          string component = LogFile.DefaultComponent) =>
            Lines.Add((message, severity));

        public bool Said(string fragment) =>
            Lines.Any(l => l.Message.Contains(fragment, StringComparison.OrdinalIgnoreCase));

        public bool Warned => Lines.Any(l => l.Severity != LogSeverity.Info);
    }

    // -------------------------------------------------------------------------
    // Formats

    [Theory]
    [InlineData(".png")]
    [InlineData(".bmp")]
    [InlineData(".gif")]
    public void A_local_image_is_loaded(string extension)
    {
        var bytes = extension switch
        {
            ".png" => TestImages.Png(),
            ".bmp" => TestImages.Bmp(),
            _      => TestImages.Gif(),
        };

        var path = TestImages.WriteToFile(bytes, extension);

        try
        {
            var log = new RecordingLog();
            using var image = UiImage.Load(path, log, "banner image");

            Assert.NotNull(image);
            Assert.Equal(40, image!.Width);
            Assert.Equal(20, image.Height);
            Assert.False(log.Warned);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void An_animated_gif_keeps_its_frames()
    {
        // The question that started this: an animated GIF must survive loading
        // with more than one frame, or PictureBox has nothing to animate.
        var path = TestImages.WriteToFile(TestImages.AnimatedGif(), ".gif");

        try
        {
            using var image = UiImage.Load(path);

            Assert.NotNull(image);
            Assert.Equal(2, image!.GetFrameCount(System.Drawing.Imaging.FrameDimension.Time));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Loading_does_not_hold_the_file_open()
    {
        // Image.FromFile keeps a handle for the life of the image, which would
        // leave a downloaded temp file undeletable and block a deployment that
        // wants to replace the file.
        var path = TestImages.WriteToFile(TestImages.Png(), ".png");
        using var image = UiImage.Load(path);

        Assert.NotNull(image);

        File.Delete(path);                       // would throw if still held
        Assert.Equal(40, image!.Width);          // and the image still works
    }

    // -------------------------------------------------------------------------
    // Failures are reported, never thrown

    [Fact]
    public void A_missing_file_is_reported_and_returns_nothing()
    {
        var log = new RecordingLog();

        Assert.Null(UiImage.Load(@"C:\no\such\banner.png", log, "banner image"));
        Assert.True(log.Warned);
        Assert.True(log.Said("banner image"));
        Assert.True(log.Said("banner.png"));
    }

    [Fact]
    public void A_file_that_is_not_an_image_is_reported_and_returns_nothing()
    {
        var path = TestImages.WriteToFile("this is not a picture"u8.ToArray(), ".png");

        try
        {
            var log = new RecordingLog();

            Assert.Null(UiImage.Load(path, log, "banner image"));
            Assert.True(log.Warned);

            // A boot image missing a codec is the usual cause, so the log says so.
            Assert.True(log.Said("codec"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void No_path_is_not_a_failure()
    {
        var log = new RecordingLog();

        Assert.Null(UiImage.Load(null, log));
        Assert.Null(UiImage.Load("", log));
        Assert.Null(UiImage.Load("   ", log));
        Assert.Empty(log.Lines);
    }

    // -------------------------------------------------------------------------
    // URLs

    [Theory]
    [InlineData("http://example.invalid/banner.png")]
    [InlineData("HTTPS://example.invalid/banner.png")]
    public void An_http_url_is_downloaded(string url)
    {
        var log = new RecordingLog();
        var asked = new List<string>();

        using var image = UiImage.Load(url, log, "banner image", fetch: u =>
        {
            asked.Add(u);
            return TestImages.Png();
        });

        Assert.Equal([url], asked);
        Assert.NotNull(image);
        Assert.False(log.Warned);
        Assert.True(log.Said("Downloaded"));
    }

    [Fact]
    public void A_download_that_fails_is_reported_and_the_dialog_goes_on_without_it()
    {
        // WinPE often reaches a dialog before the network is up. Losing the
        // branding is acceptable; losing the dialog is not.
        var log = new RecordingLog();

        var image = UiImage.Load(
            "http://example.invalid/banner.png", log, "banner image",
            fetch: _ => throw new HttpRequestException("no such host is known"));

        Assert.Null(image);
        Assert.True(log.Warned);
        Assert.True(log.Said("no such host is known"));
        Assert.True(log.Said("without it"));
    }

    [Theory]
    [InlineData("http://host/x.png", true)]
    [InlineData("https://host/x.png", true)]
    [InlineData("HtTp://host/x.png", true)]
    [InlineData(@"C:\images\x.png", false)]
    [InlineData(@"\\server\share\x.png", false)]
    [InlineData("x.png", false)]
    public void Http_urls_are_told_apart_from_paths(string path, bool isUrl) =>
        Assert.Equal(isUrl, UiImage.IsHttpUrl(path));

    // -------------------------------------------------------------------------
    // Scaling

    [Theory]
    // Already within bounds: left exactly alone.
    [InlineData(40, 20, 100, 100, 40, 20)]
    [InlineData(100, 100, 100, 100, 100, 100)]
    // Too wide, too tall, and both: the tighter constraint wins and the
    // proportions hold.
    [InlineData(200, 100, 100, 100, 100, 50)]
    [InlineData(100, 200, 100, 100, 50, 100)]
    [InlineData(800, 600, 400, 400, 400, 300)]
    [InlineData(4000, 10, 100, 100, 100, 1)]
    public void An_oversized_image_is_fitted_to_the_space(
        int w, int h, int maxW, int maxH, int expectedW, int expectedH) =>
        Assert.Equal(new Size(expectedW, expectedH),
                     UiImage.Fit(new Size(w, h), new Size(maxW, maxH)));

    [Fact]
    public void A_degenerate_size_produces_nothing_rather_than_a_division_by_zero()
    {
        Assert.Equal(Size.Empty, UiImage.Fit(new Size(0, 10),  new Size(100, 100)));
        Assert.Equal(Size.Empty, UiImage.Fit(new Size(10, 10), new Size(0, 100)));
        Assert.Equal(Size.Empty, UiImage.Fit(new Size(10, 10), new Size(100, -1)));
    }

    [Fact]
    public void A_fitted_image_is_scaled_by_the_control_not_resampled()
    {
        // Resampling into a new bitmap would flatten an animated GIF to its
        // first frame. Zoom leaves the original image alone and scales as it
        // draws, so the animation survives.
        using var image = UiImage.Load(TestImages.WriteToFile(TestImages.Png(400, 200), ".png"))!;
        using var box   = UiImage.Box(image, new Size(100, 100));

        Assert.Same(image, box.Image);
        Assert.Equal(new Size(100, 50), box.Size);
        Assert.Equal(PictureBoxSizeMode.Zoom, box.SizeMode);
    }

    [Fact]
    public void An_image_that_already_fits_is_shown_at_its_own_size()
    {
        using var image = UiImage.Load(TestImages.WriteToFile(TestImages.Png(40, 20), ".png"))!;
        using var box   = UiImage.Box(image, new Size(100, 100));

        Assert.Equal(new Size(40, 20), box.Size);
        Assert.Equal(PictureBoxSizeMode.Normal, box.SizeMode);
    }

    // -------------------------------------------------------------------------
    // Icons

    [Fact]
    public void An_icon_is_loaded_from_a_non_ico_image()
    {
        // The attribute is called Icon, but nothing stops a configuration
        // pointing it at a PNG, and the original's own samples are casual about
        // this sort of thing.
        var path = TestImages.WriteToFile(TestImages.Png(32, 32), ".png");

        try
        {
            var log = new RecordingLog();
            using var icon = UiImage.LoadIcon(path, log);

            Assert.NotNull(icon);
            Assert.False(log.Warned);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void A_missing_icon_is_reported_and_returns_nothing()
    {
        var log = new RecordingLog();

        Assert.Null(UiImage.LoadIcon(@"C:\no\such\app.ico", log));
        Assert.True(log.Warned);
    }
}
