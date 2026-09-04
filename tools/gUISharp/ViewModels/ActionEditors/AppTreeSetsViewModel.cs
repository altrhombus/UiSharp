using System.Collections.ObjectModel;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UiSharp.Editor.ViewModels;
using C = UiSharp.Core.Configuration.XmlConstants;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public abstract partial class AppTreeNodeBase : ObservableObject
{
    [ObservableProperty] public partial string Condition { get; set; } = string.Empty;
    public abstract bool IsGroup { get; }
    public abstract XElement ToXml();
}

public sealed partial class AppTreeRefItem : AppTreeNodeBase
{
    [ObservableProperty] public partial string SoftwareId  { get; set; } = string.Empty;
    [ObservableProperty] public partial bool   IsDefault   { get; set; }
    [ObservableProperty] public partial bool   IsRequired  { get; set; }
    [ObservableProperty] public partial bool   IsHidden    { get; set; }

    public override bool IsGroup => false;

    public ObservableCollection<SoftwareItemViewModel> Catalog => App.MainVm.Software.Items;

    public SoftwareItemViewModel? SelectedSoftware
    {
        get => App.MainVm.Software.Items.FirstOrDefault(s => s.Id == SoftwareId);
        set
        {
            if (value?.Id == SoftwareId) return;
            SoftwareId = value?.Id ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public bool IsUnresolved =>
        !string.IsNullOrEmpty(SoftwareId) &&
        !App.MainVm.Software.Items.Any(s => s.Id.Equals(SoftwareId, StringComparison.OrdinalIgnoreCase));

    partial void OnSoftwareIdChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedSoftware));
        OnPropertyChanged(nameof(IsUnresolved));
    }

    public override XElement ToXml()
    {
        var el = new XElement(C.Elements.SoftwareRef);
        if (!string.IsNullOrEmpty(SoftwareId)) el.SetAttributeValue(C.Attributes.Id,        SoftwareId);
        if (IsDefault)                          el.SetAttributeValue(C.Attributes.Default,   "True");
        if (IsRequired)                         el.SetAttributeValue(C.Attributes.Required,  "True");
        if (IsHidden)                           el.SetAttributeValue(C.Attributes.Hidden,    "True");
        if (!string.IsNullOrEmpty(Condition))   el.SetAttributeValue(C.Attributes.Condition, Condition);
        return el;
    }
}

public sealed partial class AppTreeGroupItem : AppTreeNodeBase
{
    [ObservableProperty] public partial string GroupId    { get; set; } = string.Empty;
    [ObservableProperty] public partial string Label      { get; set; } = string.Empty;
    [ObservableProperty] public partial bool   IsDefault  { get; set; }
    [ObservableProperty] public partial bool   IsRequired { get; set; }

    public override bool IsGroup => true;

    public ObservableCollection<AppTreeNodeBase> Items { get; } = [];

    [RelayCommand]
    private void AddRef() => Items.Add(new AppTreeRefItem());

    [RelayCommand]
    private void AddGroup() => Items.Add(new AppTreeGroupItem
    {
        GroupId = Guid.NewGuid().ToString("D").ToUpper(),
        Label   = "New Group",
    });

    [RelayCommand]
    private void RemoveItem(AppTreeNodeBase item) => Items.Remove(item);

    public override XElement ToXml()
    {
        var el = new XElement(C.Elements.SoftwareGroup);
        if (!string.IsNullOrEmpty(GroupId))   el.SetAttributeValue(C.Attributes.Id,        GroupId);
        if (!string.IsNullOrEmpty(Label))     el.SetAttributeValue(C.Attributes.Label,     Label);
        if (IsDefault)                        el.SetAttributeValue(C.Attributes.Default,   "True");
        if (IsRequired)                       el.SetAttributeValue(C.Attributes.Required,  "True");
        if (!string.IsNullOrEmpty(Condition)) el.SetAttributeValue(C.Attributes.Condition, Condition);
        foreach (var item in Items)
            el.Add(item.ToXml());
        return el;
    }
}

public sealed partial class AppTreeSetItem : ObservableObject
{
    [ObservableProperty] public partial string Name      { get; set; } = string.Empty;
    [ObservableProperty] public partial string Condition { get; set; } = string.Empty;

    public ObservableCollection<AppTreeNodeBase> Items { get; } = [];

    [RelayCommand]
    private void AddRef() => Items.Add(new AppTreeRefItem());

    [RelayCommand]
    private void AddGroup() => Items.Add(new AppTreeGroupItem
    {
        GroupId = Guid.NewGuid().ToString("D").ToUpper(),
        Label   = "New Group",
    });

    [RelayCommand]
    private void RemoveItem(AppTreeNodeBase item) => Items.Remove(item);

    public static AppTreeSetItem FromXml(XElement el)
    {
        var set = new AppTreeSetItem
        {
            Name      = (string?)el.Attribute(C.Attributes.Name)      ?? string.Empty,
            Condition = (string?)el.Attribute(C.Attributes.Condition)  ?? string.Empty,
        };
        foreach (var child in el.Elements())
            set.Items.Add(NodeFromXml(child));
        return set;
    }

    public XElement ToXml()
    {
        var el = new XElement(C.Elements.SoftwareSet);
        if (!string.IsNullOrEmpty(Name))      el.SetAttributeValue(C.Attributes.Name,      Name);
        if (!string.IsNullOrEmpty(Condition)) el.SetAttributeValue(C.Attributes.Condition, Condition);
        foreach (var item in Items)
            el.Add(item.ToXml());
        return el;
    }

    private static AppTreeNodeBase NodeFromXml(XElement el) =>
        el.Name.LocalName == C.Elements.SoftwareRef ? BuildRef(el) : BuildGroup(el);

    private static AppTreeRefItem BuildRef(XElement el) => new()
    {
        SoftwareId = (string?)el.Attribute(C.Attributes.Id)        ?? string.Empty,
        IsDefault  = ParseBool(el.Attribute(C.Attributes.Default)),
        IsRequired = ParseBool(el.Attribute(C.Attributes.Required)),
        IsHidden   = ParseBool(el.Attribute(C.Attributes.Hidden)),
        Condition  = (string?)el.Attribute(C.Attributes.Condition) ?? string.Empty,
    };

    private static AppTreeGroupItem BuildGroup(XElement el)
    {
        var group = new AppTreeGroupItem
        {
            GroupId    = (string?)el.Attribute(C.Attributes.Id)        ?? Guid.NewGuid().ToString("D").ToUpper(),
            Label      = (string?)el.Attribute(C.Attributes.Label)     ?? string.Empty,
            IsDefault  = ParseBool(el.Attribute(C.Attributes.Default)),
            IsRequired = ParseBool(el.Attribute(C.Attributes.Required)),
            Condition  = (string?)el.Attribute(C.Attributes.Condition) ?? string.Empty,
        };
        foreach (var child in el.Elements())
            group.Items.Add(NodeFromXml(child));
        return group;
    }

    private static bool ParseBool(XAttribute? attr)
        => attr is not null && attr.Value.Equals("True", StringComparison.OrdinalIgnoreCase);
}
