using CommunityToolkit.Mvvm.ComponentModel;
using UiSharp.Editor.Services;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public sealed partial class InfoFullScreenViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Name        { get; set; }
    [ObservableProperty] public partial string Title       { get; set; }
    [ObservableProperty] public partial string Image       { get; set; }
    [ObservableProperty] public partial string Condition   { get; set; }
    [ObservableProperty] public partial string MessageText { get; set; }

    public InfoFullScreenViewModel(ActionNodeModel model)
    {
        _model      = model;
        Name        = Attr(C.Attributes.Name)      ?? string.Empty;
        Title       = Attr(C.Attributes.Title)     ?? string.Empty;
        Image       = Attr(C.Attributes.Image)     ?? string.Empty;
        Condition   = Attr(C.Attributes.Condition) ?? string.Empty;
        MessageText = model.Node.Value;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.Name,      Name);
        Set(C.Attributes.Title,     Title);
        Set(C.Attributes.Image,     Image);
        Set(C.Attributes.Condition, Condition);
        _model.Node.Value = MessageText;
    }

    private string? Attr(string name) => (string?)_model.Node.Attribute(name);
    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }
}
