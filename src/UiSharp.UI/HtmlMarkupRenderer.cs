using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace UiSharp.UI;

// Applies the minimal HTML-like tag subset that C++ CXHTMLCtrl supports:
//   <b>, <i>, <color color="#RRGGBB"> (any attribute containing a hex colour), <br>
// All other tags are silently ignored.  Basic entities (&amp; &lt; &gt; &nbsp;) are decoded.
internal static partial class HtmlMarkupRenderer
{
    public static void Apply(RichTextBox rtb, string html, Font baseFont, Color baseColor)
    {
        rtb.SuspendLayout();
        rtb.Clear();

        // Pre-create font variants so Flush() doesn't allocate a new GDI Font per segment.
        using var fontBold       = new Font(baseFont, FontStyle.Bold);
        using var fontItalic     = new Font(baseFont, FontStyle.Italic);
        using var fontBoldItalic = new Font(baseFont, FontStyle.Bold | FontStyle.Italic);

        bool bold   = false;
        bool italic = false;
        var  color  = baseColor;
        var  sb     = new StringBuilder();

        void Flush()
        {
            if (sb.Length == 0) return;
            rtb.SelectionFont = (bold, italic) switch
            {
                (true,  true)  => fontBoldItalic,
                (true,  false) => fontBold,
                (false, true)  => fontItalic,
                _              => baseFont,
            };
            rtb.SelectionColor = color;
            rtb.AppendText(sb.ToString());
            sb.Clear();
        }

        int i = 0;
        while (i < html.Length)
        {
            char c = html[i];

            if (c == '<')
            {
                int end = html.IndexOf('>', i);
                if (end < 0) { sb.Append(c); i++; continue; }

                var tag = html[(i + 1)..end].Trim();
                i = end + 1;

                if (tag.Equals("b", StringComparison.OrdinalIgnoreCase))
                    { Flush(); bold = true; }
                else if (tag.Equals("/b", StringComparison.OrdinalIgnoreCase))
                    { Flush(); bold = false; }
                else if (tag.Equals("i", StringComparison.OrdinalIgnoreCase))
                    { Flush(); italic = true; }
                else if (tag.Equals("/i", StringComparison.OrdinalIgnoreCase))
                    { Flush(); italic = false; }
                else if (tag.StartsWith("color", StringComparison.OrdinalIgnoreCase))
                    { Flush(); color = ParseHexColor(tag, baseColor); }
                else if (tag.Equals("/color", StringComparison.OrdinalIgnoreCase))
                    { Flush(); color = baseColor; }
                else if (tag.Equals("br", StringComparison.OrdinalIgnoreCase)   ||
                         tag.Equals("br/", StringComparison.OrdinalIgnoreCase)  ||
                         tag.Equals("br /", StringComparison.OrdinalIgnoreCase))
                    { Flush(); sb.Append('\n'); }
                // Unknown tags (including <img>) are silently ignored.
            }
            else if (c == '&')
            {
                // Decode the four basic HTML entities; pass anything else through.
                if (html.AsSpan(i).StartsWith("&amp;",  StringComparison.Ordinal)) { sb.Append('&');    i += 5; }
                else if (html.AsSpan(i).StartsWith("&lt;",   StringComparison.Ordinal)) { sb.Append('<');    i += 4; }
                else if (html.AsSpan(i).StartsWith("&gt;",   StringComparison.Ordinal)) { sb.Append('>');    i += 4; }
                else if (html.AsSpan(i).StartsWith("&nbsp;", StringComparison.Ordinal)) { sb.Append(' '); i += 6; }
                else { sb.Append(c); i++; }
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }

        Flush();
        rtb.ResumeLayout();
    }

    // Extracts the first #RRGGBB hex colour from tag content; falls back to baseColor.
    private static Color ParseHexColor(string tag, Color fallback)
    {
        var m = HexColorRegex().Match(tag);
        if (!m.Success) return fallback;
        if (!int.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
            return fallback;
        return Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
    }

    [GeneratedRegex(@"#([0-9A-Fa-f]{6})", RegexOptions.Compiled)]
    private static partial Regex HexColorRegex();
}
