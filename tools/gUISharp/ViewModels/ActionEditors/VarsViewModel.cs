using CommunityToolkit.Mvvm.ComponentModel;
using UiSharp.Editor.Services;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;
using UiSharp.Editing;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public sealed partial class VarsViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Direction { get; set; }
    [ObservableProperty] public partial string Filename  { get; set; }
    [ObservableProperty] public partial string Condition { get; set; }

    public static IReadOnlyList<string> DirectionOptions { get; } =
        [C.Values.DirectionSave, C.Values.DirectionLoad];

    public VarsViewModel(ActionNodeModel model)
    {
        _model    = model;
        Direction = Attr(C.Attributes.Direction) ?? C.Values.DirectionSave;
        Filename  = Attr(C.Attributes.Filename)  ?? C.Defaults.Filename;
        Condition = Attr(C.Attributes.Condition) ?? string.Empty;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.Direction, Direction);
        Set(C.Attributes.Filename,  Filename);
        Set(C.Attributes.Condition, Condition);
    }

    private string? Attr(string name) => (string?)_model.Node.Attribute(name);
    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }
}
