using CommunityToolkit.Mvvm.ComponentModel;
using UiSharp.Editor.Services;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;
using UiSharp.Editing;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public sealed partial class ActionGroupViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Name       { get; set; }
    [ObservableProperty] public partial string Condition  { get; set; }
    [ObservableProperty] public partial string GroupColor { get; set; }

    public ActionGroupViewModel(ActionNodeModel model)
    {
        _model     = model;
        Name       = Attr(C.Attributes.Name)       ?? string.Empty;
        Condition  = Attr(C.Attributes.Condition)  ?? string.Empty;
        GroupColor = Attr(C.Attributes.GroupColor) ?? string.Empty;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.Name,       Name);
        Set(C.Attributes.Condition,  Condition);
        Set(C.Attributes.GroupColor, GroupColor);
    }

    private string? Attr(string name) => (string?)_model.Node.Attribute(name);
    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }
}
