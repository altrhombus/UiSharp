using System.Collections.ObjectModel;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class SwitchViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string OnValue   { get; set; }
    [ObservableProperty] public partial bool   DontEval  { get; set; }
    [ObservableProperty] public partial string Condition { get; set; }

    public ObservableCollection<SwitchCaseItem> Cases { get; } = [];
    public ObservableCollection<VariableAssignmentItem> DefaultVariables { get; } = [];

    public SwitchViewModel(ActionNodeModel model)
    {
        _model    = model;
        OnValue   = Attr(C.Attributes.OnValue)   ?? string.Empty;
        DontEval  = BoolAttr(C.Attributes.DontEval);
        Condition = Attr(C.Attributes.Condition) ?? string.Empty;

        foreach (var el in model.Node.Elements(C.Elements.Case))
        {
            var caseItem = new SwitchCaseItem
            {
                RegEx = (string?)el.Attribute(C.Attributes.RegEx) ?? string.Empty,
            };
            foreach (var varEl in el.Elements(C.Elements.Variable))
            {
                caseItem.Variables.Add(new VariableAssignmentItem
                {
                    Name  = (string?)varEl.Attribute(C.Attributes.Name) ?? string.Empty,
                    Value = varEl.Value,
                });
            }
            Cases.Add(caseItem);
        }

        var defaultEl = model.Node.Element(C.Elements.Default);
        if (defaultEl is not null)
        {
            foreach (var varEl in defaultEl.Elements(C.Elements.Variable))
            {
                DefaultVariables.Add(new VariableAssignmentItem
                {
                    Name  = (string?)varEl.Attribute(C.Attributes.Name) ?? string.Empty,
                    Value = varEl.Value,
                });
            }
        }
    }

    [RelayCommand]
    private void AddCase() => Cases.Add(new SwitchCaseItem());

    [RelayCommand]
    private void RemoveCase(SwitchCaseItem item) => Cases.Remove(item);

    [RelayCommand]
    private void AddDefaultVariable() => DefaultVariables.Add(new VariableAssignmentItem());

    [RelayCommand]
    private void RemoveDefaultVariable(VariableAssignmentItem item) => DefaultVariables.Remove(item);

    public void FlushToNode()
    {
        Set(C.Attributes.OnValue,      OnValue);
        SetBool(C.Attributes.DontEval, DontEval);
        Set(C.Attributes.Condition,    Condition);

        _model.Node.Elements(C.Elements.Case).Remove();
        _model.Node.Elements(C.Elements.Default).Remove();

        foreach (var caseItem in Cases)
        {
            var caseEl = new XElement(C.Elements.Case);
            if (!string.IsNullOrEmpty(caseItem.RegEx))
                caseEl.SetAttributeValue(C.Attributes.RegEx, caseItem.RegEx);
            foreach (var v in caseItem.Variables)
            {
                var varEl = new XElement(C.Elements.Variable);
                if (!string.IsNullOrEmpty(v.Name))
                    varEl.SetAttributeValue(C.Attributes.Name, v.Name);
                varEl.Value = v.Value;
                caseEl.Add(varEl);
            }
            _model.Node.Add(caseEl);
        }

        if (DefaultVariables.Count > 0)
        {
            var defaultEl = new XElement(C.Elements.Default);
            foreach (var v in DefaultVariables)
            {
                var varEl = new XElement(C.Elements.Variable);
                if (!string.IsNullOrEmpty(v.Name))
                    varEl.SetAttributeValue(C.Attributes.Name, v.Name);
                varEl.Value = v.Value;
                defaultEl.Add(varEl);
            }
            _model.Node.Add(defaultEl);
        }
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

public sealed partial class SwitchCaseItem : ObservableObject
{
    [ObservableProperty] public partial string RegEx { get; set; }
    public ObservableCollection<VariableAssignmentItem> Variables { get; } = [];

    public SwitchCaseItem()
    {
        RegEx = string.Empty;
    }

    [RelayCommand]
    private void AddVariable() => Variables.Add(new VariableAssignmentItem());

    [RelayCommand]
    private void RemoveVariable(VariableAssignmentItem item) => Variables.Remove(item);
}

public sealed partial class VariableAssignmentItem : ObservableObject
{
    [ObservableProperty] public partial string Name  { get; set; }
    [ObservableProperty] public partial string Value { get; set; }

    public VariableAssignmentItem()
    {
        Name  = string.Empty;
        Value = string.Empty;
    }
}
