using CommunityToolkit.Mvvm.ComponentModel;
using UiSharp.Editor.Services;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public sealed partial class RestViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Url       { get; set; }
    [ObservableProperty] public partial string Variable  { get; set; }
    [ObservableProperty] public partial string Json      { get; set; }
    [ObservableProperty] public partial string Method    { get; set; }
    [ObservableProperty] public partial string Condition { get; set; }

    public RestViewModel(ActionNodeModel model)
    {
        _model    = model;
        Url       = Attr(C.Attributes.Url)       ?? string.Empty;
        Variable  = Attr(C.Attributes.Variable)   ?? C.Defaults.RestVariable;
        Json      = Attr(C.Attributes.Json)       ?? string.Empty;
        Method    = Attr(C.Attributes.Method)     ?? "GET";
        Condition = Attr(C.Attributes.Condition)  ?? string.Empty;
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
