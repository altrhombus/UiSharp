using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels;

public sealed partial class ActionNodeViewModel : ObservableObject
{
    private readonly EditorViewModelFactory _factory;

    public ActionNodeModel Model { get; }

    public string TypeName => Model.TypeName;

    public bool IsGroup => Model.IsGroup;

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

    public bool HasWarning => !string.IsNullOrEmpty(WarningMessage);

    public bool HasCondition => !IsGroup && !string.IsNullOrWhiteSpace(Attr(C.Attributes.Condition));

    public string WarningMessage => TypeName switch
    {
        C.ActionTypes.TSVar     => string.IsNullOrWhiteSpace(Attr(C.Attributes.Variable))
                                   && string.IsNullOrWhiteSpace(Attr(C.Attributes.Name))
                                       ? "Variable name is empty — this action sets nothing."
                                       : string.Empty,
        C.ActionTypes.UserInput => HasInputFieldWithoutVariable()
                                       ? "One or more input fields have no Variable — the response will not be captured."
                                       : string.Empty,
        C.ActionTypes.Preflight => HasUnconditionedPreflightCheck()
                                       ? "One or more preflight checks have no condition — they will always pass."
                                       : string.Empty,
        C.ActionTypes.Switch    => !Model.Node.Elements(C.Elements.Case).Any()
                                   && !(Model.Node.Element(C.Elements.Default)?.Elements(C.Elements.Variable).Any() ?? false)
                                       ? "Switch has no cases and no default — it does nothing."
                                       : string.Empty,
        _ => string.Empty
    };

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
        OnPropertyChanged(nameof(WarningMessage));
        OnPropertyChanged(nameof(HasWarning));
        OnPropertyChanged(nameof(HasCondition));
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

    public event EventHandler? Dirtied;

    public ActionNodeViewModel(ActionNodeModel model, EditorViewModelFactory factory)
    {
        _factory = factory;
        Model = model;

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
            OnPropertyChanged(nameof(WarningMessage));
            OnPropertyChanged(nameof(HasWarning));
            OnPropertyChanged(nameof(HasCondition));
        };
        Children.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(SummaryLabel));
            OnPropertyChanged(nameof(HasSummary));
        };
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
