using GUISharp.ViewModels.ActionEditors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GUISharp.Views.ActionEditors;

public sealed partial class InputActionEditor : UserControl
{
    public InputActionEditor() => InitializeComponent();
}

public sealed class InputFieldTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate     { get; set; }
    public DataTemplate? ChoiceTemplate   { get; set; }
    public DataTemplate? CheckboxTemplate { get; set; }
    public DataTemplate? InfoTemplate     { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) =>
        item switch
        {
            InputTextViewModel     => TextTemplate     ?? FallbackTemplate,
            InputChoiceViewModel   => ChoiceTemplate   ?? FallbackTemplate,
            InputCheckboxViewModel => CheckboxTemplate ?? FallbackTemplate,
            InputInfoViewModel     => InfoTemplate     ?? FallbackTemplate,
            _ => FallbackTemplate,
        };

    private DataTemplate FallbackTemplate =>
        TextTemplate ?? throw new InvalidOperationException("No templates assigned to InputFieldTemplateSelector.");
}
