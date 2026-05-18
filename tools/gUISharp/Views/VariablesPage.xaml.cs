using System.Collections.ObjectModel;
using System.ComponentModel;
using GUISharp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;

namespace GUISharp.Views;

public sealed partial class VariablesPage : Page
{
    public ObservableCollection<VariableEntry> FilteredVariables { get; } = [];

    private string _searchText = string.Empty;
    private string _sortMode   = "position";

    public VariablesPage()
    {
        this.InitializeComponent();
        App.MainVm.ActionList.PropertyChanged += OnActionListPropertyChanged;
        Unloaded += (_, _) => App.MainVm.ActionList.PropertyChanged -= OnActionListPropertyChanged;
        RefreshFilter();
    }

    private void OnActionListPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ActionListViewModel.DeclaredVariables))
            RefreshFilter();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text;
        RefreshFilter();
    }

    private void SearchBox_EscapeInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        SearchBox.Text = string.Empty;
        args.Handled = true;
    }

    private void SortBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SortBox.SelectedItem is ComboBoxItem { Tag: string tag })
            _sortMode = tag;
        RefreshFilter();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: VariableEntry entry })
        {
            var dp = new DataPackage();
            dp.SetText(entry.NameLabel);
            Clipboard.SetContent(dp);
        }
    }

    private void RefreshFilter()
    {
        if (CountLabel is null || EmptyState is null) return;
        var source = App.MainVm.ActionList.DeclaredVariables;
        var text   = _searchText;

        IEnumerable<VariableEntry> filtered = source;

        if (text.Length > 0)
            filtered = filtered.Where(e =>
                e.Name.Contains(text,       StringComparison.OrdinalIgnoreCase) ||
                e.SourceType.Contains(text, StringComparison.OrdinalIgnoreCase));

        filtered = _sortMode switch
        {
            "name"  => filtered.OrderBy(e => e.Name,       StringComparer.OrdinalIgnoreCase),
            "type"  => filtered.OrderBy(e => e.SourceType, StringComparer.OrdinalIgnoreCase)
                                .ThenBy(e => e.Name,       StringComparer.OrdinalIgnoreCase),
            "refs"  => filtered.OrderByDescending(e => e.RefCount)
                                .ThenBy(e => e.Name,       StringComparer.OrdinalIgnoreCase),
            _       => filtered, // "position" — preserve declaration order
        };

        FilteredVariables.Clear();
        foreach (var entry in filtered)
            FilteredVariables.Add(entry);

        int count = FilteredVariables.Count;
        CountLabel.Text = count switch
        {
            0 => "0 variables",
            1 => "1 variable",
            _ => $"{count} variables"
        };
        EmptyState.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
