using System.Collections.ObjectModel;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class ToJsonViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] private string _variable  = string.Empty;
    [ObservableProperty] private string _condition = string.Empty;

    public ObservableCollection<JsonAttributeItem> Attributes { get; } = [];

    public ToJsonViewModel(ActionNodeModel model)
    {
        _model    = model;
        _variable = Attr(C.Attributes.Variable)  ?? C.Defaults.JsonVariable;
        _condition = Attr(C.Attributes.Condition) ?? string.Empty;

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
    [ObservableProperty] private string _name      = string.Empty;
    [ObservableProperty] private string _value     = string.Empty;
    [ObservableProperty] private string _condition = string.Empty;
}
