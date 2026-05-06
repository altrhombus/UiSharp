using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class VarsViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] private string _direction = string.Empty;
    [ObservableProperty] private string _filename  = string.Empty;
    [ObservableProperty] private string _condition = string.Empty;

    public static IReadOnlyList<string> DirectionOptions { get; } =
        [C.Values.DirectionSave, C.Values.DirectionLoad];

    public VarsViewModel(ActionNodeModel model)
    {
        _model     = model;
        _direction = Attr(C.Attributes.Direction) ?? C.Values.DirectionSave;
        _filename  = Attr(C.Attributes.Filename)  ?? C.Defaults.Filename;
        _condition = Attr(C.Attributes.Condition) ?? string.Empty;
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
