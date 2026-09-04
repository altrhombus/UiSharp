using System.Collections.ObjectModel;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UiSharp.Editor.Services;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public sealed partial class WmiWriteViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Namespace    { get; set; }
    [ObservableProperty] public partial string Class        { get; set; }
    [ObservableProperty] public partial string KeyQualifier { get; set; }
    [ObservableProperty] public partial string Condition    { get; set; }

    public ObservableCollection<WmiPropertyItem> Properties { get; } = [];
    public bool HasProperties => Properties.Count > 0;

    public static IReadOnlyList<string> CimTypeOptions { get; } =
        ["CIM_STRING", "CIM_UINT32", "CIM_SINT32", "CIM_BOOLEAN", "CIM_REAL64", "CIM_DATETIME"];

    public WmiWriteViewModel(ActionNodeModel model)
    {
        _model       = model;
        Properties.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasProperties));
        Namespace    = Attr(C.Attributes.Namespace)    ?? C.Defaults.Namespace;
        Class        = Attr(C.Attributes.Class)        ?? string.Empty;
        KeyQualifier = Attr(C.Attributes.KeyQualifier) ?? string.Empty;
        Condition    = Attr(C.Attributes.Condition)    ?? string.Empty;

        foreach (var el in model.Node.Elements(C.Attributes.Property))
        {
            Properties.Add(new WmiPropertyItem
            {
                Name    = (string?)el.Attribute(C.Attributes.Name)  ?? string.Empty,
                CimType = (string?)el.Attribute(C.Attributes.Type)  ?? C.Defaults.CimType,
                Value   = (string?)el.Attribute(C.Attributes.Value) ?? string.Empty,
                IsKey   = string.Equals((string?)el.Attribute(C.Attributes.Key), C.Values.True,
                              StringComparison.OrdinalIgnoreCase),
            });
        }
    }

    [RelayCommand]
    private void AddProperty() => Properties.Add(new WmiPropertyItem());

    [RelayCommand]
    private void RemoveProperty(WmiPropertyItem item) => Properties.Remove(item);

    public void FlushToNode()
    {
        Set(C.Attributes.Namespace,    Namespace);
        Set(C.Attributes.Class,        Class);
        Set(C.Attributes.KeyQualifier, KeyQualifier);
        Set(C.Attributes.Condition,    Condition);

        _model.Node.Elements(C.Attributes.Property).Remove();
        foreach (var prop in Properties)
        {
            var el = new XElement(C.Attributes.Property);
            SetEl(el, C.Attributes.Name,  prop.Name);
            SetEl(el, C.Attributes.Type,  prop.CimType);
            SetEl(el, C.Attributes.Value, prop.Value);
            if (prop.IsKey)
                el.SetAttributeValue(C.Attributes.Key, C.Values.True);
            _model.Node.Add(el);
        }
    }

    private string? Attr(string name) => (string?)_model.Node.Attribute(name);
    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }
    private static void SetEl(XElement el, string attr, string val)
    {
        if (!string.IsNullOrEmpty(val)) el.SetAttributeValue(attr, val);
    }
}

public sealed partial class WmiPropertyItem : ObservableObject
{
    [ObservableProperty] public partial string Name    { get; set; }
    [ObservableProperty] public partial string CimType { get; set; }
    [ObservableProperty] public partial string Value   { get; set; }
    [ObservableProperty] public partial bool   IsKey   { get; set; }

    public WmiPropertyItem()
    {
        Name    = string.Empty;
        CimType = XmlConstants.Defaults.CimType;
        Value   = string.Empty;
        IsKey   = false;
    }
}
