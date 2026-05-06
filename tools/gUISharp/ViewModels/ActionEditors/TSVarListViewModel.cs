using System.Collections.ObjectModel;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class TSVarListViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] private string _appVarBase     = string.Empty;
    [ObservableProperty] private string _packageVarBase = string.Empty;
    [ObservableProperty] private string _condition      = string.Empty;

    public ObservableCollection<SoftwareRefItem> SoftwareRefs { get; } = [];

    public TSVarListViewModel(ActionNodeModel model)
    {
        _model         = model;
        _appVarBase    = Attr(C.Attributes.AppVarBase)     ?? C.Defaults.AppVarBase;
        _packageVarBase = Attr(C.Attributes.PackageVarBase) ?? C.Defaults.PackageVarBase;
        _condition     = Attr(C.Attributes.Condition)      ?? string.Empty;

        foreach (var el in model.Node.Elements(C.Elements.SoftwareListRef))
        {
            SoftwareRefs.Add(new SoftwareRefItem
            {
                Id        = (string?)el.Attribute(C.Attributes.Id)        ?? string.Empty,
                Condition = (string?)el.Attribute(C.Attributes.Condition) ?? string.Empty,
            });
        }
    }

    [RelayCommand]
    private void AddRef() => SoftwareRefs.Add(new SoftwareRefItem());

    [RelayCommand]
    private void RemoveRef(SoftwareRefItem item) => SoftwareRefs.Remove(item);

    public void FlushToNode()
    {
        Set(C.Attributes.AppVarBase,     AppVarBase);
        Set(C.Attributes.PackageVarBase, PackageVarBase);
        Set(C.Attributes.Condition,      Condition);

        _model.Node.Elements(C.Elements.SoftwareListRef).Remove();
        foreach (var item in SoftwareRefs)
        {
            var el = new XElement(C.Elements.SoftwareListRef);
            el.SetAttributeValue(C.Attributes.Id, item.Id);
            if (!string.IsNullOrEmpty(item.Condition))
                el.SetAttributeValue(C.Attributes.Condition, item.Condition);
            _model.Node.Add(el);
        }
    }

    private string? Attr(string name) => (string?)_model.Node.Attribute(name);
    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }
}

public sealed partial class SoftwareRefItem : ObservableObject
{
    [ObservableProperty] private string _id        = string.Empty;
    [ObservableProperty] private string _condition = string.Empty;
}
