using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class ExternalCallViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] private string _commandLine    = string.Empty;
    [ObservableProperty] private string _maxRunTime     = string.Empty;
    [ObservableProperty] private string _exitCodeVariable = string.Empty;
    [ObservableProperty] private string _condition      = string.Empty;

    public ExternalCallViewModel(ActionNodeModel model)
    {
        _model           = model;
        _commandLine     = model.Node.Value.Trim();
        _maxRunTime      = Attr(C.Attributes.MaxRunTime) ?? string.Empty;
        _exitCodeVariable = Attr(C.Attributes.ExitCodeVariable) ?? string.Empty;
        _condition       = Attr(C.Attributes.Condition) ?? string.Empty;
    }

    public void FlushToNode()
    {
        _model.Node.Value = CommandLine;
        Set(C.Attributes.MaxRunTime,      MaxRunTime);
        Set(C.Attributes.ExitCodeVariable, ExitCodeVariable);
        Set(C.Attributes.Condition,       Condition);
    }

    private string? Attr(string name) => (string?)_model.Node.Attribute(name);
    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }
}
