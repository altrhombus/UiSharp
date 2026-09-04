using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UiSharp.Editor.Services;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public sealed partial class PreflightViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Title           { get; set; }
    [ObservableProperty] public partial bool   ShowOnFailureOnly { get; set; }
    [ObservableProperty] public partial string Timeout         { get; set; }
    [ObservableProperty] public partial string TimeoutAction   { get; set; }
    [ObservableProperty] public partial string Condition       { get; set; }

    public ObservableCollection<PreflightCheckItem> Checks { get; } = [];
    public bool HasChecks => Checks.Count > 0;

    public PreflightViewModel(ActionNodeModel model)
    {
        _model           = model;
        Title            = Attr(C.Attributes.Title)          ?? string.Empty;
        ShowOnFailureOnly = BoolAttr(C.Attributes.ShowOnFailureOnly);
        Timeout          = Attr(C.Attributes.Timeout)        ?? string.Empty;
        TimeoutAction    = Attr(C.Attributes.TimeoutAction)  ?? C.Defaults.TimeoutAction;
        Condition        = Attr(C.Attributes.Condition)      ?? string.Empty;

        Checks.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasChecks));

        string? pendingCheckComment = null;
        foreach (var node in model.Node.Nodes())
        {
            if (node is XComment cmt)
                pendingCheckComment = pendingCheckComment is null ? cmt.Value.Trim() : pendingCheckComment + "\n" + cmt.Value.Trim();
            else if (node is XElement el && el.Name.LocalName == C.Elements.PreflightCheck)
            {
                Checks.Add(new PreflightCheckItem
                {
                    Text             = (string?)el.Attribute(C.Attributes.Text)             ?? string.Empty,
                    Description      = (string?)el.Attribute(C.Attributes.Description)      ?? string.Empty,
                    ErrorDescription = (string?)el.Attribute(C.Attributes.ErrorDescription) ?? string.Empty,
                    WarnDescription  = (string?)el.Attribute(C.Attributes.WarnDescription)  ?? string.Empty,
                    CheckCondition   = (string?)el.Attribute(C.Attributes.CheckCondition)   ?? string.Empty,
                    WarnCondition    = (string?)el.Attribute(C.Attributes.WarnCondition)    ?? string.Empty,
                    Condition        = (string?)el.Attribute(C.Attributes.Condition)        ?? string.Empty,
                    Comment          = pendingCheckComment                                  ?? string.Empty,
                });
                pendingCheckComment = null;
            }
        }
    }

    [RelayCommand]
    private void AddCheck() => Checks.Add(new PreflightCheckItem());

    [RelayCommand]
    private void RemoveCheck(PreflightCheckItem item) => Checks.Remove(item);

    public void CopyUiStateFrom(UiSharp.Editor.Services.IActionEditor previous)
    {
        if (previous is not PreflightViewModel prev) return;
        var expanded = prev.Checks
            .Where(c => c.IsExpanded)
            .Select(c => c.Text)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var c in Checks)
            if (expanded.Contains(c.Text))
                c.IsExpanded = true;
    }

    public void FlushToNode()
    {
        Set(C.Attributes.Title,             Title);
        SetBool(C.Attributes.ShowOnFailureOnly, ShowOnFailureOnly);
        Set(C.Attributes.Timeout,           Timeout);
        Set(C.Attributes.TimeoutAction,     TimeoutAction);
        Set(C.Attributes.Condition,         Condition);

        _model.Node.Nodes().OfType<XComment>().Remove();
        _model.Node.Elements(C.Elements.PreflightCheck).Remove();
        foreach (var item in Checks)
        {
            if (!string.IsNullOrEmpty(item.Comment))
                _model.Node.Add(new XComment(item.Comment));
            var el = new XElement(C.Elements.PreflightCheck);
            SetEl(el, C.Attributes.Text,             item.Text);
            SetEl(el, C.Attributes.Description,      item.Description);
            SetEl(el, C.Attributes.ErrorDescription, item.ErrorDescription);
            SetEl(el, C.Attributes.WarnDescription,  item.WarnDescription);
            SetEl(el, C.Attributes.CheckCondition,   item.CheckCondition);
            SetEl(el, C.Attributes.WarnCondition,    item.WarnCondition);
            SetEl(el, C.Attributes.Condition,        item.Condition);
            _model.Node.Add(el);
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
    private static void SetEl(XElement el, string attr, string val)
    {
        if (!string.IsNullOrEmpty(val)) el.SetAttributeValue(attr, val);
    }
}

public sealed partial class PreflightCheckItem : ObservableObject
{
    [ObservableProperty] public partial string Text             { get; set; }
    [ObservableProperty] public partial string Description      { get; set; }
    [ObservableProperty] public partial string ErrorDescription { get; set; }
    [ObservableProperty] public partial string WarnDescription  { get; set; }
    [ObservableProperty] public partial string CheckCondition   { get; set; }
    [ObservableProperty] public partial string WarnCondition    { get; set; }
    [ObservableProperty] public partial string Condition        { get; set; }
    [ObservableProperty] public partial bool   IsExpanded       { get; set; }
    [ObservableProperty] public partial string Comment          { get; set; }

    public ICommand ToggleExpandedCommand { get; }

    public PreflightCheckItem()
    {
        ToggleExpandedCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
        Text             = string.Empty;
        Description      = string.Empty;
        ErrorDescription = string.Empty;
        WarnDescription  = string.Empty;
        CheckCondition   = string.Empty;
        WarnCondition    = string.Empty;
        Condition        = string.Empty;
        IsExpanded       = false;
        Comment          = string.Empty;
    }
}
