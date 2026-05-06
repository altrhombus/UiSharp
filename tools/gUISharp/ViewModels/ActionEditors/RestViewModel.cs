using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class RestViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] private string _url       = string.Empty;
    [ObservableProperty] private string _variable  = string.Empty;
    [ObservableProperty] private string _json      = string.Empty;
    [ObservableProperty] private string _method    = string.Empty;
    [ObservableProperty] private string _condition = string.Empty;

    public RestViewModel(ActionNodeModel model)
    {
        _model    = model;
        _url      = Attr(C.Attributes.Url)      ?? string.Empty;
        _variable = Attr(C.Attributes.Variable)  ?? C.Defaults.RestVariable;
        _json     = Attr(C.Attributes.Json)      ?? string.Empty;
        _method   = Attr(C.Attributes.Method)    ?? string.Empty;
        _condition = Attr(C.Attributes.Condition) ?? string.Empty;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.Url,       Url);
        Set(C.Attributes.Variable,  Variable);
        Set(C.Attributes.Json,      Json);
        Set(C.Attributes.Method,    Method);
        Set(C.Attributes.Condition, Condition);
    }

    private string? Attr(string name) => (string?)_model.Node.Attribute(name);
    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }
}
