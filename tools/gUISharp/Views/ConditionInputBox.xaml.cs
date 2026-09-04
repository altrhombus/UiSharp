using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace UiSharp.Editor.Views;

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
        ConditionHint.Visibility = InputBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateSuggestions();
    }

    private void InputBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (!SuggestPopup.IsOpen) return;
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Escape:
                HideSuggestions();
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Down:
                if (SuggestList.Items.Count > 0)
                {
                    SuggestList.SelectedIndex = Math.Min(SuggestList.SelectedIndex + 1, SuggestList.Items.Count - 1);
                    SuggestList.ScrollIntoView(SuggestList.SelectedItem);
                    e.Handled = true;
                }
                break;
            case Windows.System.VirtualKey.Up:
                if (SuggestList.Items.Count > 0 && SuggestList.SelectedIndex > 0)
                {
                    SuggestList.SelectedIndex--;
                    SuggestList.ScrollIntoView(SuggestList.SelectedItem);
                    e.Handled = true;
                }
                break;
            case Windows.System.VirtualKey.Enter:
            case Windows.System.VirtualKey.Tab:
                if (SuggestList.SelectedItem is string selectedName)
                {
                    InsertSuggestion(selectedName);
                    e.Handled = true;
                }
                break;
        }
    }

    private void SuggestList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is string name)
            InsertSuggestion(name);
    }

    private void SuggestPopup_Closed(object sender, object e)
    {
        // Ensure list is cleared when light-dismissed so it doesn't reappear stale
        SuggestList.ItemsSource = null;
    }

    private void UpdateSuggestions()
    {
        var text = InputBox.Text;
        var pos = InputBox.SelectionStart;
        if (pos <= 0) { HideSuggestions(); return; }

        var before = text.Substring(0, pos);
        var lastPct = before.LastIndexOf('%');
        if (lastPct < 0) { HideSuggestions(); return; }

        var partial = before.Substring(lastPct + 1);
        if (partial.Length == 0 && lastPct == pos - 1)
        {
            // Just typed % — show all
        }
        else if (partial.Contains('%'))
        {
            // There's a closing % already — we're past a complete token
            HideSuggestions();
            return;
        }

        var vars = App.MainVm?.ActionList.DeclaredVariables;
        if (vars is null || vars.Count == 0) { HideSuggestions(); return; }

        var matches = vars
            .Where(v => v.Name.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
            .Select(v => v.Name)
            .ToList();

        if (matches.Count == 0) { HideSuggestions(); return; }

        SuggestList.ItemsSource = matches;
        SuggestList.SelectedIndex = 0;
        SuggestPopup.VerticalOffset = InputBox.ActualHeight + 2;
        SuggestPopup.HorizontalOffset = 0;
        SuggestPopup.IsOpen = true;
    }

    private void InsertSuggestion(string name)
    {
        var text = InputBox.Text;
        var pos = InputBox.SelectionStart;
        var before = text.Substring(0, pos);
        var lastPct = before.LastIndexOf('%');
        if (lastPct < 0) return;

        var insertion = $"%{name}%";
        var newText = text.Substring(0, lastPct) + insertion + text.Substring(pos);
        InputBox.Text = newText;
        InputBox.SelectionStart = lastPct + insertion.Length;
        InputBox.SelectionLength = 0;

        HideSuggestions();
        InputBox.Focus(FocusState.Programmatic);

        _updatingText = true;
        Text = InputBox.Text;
        _updatingText = false;
    }

    private void HideSuggestions()
    {
        SuggestPopup.IsOpen = false;
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
