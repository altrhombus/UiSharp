using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace UiSharp.Editor.Views;

public sealed partial class ColorPickerField : UserControl
{
    public static readonly DependencyProperty ColorHexProperty =
        DependencyProperty.Register(nameof(ColorHex), typeof(string), typeof(ColorPickerField),
            new PropertyMetadata("#000000", OnColorHexChanged));

    public string ColorHex
    {
        get => (string)GetValue(ColorHexProperty);
        set => SetValue(ColorHexProperty, value);
    }

    private static void OnColorHexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorPickerField f)
            f.ApplyHex((string)e.NewValue, updatePicker: true);
    }

    private bool _updating;

    public ColorPickerField()
    {
        this.InitializeComponent();
        this.Loaded += (_, _) => ApplyHex(ColorHex, updatePicker: true);
    }

    private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating) return;
        var hex = HexTextBox.Text;
        var color = ParseHex(hex);
        _updating = true;
        try
        {
            SwatchPreview.Background  = new SolidColorBrush(color);
            TheColorPicker.Color      = color;
            SetValue(ColorHexProperty, hex);
        }
        finally { _updating = false; }
    }

    private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (_updating) return;
        var hex = ToHex(args.NewColor);
        _updating = true;
        try
        {
            HexTextBox.Text           = hex;
            SwatchPreview.Background  = new SolidColorBrush(args.NewColor);
            SetValue(ColorHexProperty, hex);
        }
        finally { _updating = false; }
    }

    private void PickerFlyout_Opened(object sender, object e)
    {
        if (_updating) return;
        _updating = true;
        try { TheColorPicker.Color = ParseHex(HexTextBox.Text); }
        finally { _updating = false; }
    }

    private void ApplyHex(string hex, bool updatePicker)
    {
        if (_updating) return;
        var color = ParseHex(hex);
        _updating = true;
        try
        {
            if (HexTextBox    is not null) HexTextBox.Text          = hex;
            if (SwatchPreview is not null) SwatchPreview.Background  = new SolidColorBrush(color);
            if (updatePicker  && TheColorPicker is not null) TheColorPicker.Color = color;
        }
        finally { _updating = false; }
    }

    private static Color ParseHex(string? hex)
    {
        try
        {
            var s = (hex ?? "").TrimStart('#').Trim();
            if (s.Length == 3) s = $"{s[0]}{s[0]}{s[1]}{s[1]}{s[2]}{s[2]}";
            if (s.Length == 6)
                return Color.FromArgb(255,
                    Convert.ToByte(s[0..2], 16),
                    Convert.ToByte(s[2..4], 16),
                    Convert.ToByte(s[4..6], 16));
        }
        catch { }
        return Color.FromArgb(255, 0, 0, 0);
    }

    private static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
}
