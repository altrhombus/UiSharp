using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class FileReadViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Filename   { get; set; }
    [ObservableProperty] public partial string Variable   { get; set; }
    [ObservableProperty] public partial bool   DeleteLine { get; set; }
    [ObservableProperty] public partial string Condition  { get; set; }

    public FileReadViewModel(ActionNodeModel model)
    {
        _model     = model;
        Filename   = Attr(C.Attributes.Filename) ?? string.Empty;
        Variable   = Attr(C.Attributes.Variable) ?? string.Empty;
        DeleteLine = BoolAttr(C.Attributes.DeleteLine);
        Condition  = Attr(C.Attributes.Condition) ?? string.Empty;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.Filename,   Filename);
        Set(C.Attributes.Variable,   Variable);
        SetBool(C.Attributes.DeleteLine, DeleteLine);
        Set(C.Attributes.Condition,  Condition);
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
