using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace GUISharp.Views;

public sealed partial class VariableAwareTextBox : UserControl
{
    // ── DependencyProperties ─────────────────────────────────────────────────

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(VariableAwareTextBox),
            new PropertyMetadata(string.Empty, OnTextPropertyChanged));

    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(VariableAwareTextBox),
            new PropertyMetadata(string.Empty, OnPlaceholderTextPropertyChanged));

    public static readonly DependencyProperty AcceptsReturnProperty =
        DependencyProperty.Register(nameof(AcceptsReturn), typeof(bool), typeof(VariableAwareTextBox),
            new PropertyMetadata(false, OnAcceptsReturnPropertyChanged));

    public static readonly DependencyProperty TextWrappingProperty =
        DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(VariableAwareTextBox),
            new PropertyMetadata(TextWrapping.NoWrap, OnTextWrappingPropertyChanged));

    public static readonly DependencyProperty InputMaxHeightProperty =
        DependencyProperty.Register(nameof(InputMaxHeight), typeof(double), typeof(VariableAwareTextBox),
            new PropertyMetadata(double.PositiveInfinity, OnInputMaxHeightPropertyChanged));

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

    public bool AcceptsReturn
    {
        get => (bool)GetValue(AcceptsReturnProperty);
        set => SetValue(AcceptsReturnProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public double InputMaxHeight
    {
        get => (double)GetValue(InputMaxHeightProperty);
        set => SetValue(InputMaxHeightProperty, value);
    }

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VariableAwareTextBox box && !box._updatingText && box.InputBox is not null)
            box.InputBox.Text = (string)(e.NewValue ?? string.Empty);
    }

    private static void OnPlaceholderTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VariableAwareTextBox box && box.InputBox is not null)
            box.InputBox.PlaceholderText = (string)(e.NewValue ?? string.Empty);
    }

    private static void OnAcceptsReturnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VariableAwareTextBox box && box.InputBox is not null)
            box.InputBox.AcceptsReturn = (bool)e.NewValue;
    }

    private static void OnTextWrappingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VariableAwareTextBox box && box.InputBox is not null)
            box.InputBox.TextWrapping = (TextWrapping)e.NewValue;
    }

    private static void OnInputMaxHeightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VariableAwareTextBox box && box.InputBox is not null)
            box.InputBox.MaxHeight = (double)e.NewValue;
    }

    // ── Pass-through members for TextBox compatibility ────────────────────────

    public int SelectionStart
    {
        get => InputBox.SelectionStart;
        set => InputBox.SelectionStart = value;
    }

    public int SelectionLength
    {
        get => InputBox.SelectionLength;
        set => InputBox.SelectionLength = value;
    }

    public new bool Focus(FocusState value) => InputBox.Focus(value);

    public event RoutedEventHandler? TextChanged;

    // ── Construction ──────────────────────────────────────────────────────────

    private bool _updatingText;

    public VariableAwareTextBox() => InitializeComponent();

    // ── Text sync ─────────────────────────────────────────────────────────────

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _updatingText = true;
        Text = InputBox.Text;
        _updatingText = false;
        VariableHint.Visibility = InputBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateSuggestions();
        TextChanged?.Invoke(this, new RoutedEventArgs());
    }

    // ── Keyboard navigation ───────────────────────────────────────────────────

    private void InputBox_KeyDown(object sender, KeyRoutedEventArgs e)
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

    // ── Suggestion popup ──────────────────────────────────────────────────────

    private void SuggestList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is string name)
            InsertSuggestion(name);
    }

    private void SuggestPopup_Closed(object sender, object e)
    {
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
        if (partial.Contains('%'))
        {
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
}
