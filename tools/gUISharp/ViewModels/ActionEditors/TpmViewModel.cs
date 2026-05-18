using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class TpmViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Request   { get; set; }
    [ObservableProperty] public partial string Variable  { get; set; }
    [ObservableProperty] public partial string Condition { get; set; }

    public static IReadOnlyList<string> RequestOptions { get; } =
        ["Activate", "Deactivate", "Clear", "ClearActivate", "GetInfo"];

    public TpmViewModel(ActionNodeModel model)
    {
        _model    = model;
        Request   = Attr(C.Attributes.TpmRequest) ?? string.Empty;
        Variable  = Attr(C.Attributes.Variable)   ?? string.Empty;
        Condition = Attr(C.Attributes.Condition)  ?? string.Empty;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.TpmRequest, Request);
        Set(C.Attributes.Variable,   Variable);
        Set(C.Attributes.Condition,  Condition);
    }

    private string? Attr(string name) => (string?)_model.Node.Attribute(name);
    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }
}
