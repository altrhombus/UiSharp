using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GUISharp.Views;

public sealed partial class ConditionInputBox : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(ConditionInputBox),
            new PropertyMetadata(string.Empty, OnTextPropertyChanged));

    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(ConditionInputBox),
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

    public IReadOnlyList<ConditionTemplate> Templates { get; } =
    [
        new("Equals string",        "\"%VARNAME%\" = \"value\"",               "Variable equals a string value"),
        new("Not equal string",     "\"%VARNAME%\" <> \"value\"",              "Variable does not equal a string value"),
        new("Numeric ≥",            "%VARNAME% >= 1024",                       "Variable is numerically greater than or equal"),
        new("Numeric ≤",            "%VARNAME% <= 1024",                       "Variable is numerically less than or equal"),
        new("Contains substring",   "InStr(\"%VARNAME%\", \"needle\") > 0",    "Variable contains a substring"),
        new("Starts with",          "Left(\"%VARNAME%\", 4) = \"ABCD\"",       "Variable starts with a prefix"),
        new("Is empty",             "\"%VARNAME%\" = \"\"",                     "Variable is empty or not set"),
        new("Is not empty",         "\"%VARNAME%\" <> \"\"",                    "Variable has any value"),
        new("AND combinator",       " AND ",                                    "Both conditions must be true"),
        new("OR combinator",        " OR ",                                     "Either condition must be true"),
        new("NOT combinator",       "NOT ",                                     "Negate a condition"),
        new("OR chain (multi-val)", "\"%VAR%\" = \"A\" OR \"%VAR%\" = \"B\"",  "Match any one of several values"),
    ];

    private bool _updatingText;

    public ConditionInputBox() => InitializeComponent();

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ConditionInputBox box && !box._updatingText && box.InputBox is not null)
            box.InputBox.Text = (string)(e.NewValue ?? string.Empty);
    }

    private static void OnPlaceholderTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ConditionInputBox box && box.InputBox is not null)
            box.InputBox.PlaceholderText = (string)(e.NewValue ?? string.Empty);
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _updatingText = true;
        Text = InputBox.Text;
        _updatingText = false;
    }

    private void Template_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ConditionTemplate tpl })
        {
            int pos = InputBox.SelectionStart;
            int len = InputBox.SelectionLength;
            string text = InputBox.Text;
            string result = len > 0
                ? text.Remove(pos, len).Insert(pos, tpl.Template)
                : text.Insert(pos, tpl.Template);
            InputBox.Text = result;
            InputBox.SelectionStart = pos + tpl.Template.Length;
            InputBox.SelectionLength = 0;
            HelperFlyout.Hide();
            InputBox.Focus(FocusState.Programmatic);
        }
    }
}

public record ConditionTemplate(string Label, string Template, string Description);
