using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class RegReadViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Hive      { get; set; }
    [ObservableProperty] public partial string Key       { get; set; }
    [ObservableProperty] public partial string Value     { get; set; }
    [ObservableProperty] public partial string Variable  { get; set; }
    [ObservableProperty] public partial bool   Reg64     { get; set; }
    [ObservableProperty] public partial string Condition { get; set; }

    public static IReadOnlyList<string> HiveOptions { get; } =
        [C.Values.HiveHklm, C.Values.HiveHkcu];

    public RegReadViewModel(ActionNodeModel model)
    {
        _model    = model;
        Hive      = Attr(C.Attributes.Hive)      ?? C.Values.HiveHklm;
        Key       = Attr(C.Attributes.Key)        ?? string.Empty;
        Value     = Attr(C.Attributes.Value)      ?? string.Empty;
        Variable  = Attr(C.Attributes.Variable)   ?? string.Empty;
        Reg64     = BoolAttr(C.Attributes.Reg64);
        Condition = Attr(C.Attributes.Condition)  ?? string.Empty;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.Hive,      Hive);
        Set(C.Attributes.Key,       Key);
        Set(C.Attributes.Value,     Value);
        Set(C.Attributes.Variable,  Variable);
        SetBool(C.Attributes.Reg64, Reg64);
        Set(C.Attributes.Condition, Condition);
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
