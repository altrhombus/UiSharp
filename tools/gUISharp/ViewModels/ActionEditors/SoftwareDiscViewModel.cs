using System.Collections.ObjectModel;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UiSharp.Editor.Services;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;
using UiSharp.Editing;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public sealed partial class SoftwareDiscViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Condition { get; set; }

    public ObservableCollection<SoftwareMatchItem> Matches { get; } = [];
    public bool HasMatches => Matches.Count > 0;

    public SoftwareDiscViewModel(ActionNodeModel model)
    {
        _model    = model;
        Matches.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasMatches));
        Condition = Attr(C.Attributes.Condition) ?? string.Empty;

        foreach (var el in model.Node.Elements("Match"))
        {
            Matches.Add(new SoftwareMatchItem
            {
                DisplayName     = (string?)el.Attribute(C.Attributes.DisplayName)     ?? string.Empty,
                Variable        = (string?)el.Attribute(C.Attributes.Variable)        ?? string.Empty,
                Version         = (string?)el.Attribute(C.Attributes.Version)         ?? string.Empty,
                VersionOperator = (string?)el.Attribute(C.Attributes.VersionOperator) ?? string.Empty,
            });
        }
    }

    [RelayCommand]
    private void AddMatch() => Matches.Add(new SoftwareMatchItem());

    [RelayCommand]
    private void RemoveMatch(SoftwareMatchItem item) => Matches.Remove(item);

    public void FlushToNode()
    {
        Set(C.Attributes.Condition, Condition);

        _model.Node.Elements("Match").Remove();
        foreach (var match in Matches)
        {
            var el = new XElement("Match");
            SetEl(el, C.Attributes.DisplayName,     match.DisplayName);
            SetEl(el, C.Attributes.Variable,        match.Variable);
            SetEl(el, C.Attributes.Version,         match.Version);
            SetEl(el, C.Attributes.VersionOperator, match.VersionOperator);
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

public sealed partial class SoftwareMatchItem : ObservableObject
{
    [ObservableProperty] public partial string DisplayName     { get; set; }
    [ObservableProperty] public partial string Variable        { get; set; }
    [ObservableProperty] public partial string Version         { get; set; }
    [ObservableProperty] public partial string VersionOperator { get; set; }

    public static IReadOnlyList<string> VersionOperatorOptions { get; } =
        [">=", "<=", ">", "<", "=", "!="];

    public SoftwareMatchItem()
    {
        DisplayName     = string.Empty;
        Variable        = string.Empty;
        Version         = string.Empty;
        VersionOperator = string.Empty;
    }
}
