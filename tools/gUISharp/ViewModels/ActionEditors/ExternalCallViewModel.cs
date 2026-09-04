using CommunityToolkit.Mvvm.ComponentModel;
using UiSharp.Editor.Services;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public sealed partial class ExternalCallViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string CommandLine     { get; set; }
    [ObservableProperty] public partial string MaxRunTime      { get; set; }
    [ObservableProperty] public partial string ExitCodeVariable { get; set; }
    [ObservableProperty] public partial string Condition       { get; set; }

    public ExternalCallViewModel(ActionNodeModel model)
    {
        _model          = model;
        CommandLine     = model.Node.Value.Trim();
        MaxRunTime      = Attr(C.Attributes.MaxRunTime) ?? string.Empty;
        ExitCodeVariable = Attr(C.Attributes.ExitCodeVariable) ?? string.Empty;
        Condition       = Attr(C.Attributes.Condition) ?? string.Empty;
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
