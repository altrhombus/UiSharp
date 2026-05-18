using System.Collections.ObjectModel;
using GUISharp.ViewModels.ActionEditors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GUISharp.Views.ActionEditors;

public sealed partial class AppTreeEditor : UserControl
{
    public AppTreeEditor()
    {
        this.InitializeComponent();
    }

    private void RemoveNodeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: AppTreeNodeBase node }) return;
        if (DataContext is not AppTreeViewModel vm) return;
        RemoveNode(vm.Sets, node);
    }

    private static void RemoveNode(
        ObservableCollection<AppTreeSetItem> sets, AppTreeNodeBase node)
    {
        foreach (var set in sets)
            if (RemoveFromItems(set.Items, node)) return;
    }

    private static bool RemoveFromItems(
        ObservableCollection<AppTreeNodeBase> items, AppTreeNodeBase node)
    {
        if (items.Remove(node)) return true;
        foreach (var group in items.OfType<AppTreeGroupItem>())
            if (RemoveFromItems(group.Items, node)) return true;
        return false;
    }
}
