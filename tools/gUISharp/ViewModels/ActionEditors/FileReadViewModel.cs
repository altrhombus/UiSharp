using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class FileReadViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] private string _filename   = string.Empty;
    [ObservableProperty] private string _variable   = string.Empty;
    [ObservableProperty] private bool   _deleteLine;
    [ObservableProperty] private string _condition  = string.Empty;

    public FileReadViewModel(ActionNodeModel model)
    {
        _model      = model;
        _filename   = Attr(C.Attributes.Filename) ?? string.Empty;
        _variable   = Attr(C.Attributes.Variable) ?? string.Empty;
        _deleteLine = BoolAttr(C.Attributes.DeleteLine);
        _condition  = Attr(C.Attributes.Condition) ?? string.Empty;
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
