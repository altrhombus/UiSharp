using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace UiSharp.Editor.Views;

public sealed partial class InfoMarkupEditor : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(InfoMarkupEditor),
            new PropertyMetadata(string.Empty, OnTextPropertyChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private bool _updatingText;
    private Color _selectedColor = Color.FromArgb(255, 200, 60, 60);

    public InfoMarkupEditor()
    {
        InitializeComponent();
        ColorSwatch.Background = new SolidColorBrush(_selectedColor);
        ColorPickerCtrl.Color = _selectedColor;
    }

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InfoMarkupEditor ed && !ed._updatingText && ed.EditBox is not null)
        {
            ed.EditBox.Text = (string)(e.NewValue ?? string.Empty);
            ed.RefreshPreview();
        }
    }

    private void EditBox_TextChanged(object sender, RoutedEventArgs e)
    {
        _updatingText = true;
        Text = EditBox.Text;
        _updatingText = false;
        RefreshPreview();
    }

    // ── Toolbar ───────────────────────────────────────────────────────────────

    private void Bold_Click(object sender, RoutedEventArgs e)   => WrapSelection("<b>", "</b>");
    private void Italic_Click(object sender, RoutedEventArgs e) => WrapSelection("<i>", "</i>");
    private void Break_Click(object sender, RoutedEventArgs e)  => InsertAt("<br>");

    private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        _selectedColor = args.NewColor;
        ColorSwatch.Background = new SolidColorBrush(args.NewColor);
    }

    private void InsertColor_Click(object sender, RoutedEventArgs e)
    {
        string hex = $"#{_selectedColor.R:X2}{_selectedColor.G:X2}{_selectedColor.B:X2}";
        WrapSelection($"<color color=\"{hex}\">", "</color>");
        ColorFlyout.Hide();
    }

    // ── Text manipulation ─────────────────────────────────────────────────────

    private void WrapSelection(string open, string close)
    {
        int start = EditBox.SelectionStart;
        int len   = EditBox.SelectionLength;
        string selected = EditBox.Text.Substring(start, len);
        string inserted = open + selected + close;
        EditBox.Text = EditBox.Text.Remove(start, len).Insert(start, inserted);
        EditBox.SelectionStart  = len > 0 ? start + inserted.Length : start + open.Length;
        EditBox.SelectionLength = 0;
        EditBox.Focus(FocusState.Programmatic);
    }

    private void InsertAt(string text)
    {
        int pos = EditBox.SelectionStart;
        EditBox.Text = EditBox.Text.Insert(pos, text);
        EditBox.SelectionStart  = pos + text.Length;
        EditBox.SelectionLength = 0;
        EditBox.Focus(FocusState.Programmatic);
    }

    // ── Preview ───────────────────────────────────────────────────────────────

    private void RefreshPreview()
    {
        PreviewBlock.Blocks.Clear();
        var para = new Paragraph();
        foreach (var inline in ParseMarkup(EditBox?.Text ?? string.Empty))
            para.Inlines.Add(inline);
        PreviewBlock.Blocks.Add(para);
    }

    private static readonly Regex VarPattern  = new(@"(%[^%\r\n]+%)", RegexOptions.Compiled);
    private static readonly Regex HexColorRx  = new(@"#([0-9A-Fa-f]{6})", RegexOptions.Compiled);

    private static IEnumerable<Inline> ParseMarkup(string raw)
    {
        bool bold = false, italic = false;
        Color? color = null;
        var sb = new StringBuilder();

        int i = 0;
        while (i < raw.Length)
        {
            char c = raw[i];

            if (c == '<')
            {
                foreach (var inl in FlushText(sb, bold, italic, color)) yield return inl;

                int close = raw.IndexOf('>', i + 1);
                if (close < 0) { sb.Append(c); i++; continue; }

                string tag = raw[(i + 1)..close].Trim();
                i = close + 1;

                if      (tag.Equals("b",  StringComparison.OrdinalIgnoreCase)) bold   = true;
                else if (tag.Equals("/b", StringComparison.OrdinalIgnoreCase)) bold   = false;
                else if (tag.Equals("i",  StringComparison.OrdinalIgnoreCase)) italic = true;
                else if (tag.Equals("/i", StringComparison.OrdinalIgnoreCase)) italic = false;
                else if (tag.StartsWith("color", StringComparison.OrdinalIgnoreCase))
                {
                    var m = HexColorRx.Match(tag);
                    if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.HexNumber, null, out int rgb))
                        color = Color.FromArgb(255, (byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF));
                }
                else if (tag.Equals("/color", StringComparison.OrdinalIgnoreCase)) color = null;
                else if (tag.Equals("br",    StringComparison.OrdinalIgnoreCase) ||
                         tag.Equals("br/",   StringComparison.OrdinalIgnoreCase) ||
                         tag.Equals("br /",  StringComparison.OrdinalIgnoreCase))
                    yield return new LineBreak();
                // unknown tags silently ignored
            }
            else if (c == '&')
            {
                int semi = raw.IndexOf(';', i + 1);
                if (semi >= 0 && semi - i <= 7)
                {
                    sb.Append(raw[(i + 1)..semi] switch
                    {
                        "amp"  => "&",
                        "lt"   => "<",
                        "gt"   => ">",
                        "nbsp" => " ",
                        var e  => $"&{e};"
                    });
                    i = semi + 1;
                }
                else { sb.Append(c); i++; }
            }
            else { sb.Append(c); i++; }
        }
        foreach (var inl in FlushText(sb, bold, italic, color)) yield return inl;
    }

    private static IEnumerable<Inline> FlushText(StringBuilder sb, bool bold, bool italic, Color? color)
    {
        if (sb.Length == 0) yield break;
        string text = sb.ToString();
        sb.Clear();

        int pos = 0;
        foreach (Match m in VarPattern.Matches(text))
        {
            if (m.Index > pos)
                yield return MakeRun(text[pos..m.Index], bold, italic, color, isVar: false);
            yield return MakeRun(m.Value, bold, italic, color, isVar: true);
            pos = m.Index + m.Length;
        }
        if (pos < text.Length)
            yield return MakeRun(text[pos..], bold, italic, color, isVar: false);
    }

    private static Run MakeRun(string text, bool bold, bool italic, Color? color, bool isVar)
    {
        var run = new Run { Text = text };
        if (bold)            run.FontWeight = FontWeights.Bold;
        if (italic || isVar) run.FontStyle  = Windows.UI.Text.FontStyle.Italic;
        if (color.HasValue)
            run.Foreground = new SolidColorBrush(color.Value);
        else if (isVar)
            run.Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush;
        return run;
    }
}
