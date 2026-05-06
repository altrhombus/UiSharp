using System.Collections.ObjectModel;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class InputActionViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] private string _title      = string.Empty;
    [ObservableProperty] private string _size       = string.Empty;
    [ObservableProperty] private bool   _showCancel;
    [ObservableProperty] private string _condition  = string.Empty;

    public ObservableCollection<InputFieldItem> Fields { get; } = [];

    public static IReadOnlyList<string> SizeOptions { get; } =
        [C.Values.SizeRegular, C.Values.SizeTall, C.Values.SizeExtraTall];

    public InputActionViewModel(ActionNodeModel model)
    {
        _model      = model;
        _title      = Attr(C.Attributes.Title)      ?? string.Empty;
        _size       = Attr(C.Attributes.Size)       ?? C.Values.SizeRegular;
        _showCancel = BoolAttr(C.Attributes.ShowCancel);
        _condition  = Attr(C.Attributes.Condition)  ?? string.Empty;

        foreach (var el in model.Node.Elements())
        {
            var localName = el.Name.LocalName;
            if (!IsInputElement(localName)) continue;

            Fields.Add(new InputFieldItem
            {
                ElementName = NormalizeInputName(localName),
                Variable    = (string?)el.Attribute(C.Attributes.Variable)  ?? string.Empty,
                Question    = (string?)el.Attribute(C.Attributes.Question)  ?? string.Empty,
                Condition   = (string?)el.Attribute(C.Attributes.Condition) ?? string.Empty,
                RawXml      = el.ToString(),
            });
        }
    }

    [RelayCommand]
    private void AddTextField() =>
        Fields.Add(new InputFieldItem { ElementName = C.InputTypes.Text });

    [RelayCommand]
    private void AddChoiceField() =>
        Fields.Add(new InputFieldItem { ElementName = C.InputTypes.Choice });

    [RelayCommand]
    private void AddCheckboxField() =>
        Fields.Add(new InputFieldItem { ElementName = C.InputTypes.Checkbox });

    [RelayCommand]
    private void AddInfoField() =>
        Fields.Add(new InputFieldItem { ElementName = C.InputTypes.Info });

    [RelayCommand]
    private void RemoveField(InputFieldItem item) => Fields.Remove(item);

    public void FlushToNode()
    {
        Set(C.Attributes.Title,      Title);
        Set(C.Attributes.Size,       Size);
        SetBool(C.Attributes.ShowCancel, ShowCancel);
        Set(C.Attributes.Condition,  Condition);

        // Remove existing input child elements and re-add from field list.
        _model.Node.Elements().Where(e => IsInputElement(e.Name.LocalName)).Remove();
        foreach (var field in Fields)
        {
            try
            {
                var el = XElement.Parse(field.RawXml);
                _model.Node.Add(el);
            }
            catch
            {
                // Malformed raw XML: skip rather than corrupt the document.
            }
        }
    }

    private static bool IsInputElement(string name) =>
        name.Equals(C.InputTypes.Text,       StringComparison.OrdinalIgnoreCase) ||
        name.Equals(C.InputTypes.Choice,     StringComparison.OrdinalIgnoreCase) ||
        name.Equals(C.InputTypes.Checkbox,   StringComparison.OrdinalIgnoreCase) ||
        name.Equals(C.InputTypes.Info,       StringComparison.OrdinalIgnoreCase) ||
        name.Equals(C.InputTypes.TextOld,    StringComparison.OrdinalIgnoreCase) ||
        name.Equals(C.InputTypes.ChoiceOld,  StringComparison.OrdinalIgnoreCase) ||
        name.Equals(C.InputTypes.CheckboxOld, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeInputName(string name) => name switch
    {
        var n when n.Equals(C.InputTypes.TextOld,     StringComparison.OrdinalIgnoreCase) => C.InputTypes.Text,
        var n when n.Equals(C.InputTypes.ChoiceOld,   StringComparison.OrdinalIgnoreCase) => C.InputTypes.Choice,
        var n when n.Equals(C.InputTypes.CheckboxOld, StringComparison.OrdinalIgnoreCase) => C.InputTypes.Checkbox,
        _ => name,
    };

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

public sealed partial class InputFieldItem : ObservableObject
{
    [ObservableProperty] private string _elementName = string.Empty;
    [ObservableProperty] private string _variable    = string.Empty;
    [ObservableProperty] private string _question    = string.Empty;
    [ObservableProperty] private string _condition   = string.Empty;
    [ObservableProperty] private string _rawXml      = string.Empty;
}
