using System.ComponentModel;
using System.Windows.Input;
using GUISharp.ViewModels.ActionEditors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GUISharp.Views.ActionEditors;

public sealed partial class PreflightCheckRow : UserControl
{
    public static readonly DependencyProperty RemoveCommandProperty =
        DependencyProperty.Register(nameof(RemoveCommand), typeof(ICommand), typeof(PreflightCheckRow), new PropertyMetadata(null));

    public ICommand? RemoveCommand
    {
        get => (ICommand?)GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }

    private PreflightCheckItem? _item;

    public PreflightCheckRow()
    {
        this.InitializeComponent();
        this.DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (_item is not null)
            _item.PropertyChanged -= OnItemPropertyChanged;

        _item = args.NewValue as PreflightCheckItem;

        if (_item is not null)
        {
            _item.PropertyChanged += OnItemPropertyChanged;
            HeaderText.Text = _item.Text;
            SetExpanded(_item.IsExpanded);
        }
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PreflightCheckItem.Text) && _item is not null)
            DispatcherQueue.TryEnqueue(() => HeaderText.Text = _item.Text);
    }

    private void HeaderBtn_Click(object sender, RoutedEventArgs e)
    {
        var expanding = FieldsPanel.Visibility == Visibility.Collapsed;
        SetExpanded(expanding);
        if (_item is not null) _item.IsExpanded = expanding;
    }

    private void SetExpanded(bool expanded)
    {
        FieldsPanel.Visibility     = expanded ? Visibility.Visible   : Visibility.Collapsed;
        CollapseChevron.Visibility = expanded ? Visibility.Collapsed : Visibility.Visible;
        ExpandChevron.Visibility   = expanded ? Visibility.Visible   : Visibility.Collapsed;
    }
}
