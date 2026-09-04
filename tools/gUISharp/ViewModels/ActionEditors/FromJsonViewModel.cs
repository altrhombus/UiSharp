using CommunityToolkit.Mvvm.ComponentModel;
using UiSharp.Editor.Services;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public sealed partial class FromJsonViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Variable  { get; set; }
    [ObservableProperty] public partial string Json      { get; set; }
    [ObservableProperty] public partial string Condition { get; set; }

    public FromJsonViewModel(ActionNodeModel model)
    {
        _model    = model;
        Variable  = Attr(C.Attributes.Variable)  ?? C.Defaults.JsonVariable;
        Json      = model.Node.Value.Trim();
        Condition = Attr(C.Attributes.Condition) ?? string.Empty;
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
