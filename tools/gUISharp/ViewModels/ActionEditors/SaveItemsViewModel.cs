using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class SaveItemsViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] private string _path      = string.Empty;
    [ObservableProperty] private string _items     = string.Empty;
    [ObservableProperty] private string _condition = string.Empty;

    public SaveItemsViewModel(ActionNodeModel model)
    {
        _model    = model;
        _path     = Attr(C.Attributes.Path)      ?? string.Empty;
        _items    = Attr(C.Attributes.Items)      ?? string.Empty;
        _condition = Attr(C.Attributes.Condition) ?? string.Empty;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.Path,      Path);
        Set(C.Attributes.Items,     Items);
        Set(C.Attributes.Condition, Condition);
    }

    private string? Attr(string name) => (string?)_model.Node.Attribute(name);
    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }
}
