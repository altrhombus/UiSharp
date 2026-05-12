using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class AppTreeViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Title          { get; set; }
    [ObservableProperty] public partial string AppVarBase     { get; set; }
    [ObservableProperty] public partial string PackageVarBase { get; set; }
    [ObservableProperty] public partial string Condition      { get; set; }

    public AppTreeViewModel(ActionNodeModel model)
    {
        _model         = model;
        Title          = Attr(C.Attributes.Title)          ?? string.Empty;
        AppVarBase     = Attr(C.Attributes.AppVarBase)     ?? C.Defaults.AppVarBase;
        PackageVarBase = Attr(C.Attributes.PackageVarBase) ?? C.Defaults.PackageVarBase;
        Condition      = Attr(C.Attributes.Condition)      ?? string.Empty;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.Title,          Title);
        Set(C.Attributes.AppVarBase,     AppVarBase);
        Set(C.Attributes.PackageVarBase, PackageVarBase);
        Set(C.Attributes.Condition,      Condition);
    }

    private string? Attr(string name) => (string?)_model.Node.Attribute(name);
    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }
}
