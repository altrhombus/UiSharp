using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class TSVarViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Variable  { get; set; }
    [ObservableProperty] public partial string Value     { get; set; }
    [ObservableProperty] public partial string Condition { get; set; }

    public TSVarViewModel(ActionNodeModel model)
    {
        _model    = model;
        // "Variable" is preferred; "Name" is the legacy synonym still common in real files
        Variable  = Attr(C.Attributes.Variable) ?? Attr(C.Attributes.Name) ?? string.Empty;
        Value     = model.Node.Value;
        Condition = Attr(C.Attributes.Condition) ?? string.Empty;
    }

    public void FlushToNode()
    {
        // Normalize to the preferred "Variable" attribute on write; drop legacy "Name"
        Set(C.Attributes.Variable, Variable);
        _model.Node.Attribute(C.Attributes.Name)?.Remove();
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
