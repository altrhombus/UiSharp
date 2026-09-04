using CommunityToolkit.Mvvm.ComponentModel;
using UiSharp.Editor.Services;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public sealed partial class WmiReadViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Namespace    { get; set; }
    [ObservableProperty] public partial string Class        { get; set; }
    [ObservableProperty] public partial string Property     { get; set; }
    [ObservableProperty] public partial string Variable     { get; set; }
    [ObservableProperty] public partial string KeyQualifier { get; set; }
    [ObservableProperty] public partial string Condition    { get; set; }

    public WmiReadViewModel(ActionNodeModel model)
    {
        _model       = model;
        Namespace    = Attr(C.Attributes.Namespace)    ?? C.Defaults.Namespace;
        Class        = Attr(C.Attributes.Class)        ?? string.Empty;
        Property     = Attr(C.Attributes.Property)     ?? string.Empty;
        Variable     = Attr(C.Attributes.Variable)     ?? string.Empty;
        KeyQualifier = Attr(C.Attributes.KeyQualifier) ?? string.Empty;
        Condition    = Attr(C.Attributes.Condition)    ?? string.Empty;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.Namespace,    Namespace);
        Set(C.Attributes.Class,        Class);
        Set(C.Attributes.Property,     Property);
        Set(C.Attributes.Variable,     Variable);
        Set(C.Attributes.KeyQualifier, KeyQualifier);
        Set(C.Attributes.Condition,    Condition);
    }

    private string? Attr(string name) => (string?)_model.Node.Attribute(name);
    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }
}
