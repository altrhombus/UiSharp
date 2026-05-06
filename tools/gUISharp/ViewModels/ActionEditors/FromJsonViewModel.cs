using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class FromJsonViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] private string _variable  = string.Empty;
    [ObservableProperty] private string _json      = string.Empty;
    [ObservableProperty] private string _condition = string.Empty;

    public FromJsonViewModel(ActionNodeModel model)
    {
        _model    = model;
        _variable = Attr(C.Attributes.Variable)  ?? C.Defaults.JsonVariable;
        _json     = model.Node.Value.Trim();
        _condition = Attr(C.Attributes.Condition) ?? string.Empty;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.Variable,  Variable);
        Set(C.Attributes.Condition, Condition);
        _model.Node.Value = Json;
    }

    private string? Attr(string name) => (string?)_model.Node.Attribute(name);
    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }
}
