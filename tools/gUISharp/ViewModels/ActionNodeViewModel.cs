using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using GUISharp.ViewModels.ActionEditors;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels;

public sealed partial class ActionNodeViewModel : ObservableObject
{
    private readonly EditorViewModelFactory _factory;

    public ActionNodeModel Model { get; }

    public string TypeName => Model.TypeName;

    public bool IsGroup => Model.IsGroup;

    public Windows.UI.Text.FontWeight LabelFontWeight => IsGroup
        ? new Windows.UI.Text.FontWeight { Weight = 600 }
        : new Windows.UI.Text.FontWeight { Weight = 400 };

    public string HumanTypeName => IsGroup ? "Action Group" : TypeName switch
    {
        C.ActionTypes.TSVar         => "Variables",
        C.ActionTypes.TSVarList     => "Variable List",
        C.ActionTypes.DefaultValues => "Default Values",
        C.ActionTypes.Switch        => "Switch",
        C.ActionTypes.UserInput     => "Input Dialog",
        C.ActionTypes.Preflight     => "Preflight Checks",
        C.ActionTypes.UserInfo      => "Info Dialog",
        C.ActionTypes.UserInfoFull  => "Info (Full-Screen)",
        C.ActionTypes.ErrorInfo     => "Error Info",
        C.ActionTypes.UserAuth      => "User Authentication",
        C.ActionTypes.AppTree       => "Application Tree",
        C.ActionTypes.ExternalCall  => "External Call",
        C.ActionTypes.RandomString  => "Random String",
        C.ActionTypes.FileRead      => "File Read",
        C.ActionTypes.SaveItems     => "Save Items",
        C.ActionTypes.Vars          => "Load / Save Variables",
        C.ActionTypes.SoftwareDisc  => "Software Discovery",
        C.ActionTypes.Tpm           => "TPM Operations",
        C.ActionTypes.RegRead       => "Registry Read",
        C.ActionTypes.RegWrite      => "Registry Write",
        C.ActionTypes.WmiRead       => "WMI Read",
        C.ActionTypes.WmiWrite      => "WMI Write",
        C.ActionTypes.Rest          => "HTTP / REST Request",
        C.ActionTypes.ToJson        => "Serialize to JSON",
        C.ActionTypes.FromJson      => "Parse JSON",
        _ => TypeName
    };

    public string DisplayLabel => BuildDisplayLabel();

    public string SummaryLabel
    {
        get
        {
            if (IsGroup)
                return Children.Count == 0 ? string.Empty
                    : Children.Count == 1 ? "1 action" : $"{Children.Count} actions";
            return TypeName switch
            {
                C.ActionTypes.Preflight => FormatCount(CountElements(C.Elements.PreflightCheck), "check", "checks"),
                C.ActionTypes.UserInput => FormatCount(CountInputFields(), "field", "fields"),
                C.ActionTypes.Switch    => FormatCount(CountElements(C.Elements.Case), "case", "cases"),
                C.ActionTypes.AppTree   => FormatAppTreeSummary(),
                _ => string.Empty
            };
        }
    }

    public bool HasSummary => !string.IsNullOrEmpty(SummaryLabel);

    public string UnresolvedLabel
    {
        get
        {
            if (TypeName != C.ActionTypes.AppTree) return string.Empty;
            int n = EditorViewModel is AppTreeViewModel vm ? CountUnresolvedFromSets(vm.Sets) : 0;
            return n == 0 ? string.Empty : $"⚠ {FormatCount(n, "unresolved", "unresolved")}";
        }
    }

    public bool HasUnresolved => !string.IsNullOrEmpty(UnresolvedLabel);

    public bool HasWarning => !string.IsNullOrEmpty(WarningMessage);

    public bool HasCondition => !IsGroup && !string.IsNullOrWhiteSpace(Attr(C.Attributes.Condition));

    public string GroupColor    => EditorViewModel is ActionEditors.ActionGroupViewModel gvm ? gvm.GroupColor : string.Empty;
    public bool   HasGroupColor => !string.IsNullOrEmpty(GroupColor);

    public string WarningMessage => TypeName switch
    {
        C.ActionTypes.TSVar        => GetTSVarWarning(),
        C.ActionTypes.UserInput    => HasInputFieldWithoutVariable()
                                         ? "One or more input fields have no Variable — the response will not be captured."
                                         : string.Empty,
        C.ActionTypes.Preflight    => HasUnconditionedPreflightCheck()
                                         ? "One or more preflight checks have no condition — they will always pass."
                                         : string.Empty,
        C.ActionTypes.Switch       => GetSwitchWarning(),
        C.ActionTypes.ExternalCall => string.IsNullOrWhiteSpace(Attr(C.Attributes.ExitCodeVariable))
                                         ? "No exit code variable — cannot branch on the result."
                                         : string.Empty,
        _ => string.Empty
    };

    private string GetTSVarWarning()
    {
        if (string.IsNullOrWhiteSpace(Attr(C.Attributes.Variable))
            && string.IsNullOrWhiteSpace(Attr(C.Attributes.Name)))
            return "Variable name is empty — this action sets nothing.";
        var value = Attr(C.Attributes.Value);
        if (!string.IsNullOrEmpty(value) && value.StartsWith('"') && !value.EndsWith('"'))
            return "Value looks like an unterminated string.";
        return string.Empty;
    }

    private string GetSwitchWarning()
    {
        if (!Model.Node.Elements(C.Elements.Case).Any()
            && !(Model.Node.Element(C.Elements.Default)?.Elements(C.Elements.Variable).Any() ?? false))
            return "Switch has no cases and no default — it does nothing.";
        var onValue = Attr(C.Attributes.OnValue);
        if (!string.IsNullOrEmpty(onValue) && onValue.StartsWith('%') && onValue.EndsWith('%') && onValue.Length > 2)
        {
            var varName  = onValue[1..^1];
            var declared = App.MainVm?.ActionList.DeclaredVariables;
            if (declared is not null && !declared.Any(v => v.Name.Equals(varName, StringComparison.OrdinalIgnoreCase)))
                return $"Switch evaluates {onValue} but that variable is not declared above this point.";
        }
        return string.Empty;
    }

    public string? Comment
    {
        get => Model.Comment;
        set
        {
            // Normalize \r\n → \n (TextBox may write back Windows line endings).
            // Treat all-whitespace as null so a lone space never replaces a real comment.
            var normalized = string.IsNullOrWhiteSpace(value) ? null
                           : value.Replace("\r\n", "\n").Replace("\r", "\n");
            if (Model.Comment == normalized) return;
            Model.Comment = normalized;
            OnPropertyChanged(nameof(Comment));
            OnPropertyChanged(nameof(HasComment));
            OnPropertyChanged(nameof(CommentDisplay));
            Dirtied?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool HasComment => !string.IsNullOrEmpty(Model.Comment);

    public string CommentDisplay
    {
        get
        {
            var c = Model.Comment;
            if (string.IsNullOrEmpty(c)) return string.Empty;
            return c.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault()?.Trim() ?? string.Empty;
        }
    }

    public void NotifyCommentChanged()
    {
        OnPropertyChanged(nameof(Comment));
        OnPropertyChanged(nameof(HasComment));
        OnPropertyChanged(nameof(CommentDisplay));
    }

    public void NotifyDisplayChanged()
    {
        OnPropertyChanged(nameof(DisplayLabel));
        OnPropertyChanged(nameof(SummaryLabel));
        OnPropertyChanged(nameof(HasSummary));
        OnPropertyChanged(nameof(UnresolvedLabel));
        OnPropertyChanged(nameof(HasUnresolved));
        OnPropertyChanged(nameof(WarningMessage));
        OnPropertyChanged(nameof(HasWarning));
        OnPropertyChanged(nameof(HasCondition));
        OnPropertyChanged(nameof(GroupColor));
        OnPropertyChanged(nameof(HasGroupColor));
    }

    // Segoe MDL2 / Fluent glyph per action category, for the tree icon column.
    public string ActionIcon => IsGroup ? "" : TypeName switch
    {
        // Variables / data
        C.ActionTypes.TSVar or C.ActionTypes.TSVarList or C.ActionTypes.DefaultValues or
        C.ActionTypes.Switch or C.ActionTypes.RandomString or C.ActionTypes.Vars or
        C.ActionTypes.ToJson or C.ActionTypes.FromJson => "",

        // User-facing dialogs
        C.ActionTypes.UserInput or C.ActionTypes.Preflight or C.ActionTypes.UserInfo or
        C.ActionTypes.UserInfoFull or C.ActionTypes.ErrorInfo or
        C.ActionTypes.UserAuth or C.ActionTypes.AppTree => "",

        // Registry / WMI
        C.ActionTypes.RegRead or C.ActionTypes.RegWrite or
        C.ActionTypes.WmiRead or C.ActionTypes.WmiWrite => "",

        // Network / external execution
        C.ActionTypes.Rest or C.ActionTypes.ExternalCall => "",

        // Files, software, utilities
        _ => "",
    };

    public ObservableCollection<ActionNodeViewModel> Children { get; } = [];

    [ObservableProperty]
    public partial ObservableObject? EditorViewModel { get; set; }

    [ObservableProperty]
    public partial bool IsDirty { get; set; }

    private string _cleanNodeXml  = string.Empty;
    private string _cleanComment  = string.Empty;

    public void MarkClean()
    {
        if (!IsGroup) SnapshotCleanState();
        IsDirty = false;
        foreach (var child in Children)
            child.MarkClean();
    }

    private void SnapshotCleanState()
    {
        _cleanNodeXml = Model.Node.ToString();
        _cleanComment = Model.Comment ?? string.Empty;
    }

    /// <summary>
    /// Re-evaluates IsDirty for leaf nodes after the editor VM has been flushed to the node.
    /// Returns true if the node became clean so callers can propagate up to parent groups.
    /// </summary>
    public bool ReevaluateLeafDirtiness()
    {
        if (IsGroup) return false;
        bool dirty = Model.Node.ToString() != _cleanNodeXml
                  || (Model.Comment ?? string.Empty) != _cleanComment;
        IsDirty = dirty;
        return !dirty;
    }

    public bool HasAnyDirtyDescendant() =>
        IsDirty || Children.Any(c => c.HasAnyDirtyDescendant());

    public event EventHandler? Dirtied;

    public ActionNodeViewModel(ActionNodeModel model, EditorViewModelFactory factory)
    {
        _factory = factory;
        Model = model;
        if (!model.IsGroup) SnapshotCleanState();

        foreach (var child in model.Children)
        {
            var childVm = new ActionNodeViewModel(child, factory);
            childVm.Dirtied += (_, _) => Dirtied?.Invoke(this, EventArgs.Empty);
            Children.Add(childVm);
        }

        EditorViewModel = factory.Create(model);
        if (EditorViewModel is not null)
            EditorViewModel.PropertyChanged += (_, _) => Dirtied?.Invoke(this, EventArgs.Empty);

        Dirtied += (_, _) =>
        {
            OnPropertyChanged(nameof(DisplayLabel));
            OnPropertyChanged(nameof(SummaryLabel));
            OnPropertyChanged(nameof(HasSummary));
            OnPropertyChanged(nameof(UnresolvedLabel));
            OnPropertyChanged(nameof(HasUnresolved));
            OnPropertyChanged(nameof(WarningMessage));
            OnPropertyChanged(nameof(HasWarning));
            OnPropertyChanged(nameof(HasCondition));
            OnPropertyChanged(nameof(GroupColor));
            OnPropertyChanged(nameof(HasGroupColor));
        };
        Dirtied += (_, _) => IsDirty = true;
        Children.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(SummaryLabel));
            OnPropertyChanged(nameof(HasSummary));
        };

        if (TypeName == C.ActionTypes.AppTree)
        {
            var catalog = App.MainVm.Software.Items;
            foreach (var s in catalog)
                s.PropertyChanged += OnCatalogItemPropertyChanged;
            catalog.CollectionChanged += OnCatalogCollectionChanged;
        }
    }

    private void OnCatalogCollectionChanged(object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (SoftwareItemViewModel s in e.OldItems)
                s.PropertyChanged -= OnCatalogItemPropertyChanged;
        if (e.NewItems is not null)
            foreach (SoftwareItemViewModel s in e.NewItems)
                s.PropertyChanged += OnCatalogItemPropertyChanged;
        NotifyUnresolved();
    }

    private void OnCatalogItemPropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SoftwareItemViewModel.Id))
            NotifyUnresolved();
    }

    private void NotifyUnresolved()
    {
        OnPropertyChanged(nameof(UnresolvedLabel));
        OnPropertyChanged(nameof(HasUnresolved));
    }

    public void RefreshEditorViewModel()
    {
        var vm = _factory.Create(Model);
        if (EditorViewModel is IActionEditor old && vm is IActionEditor next)
            next.CopyUiStateFrom(old);
        vm.PropertyChanged += (_, _) => Dirtied?.Invoke(this, EventArgs.Empty);
        EditorViewModel = vm;
    }

    public void FlushEditsToNode()
    {
        if (EditorViewModel is IActionEditor editor)
            editor.FlushToNode();

        foreach (var child in Children)
            child.FlushEditsToNode();
    }

    private static string FormatCount(int n, string singular, string plural) =>
        n == 0 ? string.Empty : n == 1 ? $"1 {singular}" : $"{n} {plural}";

    private string FormatAppTreeSummary()
    {
        var setsEl = Model.Node.Element(C.Elements.SoftwareSets);
        if (setsEl is null) return string.Empty;
        int groups = setsEl.Descendants(C.Elements.SoftwareGroup).Count();
        int refs   = setsEl.Descendants(C.Elements.SoftwareRef).Count();
        if (groups == 0 && refs == 0) return string.Empty;
        var parts = new List<string>();
        if (groups > 0) parts.Add(FormatCount(groups, "group", "groups"));
        if (refs   > 0) parts.Add(FormatCount(refs,   "ref",   "refs"));
        return string.Join(", ", parts);
    }

    private static int CountUnresolvedFromSets(IEnumerable<AppTreeSetItem> sets)
    {
        var catalog = App.MainVm.Software.Items;
        return AllRefs(sets.SelectMany(s => s.Items))
            .Count(r => !string.IsNullOrEmpty(r.SoftwareId)
                     && !catalog.Any(c => c.Id.Equals(r.SoftwareId, StringComparison.OrdinalIgnoreCase)));
    }

    private static IEnumerable<AppTreeRefItem> AllRefs(IEnumerable<AppTreeNodeBase> nodes)
    {
        foreach (var n in nodes)
        {
            if (n is AppTreeRefItem r) yield return r;
            else if (n is AppTreeGroupItem g)
                foreach (var r2 in AllRefs(g.Items))
                    yield return r2;
        }
    }

    private int CountElements(string name) =>
        Model.Node.Elements(name).Count();

    private int CountInputFields() =>
        Model.Node.Elements().Count(el => el.Name.LocalName is
            C.InputTypes.Text or C.InputTypes.Choice or C.InputTypes.Checkbox or
            C.InputTypes.Info or C.InputTypes.Browse or
            C.InputTypes.TextOld or C.InputTypes.ChoiceOld or C.InputTypes.CheckboxOld);

    private bool HasInputFieldWithoutVariable()
    {
        foreach (var el in Model.Node.Elements())
        {
            if (el.Name.LocalName is not (C.InputTypes.Text or C.InputTypes.Choice or
                    C.InputTypes.Checkbox or C.InputTypes.Browse or
                    C.InputTypes.TextOld or C.InputTypes.ChoiceOld or C.InputTypes.CheckboxOld))
                continue;
            if (string.IsNullOrWhiteSpace((string?)el.Attribute(C.Attributes.Variable)))
                return true;
        }
        return false;
    }

    private bool HasUnconditionedPreflightCheck() =>
        Model.Node.Elements(C.Elements.PreflightCheck).Any(el =>
            string.IsNullOrWhiteSpace((string?)el.Attribute(C.Attributes.CheckCondition))
            && string.IsNullOrWhiteSpace((string?)el.Attribute(C.Attributes.WarnCondition)));

    private string BuildDisplayLabel()
    {
        if (IsGroup)
        {
            var name = Attr(C.Attributes.Name);
            return string.IsNullOrEmpty(name) ? "[Group]" : $"[Group] {name}";
        }

        return TypeName switch
        {
            C.ActionTypes.TSVar        => $"TSVar: {Attr(C.Attributes.Variable) ?? Attr(C.Attributes.Name) ?? "?"}",
            C.ActionTypes.ExternalCall => $"ExternalCall: {Model.Node.Value.Trim().Split('\n')[0].Trim()}",
            C.ActionTypes.DefaultValues => $"DefaultValues: {Attr(C.Attributes.DefaultValueTypes) ?? "All"}",
            C.ActionTypes.RandomString => $"RandomString → {Attr(C.Attributes.Variable) ?? "?"}",
            C.ActionTypes.FileRead     => $"FileRead: {Attr(C.Attributes.Filename) ?? "?"}",
            C.ActionTypes.Vars         => $"Vars ({Attr(C.Attributes.Direction) ?? "?"})",
            C.ActionTypes.FromJson     => $"FromJSON → {Attr(C.Attributes.Variable) ?? "?"}",
            C.ActionTypes.Rest         => $"REST: {Attr(C.Attributes.Url) ?? "?"}",
            C.ActionTypes.SaveItems    => $"SaveItems → {Attr(C.Attributes.Path) ?? "?"}",
            C.ActionTypes.ToJson       => $"ToJSON → {Attr(C.Attributes.Variable) ?? "?"}",
            C.ActionTypes.TSVarList    => "TSVarList",
            C.ActionTypes.Preflight    => $"Preflight: {Attr(C.Attributes.Title) ?? "Preflight"}",
            C.ActionTypes.UserInput    => $"Input: {Attr(C.Attributes.Title) ?? "User Input"}",
            C.ActionTypes.UserInfo     => $"Info: {Attr(C.Attributes.Title) ?? "?"}",
            C.ActionTypes.UserInfoFull => $"InfoFullScreen: {Attr(C.Attributes.Title) ?? "?"}",
            C.ActionTypes.ErrorInfo    => $"ErrorInfo: {Attr(C.Attributes.Title) ?? "?"}",
            C.ActionTypes.RegRead      => $"RegRead → {Attr(C.Attributes.Variable) ?? "?"}",
            C.ActionTypes.RegWrite     => $"RegWrite: {Attr(C.Attributes.Key) ?? "?"}",
            C.ActionTypes.AppTree      => $"AppTree: {Attr(C.Attributes.Title) ?? "?"}",
            C.ActionTypes.WmiRead      => $"WMIRead → {Attr(C.Attributes.Variable) ?? "?"}",
            C.ActionTypes.WmiWrite     => $"WMIWrite: {Attr(C.Attributes.Class) ?? "?"}",
            C.ActionTypes.UserAuth     => $"UserAuth: {Attr(C.Attributes.Title) ?? "?"}",
            C.ActionTypes.SoftwareDisc => "SoftwareDiscovery",
            C.ActionTypes.Switch       => $"Switch: {Attr(C.Attributes.OnValue) ?? "?"}",
            C.ActionTypes.Tpm          => "TPM",
            _ => string.IsNullOrEmpty(Attr(C.Attributes.Name))
                    ? TypeName
                    : $"{TypeName}: {Attr(C.Attributes.Name)}"
        };
    }

    // ── Quick find filter ─────────────────────────────────────────────────────

    private string _filterText = string.Empty;

    public bool IsMatch => _filterText.Length == 0
        || DisplayLabel.Contains(_filterText, StringComparison.OrdinalIgnoreCase)
        || TypeName.Contains(_filterText, StringComparison.OrdinalIgnoreCase);

    public double MatchOpacity => _filterText.Length == 0 || IsMatch ? 1.0 : 0.3;

    public void ApplyFilter(string filterText)
    {
        _filterText = filterText;
        OnPropertyChanged(nameof(IsMatch));
        OnPropertyChanged(nameof(MatchOpacity));
        foreach (var child in Children)
            child.ApplyFilter(filterText);
    }

    private string? Attr(string name) => (string?)Model.Node.Attribute(name);
}
