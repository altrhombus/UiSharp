using CommunityToolkit.Mvvm.ComponentModel;
using UiSharp.Editor.Services;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public sealed partial class DefaultValuesViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string ValueTypes   { get; set; }
    [ObservableProperty] public partial bool   ShowProgress { get; set; }
    [ObservableProperty] public partial string Condition    { get; set; }

    public static IReadOnlyList<string> CategoryOptions { get; } =
        [C.Defaults.DefaultValueAll, .. C.DefaultValueCategories.Ordered];

    public DefaultValuesViewModel(ActionNodeModel model)
    {
        _model       = model;
        ValueTypes   = Attr(C.Attributes.DefaultValueTypes) ?? C.Defaults.DefaultValueAll;
        ShowProgress = BoolAttr(C.Attributes.ShowProgress);
        Condition    = Attr(C.Attributes.Condition) ?? string.Empty;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.DefaultValueTypes, ValueTypes);
        SetBool(C.Attributes.ShowProgress,  ShowProgress);
        Set(C.Attributes.Condition,         Condition);
    }

    private string? Attr(string name) => (string?)_model.Node.Attribute(name);
    private bool BoolAttr(string name) =>
        string.Equals(Attr(name), C.Values.True, StringComparison.OrdinalIgnoreCase);

    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }

    private void SetBool(string name, bool val) =>
        _model.Node.SetAttributeValue(name, val ? C.Values.True : C.Values.False);
}
