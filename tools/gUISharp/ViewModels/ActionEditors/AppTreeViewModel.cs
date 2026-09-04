using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UiSharp.Editor.Services;
using UiSharp.Editor.ViewModels;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;
using UiSharp.Editing;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public sealed partial class AppTreeViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Title          { get; set; }
    [ObservableProperty] public partial string AppVarBase     { get; set; }
    [ObservableProperty] public partial string PackageVarBase { get; set; }
    [ObservableProperty] public partial string Condition      { get; set; }

    public ObservableCollection<AppTreeSetItem> Sets { get; } = [];
    public bool HasSets => Sets.Count > 0;

    public ObservableCollection<SoftwareItemViewModel> AvailableToAssign { get; } = [];
    public bool HasAvailableItems => AvailableToAssign.Count > 0;

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
        SubscribeStructureChanged();

        AvailableToAssign.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAvailableItems));
        App.MainVm.Software.Items.CollectionChanged += (_, _) => RefreshAvailableToAssign();
        RefreshAvailableToAssign();
    }

    // ── Structure change propagation ─────────────────────────────────────────
    // Any add/remove at any nesting level must raise PropertyChanged so that
    // ActionNodeViewModel fires Dirtied → dirty tracking + badge refresh.

    private void SubscribeStructureChanged()
    {
        Sets.CollectionChanged += OnSetsChanged;
        foreach (var set in Sets)
            SubscribeSet(set);
    }

    private void OnSetsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (AppTreeSetItem set in e.NewItems)
                SubscribeSet(set);
        OnPropertyChanged(nameof(HasSets));
        RefreshAvailableToAssign();
    }

    private void SubscribeSet(AppTreeSetItem set)
    {
        set.Items.CollectionChanged += OnItemsChanged;
        foreach (var node in set.Items)
        {
            if (node is AppTreeGroupItem g) SubscribeGroup(g);
            if (node is AppTreeRefItem r)   SubscribeRef(r);
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (AppTreeNodeBase node in e.NewItems)
            {
                if (node is AppTreeGroupItem g) SubscribeGroup(g);
                if (node is AppTreeRefItem r)   SubscribeRef(r);
            }
        OnPropertyChanged(nameof(HasSets));
        RefreshAvailableToAssign();
    }

    private void SubscribeGroup(AppTreeGroupItem group)
    {
        group.Items.CollectionChanged += OnItemsChanged;
        foreach (var node in group.Items)
        {
            if (node is AppTreeGroupItem g) SubscribeGroup(g);
            if (node is AppTreeRefItem r)   SubscribeRef(r);
        }
    }

    private void SubscribeRef(AppTreeRefItem refItem)
    {
        refItem.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppTreeRefItem.SoftwareId))
                RefreshAvailableToAssign();
        };
    }

    // ── Available to assign ──────────────────────────────────────────────────

    internal void RefreshAvailableToAssign()
    {
        var referenced = GetAllReferencedIds().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var catalog    = App.MainVm.Software.Items;

        // Remove items that are now referenced or no longer in the catalog
        for (int i = AvailableToAssign.Count - 1; i >= 0; i--)
        {
            var item = AvailableToAssign[i];
            if (referenced.Contains(item.Id) || !catalog.Contains(item))
                AvailableToAssign.RemoveAt(i);
        }

        // Add catalog items that are not yet referenced and not already in the list
        foreach (var item in catalog)
            if (!referenced.Contains(item.Id) && !AvailableToAssign.Contains(item))
                AvailableToAssign.Add(item);
    }

    private IEnumerable<string> GetAllReferencedIds() =>
        Sets.SelectMany(s => ReferencedIdsIn(s.Items));

    private static IEnumerable<string> ReferencedIdsIn(IEnumerable<AppTreeNodeBase> items)
    {
        foreach (var node in items)
        {
            if (node is AppTreeRefItem r)   { yield return r.SoftwareId; }
            if (node is AppTreeGroupItem g) { foreach (var id in ReferencedIdsIn(g.Items)) yield return id; }
        }
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private static void GoToCatalog() => App.MainVm.NavigateToSoftware();

    [RelayCommand]
    private void AddSet() => Sets.Add(new AppTreeSetItem
    {
        Name = Sets.Count == 0 ? "Default" : $"Set {Sets.Count + 1}",
    });

    [RelayCommand]
    private void RemoveSet(AppTreeSetItem set) => Sets.Remove(set);

    // ── IActionEditor ────────────────────────────────────────────────────────

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
