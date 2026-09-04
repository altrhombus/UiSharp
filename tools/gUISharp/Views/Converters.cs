using UiSharp.Editor.ViewModels.ActionEditors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace UiSharp.Editor.Views;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is Visibility.Visible;
}

public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is Visibility.Collapsed;
}

public sealed class BoolToTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? "Application" : "Package";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is "Application";
}

public sealed class AppTypeGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? "" : "";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

// Selects between RefTemplate and GroupTemplate for AppTree node items.
public sealed class AppTreeNodeTemplateSelector : DataTemplateSelector
{
    public DataTemplate? RefTemplate   { get; set; }
    public DataTemplate? GroupTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) =>
        item is AppTreeRefItem ? RefTemplate : GroupTemplate;

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) =>
        item is AppTreeRefItem ? RefTemplate : GroupTemplate;
}

// Converts a hex color string (e.g. "#1A3C6D" or "1A3C6D") to a SolidColorBrush.
// Returns a transparent brush if the string is null, empty, or not a valid 6-digit hex.
public sealed class HexToColorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6 && TryParseHexByte(hex, 0, out byte r)
                                && TryParseHexByte(hex, 2, out byte g)
                                && TryParseHexByte(hex, 4, out byte b))
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
            }
        }
        return new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();

    private static bool TryParseHexByte(string s, int offset, out byte result)
    {
        result = 0;
        if (!Uri.IsHexDigit(s[offset]) || !Uri.IsHexDigit(s[offset + 1])) return false;
        result = System.Convert.ToByte(s.Substring(offset, 2), 16);
        return true;
    }
}
