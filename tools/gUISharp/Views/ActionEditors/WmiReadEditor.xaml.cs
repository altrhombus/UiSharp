using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GUISharp.Views.ActionEditors;

public sealed partial class WmiReadEditor : UserControl
{
    public WmiReadEditor()
    {
        this.InitializeComponent();
        Loaded += (_, _) => RefreshUsageLink();
    }

    private void VariableNameBox_TextChanged(object sender, TextChangedEventArgs e)
        => RefreshUsageLink();

    private void RefreshUsageLink()
    {
        var varName = VariableNameBox.Text.Trim();
        if (string.IsNullOrEmpty(varName)) { ViewUsagesLink.Visibility = Visibility.Collapsed; return; }

        var entry    = App.MainVm?.ActionList.DeclaredVariables
                           .FirstOrDefault(v => v.Name.Equals(varName, StringComparison.OrdinalIgnoreCase));
        int refCount = entry?.RefCount ?? 0;
        if (refCount == 0) { ViewUsagesLink.Visibility = Visibility.Collapsed; return; }

        ViewUsagesLink.Content    = refCount == 1 ? "Used in 1 place — View usages →" : $"Used in {refCount} places — View usages →";
        ViewUsagesLink.Visibility = Visibility.Visible;
    }

    private void ViewUsagesLink_Click(object sender, RoutedEventArgs e)
    {
        var varName = VariableNameBox.Text.Trim();
        if (!string.IsNullOrEmpty(varName))
            App.MainVm?.NavigateToVariables(varName);
    }
}
