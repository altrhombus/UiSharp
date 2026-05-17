using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace GUISharp.Views;

public sealed partial class RegexInputBox : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(RegexInputBox),
            new PropertyMetadata(string.Empty, OnTextPropertyChanged));

    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(RegexInputBox),
            new PropertyMetadata(string.Empty, OnPlaceholderTextPropertyChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public IReadOnlyList<RegexPreset> Presets { get; } =
    [
        new("Computer name (3–15 chars)",    @"^.{3,15}$",                                                "Any 3–15 character string"),
        new("Computer name (alphanumeric)",  @"^[A-Za-z0-9\-]{1,15}$",                                   "Letters, digits, and hyphens up to 15 chars"),
        new("Exact match",                   @"^VALUE$",                                                   "Anchored full-string match"),
        new("Starts with",                   @"^PREFIX",                                                   "String begins with the given prefix"),
        new("Ends with",                     @"SUFFIX$",                                                   "String ends with the given suffix"),
        new("Word boundary (exact word)",    @"\bVALUE\b",                                                 "Whole-word match, e.g. \\bHRWI\\b"),
        new("One of several values",         @"\b(VAL1|VAL2|VAL3)\b",                                     "Match any one of the listed words"),
        new("Site code group (Switch)",      @"\b((XX)|(YY))ZZ\b",                                        "Pattern for matching grouped site codes"),
        new("Numbers only",                  @"^\d+$",                                                     "One or more digits"),
        new("IP address",                    @"^\d{1,3}(\.\d{1,3}){3}$",                                  "Basic IPv4 address format"),
        new("GUID",                          @"^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$",   "Standard 8-4-4-4-12 GUID"),
        new("Locale code",                   @"^[a-z]{2}-[A-Z]{2}$",                                      "Language-region code, e.g. en-US"),
        new("Alphanumeric only",             @"^[A-Za-z0-9]+$",                                           "Letters and digits only"),
    ];

    private bool _updatingText;

    public RegexInputBox() => InitializeComponent();

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RegexInputBox box && !box._updatingText && box.PatternBox is not null)
        {
            box.PatternBox.Text = (string)(e.NewValue ?? string.Empty);
            box.UpdateMatchResult();
        }
    }

    private static void OnPlaceholderTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RegexInputBox box && box.PatternBox is not null)
            box.PatternBox.PlaceholderText = (string)(e.NewValue ?? string.Empty);
    }

    private void PatternBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _updatingText = true;
        Text = PatternBox.Text;
        _updatingText = false;
        UpdateMatchResult();
    }

    private void TestValueBox_TextChanged(object sender, TextChangedEventArgs e)
        => UpdateMatchResult();

    private void UsePreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: RegexPreset preset })
        {
            PatternBox.Text = preset.Pattern;
            HelperFlyout.Hide();
        }
    }

    private void UpdateMatchResult()
    {
        var pattern = PatternBox?.Text ?? string.Empty;
        var testValue = TestValueBox?.Text ?? string.Empty;

        if (string.IsNullOrEmpty(testValue))
        {
            MatchResultText.Visibility = Visibility.Collapsed;
            return;
        }

        MatchResultText.Visibility = Visibility.Visible;
        try
        {
            bool isMatch = Regex.IsMatch(testValue, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
            MatchResultText.Text = isMatch ? "✓  Match" : "✗  No match";
            MatchResultText.Foreground = isMatch
                ? (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"]
                : (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
        }
        catch
        {
            MatchResultText.Text = "⚠  Invalid pattern";
            MatchResultText.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
        }
    }
}

public record RegexPreset(string Label, string Pattern, string Description);
