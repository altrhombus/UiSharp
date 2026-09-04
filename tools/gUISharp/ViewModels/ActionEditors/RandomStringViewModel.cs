using CommunityToolkit.Mvvm.ComponentModel;
using UiSharp.Editor.Services;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;
using UiSharp.Editing;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public sealed partial class RandomStringViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Variable     { get; set; }
    [ObservableProperty] public partial string AllowedChars { get; set; }
    [ObservableProperty] public partial string Length       { get; set; }
    [ObservableProperty] public partial string Condition    { get; set; }

    public RandomStringViewModel(ActionNodeModel model)
    {
        _model       = model;
        Variable     = Attr(C.Attributes.Variable)     ?? string.Empty;
        AllowedChars = Attr(C.Attributes.AllowedChars) ?? C.Defaults.AllowedChars;
        Length       = Attr(C.Attributes.Length)       ?? C.Defaults.Length.ToString();
        Condition    = Attr(C.Attributes.Condition)    ?? string.Empty;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.Variable,     Variable);
        Set(C.Attributes.AllowedChars, AllowedChars);
        Set(C.Attributes.Length,       Length);
        Set(C.Attributes.Condition,    Condition);
    }

    private string? Attr(string name) => (string?)_model.Node.Attribute(name);
    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }
}
