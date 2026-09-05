using System.Drawing.Imaging;

namespace UiSharp.UI.Tests;

/// <summary>
/// Images built in memory, so the tests do not depend on files checked into the
/// repository or on anything the machine happens to have.
/// </summary>
internal static class TestImages
{
    public static byte[] Png(int width = 40, int height = 20) => Encode(width, height, ImageFormat.Png);
    public static byte[] Bmp(int width = 40, int height = 20) => Encode(width, height, ImageFormat.Bmp);
    public static byte[] Gif(int width = 40, int height = 20) => Encode(width, height, ImageFormat.Gif);

    /// <summary>
    /// A two-frame animated GIF, assembled by hand: GDI+ can decode animations
    /// but will not write one.
    /// </summary>
    public static byte[] AnimatedGif()
    {
        // Header, logical screen descriptor (2x2, no global colour table),
        // Netscape looping extension, then two frames each with a local
        // two-colour table.
        var bytes = new List<byte>();

        bytes.AddRange("GIF89a"u8.ToArray());
        bytes.AddRange([2, 0, 2, 0, 0x00, 0x00, 0x00]);          // 2x2, no GCT

        // Application extension: NETSCAPE2.0, loop forever
        bytes.AddRange([0x21, 0xFF, 0x0B]);
        bytes.AddRange("NETSCAPE2.0"u8.ToArray());
        bytes.AddRange([0x03, 0x01, 0x00, 0x00, 0x00]);

        for (var frame = 0; frame < 2; frame++)
        {
            // Graphic control extension: 10/100s delay
            bytes.AddRange([0x21, 0xF9, 0x04, 0x00, 0x0A, 0x00, 0x00, 0x00]);

            // Image descriptor at 0,0, 2x2, with a local colour table of 2
            bytes.AddRange([0x2C, 0, 0, 0, 0, 2, 0, 2, 0, 0x80]);
            bytes.AddRange(frame == 0
                ? [0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF]   // red, blue
                : new byte[] { 0x00, 0xFF, 0x00, 0xFF, 0xFF, 0x00 });

            // LZW minimum code size 2, one sub-block of four uncompressed pixels
            bytes.AddRange([0x02, 0x03, 0x84, 0x51, 0x00, 0x00]);
        }

        bytes.Add(0x3B);   // trailer

        return [.. bytes];
    }

    public static string WriteToFile(byte[] bytes, string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), $"uisharp-test-{Guid.NewGuid():N}{extension}");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private static byte[] Encode(int width, int height, ImageFormat format)
    {
        using var bitmap = new Bitmap(width, height);
        using var g      = Graphics.FromImage(bitmap);

        g.Clear(Color.CornflowerBlue);

        using var stream = new MemoryStream();
        bitmap.Save(stream, format);
        return stream.ToArray();
    }
}
