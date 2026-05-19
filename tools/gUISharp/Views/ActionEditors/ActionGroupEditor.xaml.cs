using GUISharp.ViewModels.ActionEditors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GUISharp.Views.ActionEditors;

public sealed partial class ActionGroupEditor : UserControl
{
    public ActionGroupEditor()
    {
        this.InitializeComponent();
    }

    private void ActionGroupEditor_Loaded(object sender, RoutedEventArgs e)
        => RefreshSwatches();

    private void Swatch_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && DataContext is ActionGroupViewModel vm)
            vm.GroupColor = rb.Tag as string ?? string.Empty;
    }

    private void RefreshSwatches()
    {
        if (DataContext is not ActionGroupViewModel vm) return;
        foreach (var rb in SwatchPanel.Children.OfType<RadioButton>())
            rb.IsChecked = (rb.Tag as string ?? string.Empty) == vm.GroupColor;
    }
}
