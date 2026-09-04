using System.Collections.ObjectModel;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UiSharp.Editor.Services;
using UiSharp.Editor.ViewModels;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;
using UiSharp.Editing;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public sealed partial class TSVarListViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string AppVarBase     { get; set; }
    [ObservableProperty] public partial string PackageVarBase { get; set; }
    [ObservableProperty] public partial string Condition      { get; set; }

    public ObservableCollection<SoftwareRefItem> SoftwareRefs { get; } = [];

    public TSVarListViewModel(ActionNodeModel model)
    {
        _model        = model;
        AppVarBase    = Attr(C.Attributes.AppVarBase)     ?? C.Defaults.AppVarBase;
        PackageVarBase = Attr(C.Attributes.PackageVarBase) ?? C.Defaults.PackageVarBase;
        Condition     = Attr(C.Attributes.Condition)      ?? string.Empty;

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
    [ObservableProperty] public partial string Id        { get; set; }
    [ObservableProperty] public partial string Condition { get; set; }

    public ObservableCollection<SoftwareItemViewModel> Catalog => App.MainVm.Software.Items;

    public SoftwareItemViewModel? SelectedSoftware
    {
        get => App.MainVm.Software.Items.FirstOrDefault(s => s.Id == Id);
        set
        {
            Id = value?.Id ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public bool IsUnresolved =>
        !string.IsNullOrEmpty(Id) &&
        !App.MainVm.Software.Items.Any(s => s.Id.Equals(Id, StringComparison.OrdinalIgnoreCase));

    partial void OnIdChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedSoftware));
        OnPropertyChanged(nameof(IsUnresolved));
    }

    public SoftwareRefItem()
    {
        Id        = string.Empty;
        Condition = string.Empty;
    }
}
