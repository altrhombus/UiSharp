using System.Collections.Specialized;
using GUISharp.ViewModels.ActionEditors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GUISharp.Views.ActionEditors;

public sealed partial class PreflightEditor : UserControl
{
    private PreflightViewModel? _vm;

    public PreflightEditor()
    {
        this.InitializeComponent();
        this.DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (_vm is not null)
            _vm.Checks.CollectionChanged -= OnChecksChanged;

        _vm = args.NewValue as PreflightViewModel;

        if (_vm is not null)
        {
            _vm.Checks.CollectionChanged += OnChecksChanged;
            RebuildChecks();
        }
    }

    private void OnChecksChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => DispatcherQueue.TryEnqueue(RebuildChecks);

    private void RebuildChecks()
    {
        ChecksPanel.Children.Clear();
        if (_vm is null) return;
        foreach (var item in _vm.Checks)
            ChecksPanel.Children.Add(new PreflightCheckRow { DataContext = item, RemoveCommand = _vm.RemoveCheckCommand });
    }
}
