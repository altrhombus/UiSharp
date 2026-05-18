using System.Collections.ObjectModel;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

public sealed partial class AppTreeViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Title          { get; set; }
    [ObservableProperty] public partial string AppVarBase     { get; set; }
    [ObservableProperty] public partial string PackageVarBase { get; set; }
    [ObservableProperty] public partial string Condition      { get; set; }

    public ObservableCollection<AppTreeSetItem> Sets { get; } = [];
    public bool HasSets => Sets.Count > 0;

    public AppTreeViewModel(ActionNodeModel model)
    {
        _model         = model;
        Title          = Attr(C.Attributes.Title)          ?? string.Empty;
        AppVarBase     = Attr(C.Attributes.AppVarBase)     ?? C.Defaults.AppVarBase;
        PackageVarBase = Attr(C.Attributes.PackageVarBase) ?? C.Defaults.PackageVarBase;
        Condition      = Attr(C.Attributes.Condition)      ?? string.Empty;

        var setsEl = model.Node.Element(C.Elements.SoftwareSets);
        if (setsEl is not null)
        {
            foreach (var setEl in setsEl.Elements(C.Elements.SoftwareSet))
                Sets.Add(AppTreeSetItem.FromXml(setEl));
        }
        Sets.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSets));
    }

    [RelayCommand]
    private void AddSet() => Sets.Add(new AppTreeSetItem
    {
        Name = Sets.Count == 0 ? "Default" : $"Set {Sets.Count + 1}",
    });

    [RelayCommand]
    private void RemoveSet(AppTreeSetItem set) => Sets.Remove(set);

    public void FlushToNode()
    {
        Set(C.Attributes.Title,          Title);
        Set(C.Attributes.AppVarBase,     AppVarBase);
        Set(C.Attributes.PackageVarBase, PackageVarBase);
        Set(C.Attributes.Condition,      Condition);

        _model.Node.Element(C.Elements.SoftwareSets)?.Remove();
        if (Sets.Count > 0)
        {
            var setsEl = new XElement(C.Elements.SoftwareSets);
            foreach (var set in Sets)
                setsEl.Add(set.ToXml());
            _model.Node.Add(setsEl);
        }
    }

    private string? Attr(string name) => (string?)_model.Node.Attribute(name);
    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }
}
