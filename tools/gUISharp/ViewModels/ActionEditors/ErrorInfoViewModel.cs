using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class ErrorInfoViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Name        { get; set; }
    [ObservableProperty] public partial string Title       { get; set; }
    [ObservableProperty] public partial bool   ShowBack    { get; set; }
    [ObservableProperty] public partial string Condition   { get; set; }
    [ObservableProperty] public partial string MessageText { get; set; }

    public ErrorInfoViewModel(ActionNodeModel model)
    {
        _model      = model;
        Name        = Attr(C.Attributes.Name)      ?? string.Empty;
        Title       = Attr(C.Attributes.Title)     ?? string.Empty;
        ShowBack    = BoolAttr(C.Attributes.ShowBack);
        Condition   = Attr(C.Attributes.Condition) ?? string.Empty;
        MessageText = model.Node.Value;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.Name,      Name);
        Set(C.Attributes.Title,     Title);
        SetBool(C.Attributes.ShowBack, ShowBack);
        Set(C.Attributes.Condition, Condition);
        _model.Node.Value = MessageText;
    }

    private string? Attr(string name) => (string?)_model.Node.Attribute(name);
    private bool BoolAttr(string name) =>
        string.Equals(Attr(name), C.Values.True, StringComparison.OrdinalIgnoreCase);
    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }
    private void SetBool(string name, bool val) =>
        _model.Node.SetAttributeValue(name, val ? C.Values.True : C.Values.False);
}
