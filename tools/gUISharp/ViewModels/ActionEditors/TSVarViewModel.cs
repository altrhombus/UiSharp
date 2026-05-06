using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class TSVarViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] private string _variable = string.Empty;
    [ObservableProperty] private string _value    = string.Empty;
    [ObservableProperty] private string _condition = string.Empty;

    public TSVarViewModel(ActionNodeModel model)
    {
        _model    = model;
        _variable = Attr(C.Attributes.Variable) ?? string.Empty;
        _value    = model.Node.Value;
        _condition = Attr(C.Attributes.Condition) ?? string.Empty;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.Variable,  Variable);
        Set(C.Attributes.Condition, Condition);
        _model.Node.Value = Value;
    }

    private string? Attr(string name)  => (string?)_model.Node.Attribute(name);
    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }
}
