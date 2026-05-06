using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class DefaultValuesViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] private string _valueTypes = string.Empty;
    [ObservableProperty] private bool   _showProgress;
    [ObservableProperty] private string _condition  = string.Empty;

    public static IReadOnlyList<string> CategoryOptions { get; } =
        [C.Defaults.DefaultValueAll, .. C.DefaultValueCategories.Ordered];

    public DefaultValuesViewModel(ActionNodeModel model)
    {
        _model        = model;
        _valueTypes   = Attr(C.Attributes.DefaultValueTypes) ?? C.Defaults.DefaultValueAll;
        _showProgress = BoolAttr(C.Attributes.ShowProgress);
        _condition    = Attr(C.Attributes.Condition) ?? string.Empty;
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
