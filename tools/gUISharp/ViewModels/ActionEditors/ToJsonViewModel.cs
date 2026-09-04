using System.Collections.ObjectModel;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UiSharp.Editor.Services;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;
using UiSharp.Editing;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public sealed partial class ToJsonViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Variable  { get; set; }
    [ObservableProperty] public partial string Condition { get; set; }

    public ObservableCollection<JsonAttributeItem> Attributes { get; } = [];

    public ToJsonViewModel(ActionNodeModel model)
    {
        _model    = model;
        Variable  = Attr(C.Attributes.Variable)  ?? C.Defaults.JsonVariable;
        Condition = Attr(C.Attributes.Condition) ?? string.Empty;

        foreach (var el in model.Node.Elements(C.Elements.Attribute))
        {
            Attributes.Add(new JsonAttributeItem
            {
                Name      = (string?)el.Attribute(C.Attributes.Name)      ?? string.Empty,
                Value     = el.Value,
                Condition = (string?)el.Attribute(C.Attributes.Condition) ?? string.Empty,
            });
        }
    }

    [RelayCommand]
    private void AddAttribute() =>
        Attributes.Add(new JsonAttributeItem());

    [RelayCommand]
    private void RemoveAttribute(JsonAttributeItem item) =>
        Attributes.Remove(item);

    public void FlushToNode()
    {
        Set(C.Attributes.Variable,  Variable);
        Set(C.Attributes.Condition, Condition);

        _model.Node.Elements(C.Elements.Attribute).Remove();
        foreach (var item in Attributes)
        {
            var el = new XElement(C.Elements.Attribute, item.Value);
            el.SetAttributeValue(C.Attributes.Name, item.Name);
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

public sealed partial class JsonAttributeItem : ObservableObject
{
    [ObservableProperty] public partial string Name      { get; set; }
    [ObservableProperty] public partial string Value     { get; set; }
    [ObservableProperty] public partial string Condition { get; set; }

    public JsonAttributeItem()
    {
        Name      = string.Empty;
        Value     = string.Empty;
        Condition = string.Empty;
    }
}
