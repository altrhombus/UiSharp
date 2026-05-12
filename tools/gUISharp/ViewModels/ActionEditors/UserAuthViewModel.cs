using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class UserAuthViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Title         { get; set; }
    [ObservableProperty] public partial string Domain        { get; set; }
    [ObservableProperty] public partial string MaxRetry      { get; set; }
    [ObservableProperty] public partial string Group         { get; set; }
    [ObservableProperty] public partial bool   GetGroups     { get; set; }
    [ObservableProperty] public partial bool   DisableCancel { get; set; }
    [ObservableProperty] public partial string Condition     { get; set; }

    public UserAuthViewModel(ActionNodeModel model)
    {
        _model        = model;
        Title         = Attr(C.Attributes.Title)         ?? string.Empty;
        Domain        = Attr(C.Attributes.Domain)        ?? string.Empty;
        MaxRetry      = Attr(C.Attributes.MaxRetry)      ?? C.Defaults.MaxRetry;
        Group         = Attr(C.Attributes.Group)         ?? string.Empty;
        GetGroups     = BoolAttr(C.Attributes.GetGroups);
        DisableCancel = BoolAttr(C.Attributes.DisableCancel);
        Condition     = Attr(C.Attributes.Condition)     ?? string.Empty;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.Title,             Title);
        Set(C.Attributes.Domain,            Domain);
        Set(C.Attributes.MaxRetry,          MaxRetry);
        Set(C.Attributes.Group,             Group);
        SetBool(C.Attributes.GetGroups,     GetGroups);
        SetBool(C.Attributes.DisableCancel, DisableCancel);
        Set(C.Attributes.Condition,         Condition);
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
