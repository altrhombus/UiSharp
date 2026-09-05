using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UiSharp.Editor.Services;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;
using UiSharp.Editing;

namespace UiSharp.Editor.ViewModels;

public sealed partial class ActionListViewModel : ObservableObject, IXmlEditorSource
{
    private readonly EditorViewModelFactory _factory;
    private ActionNodeViewModel? _previousSelection;
    private bool _updatingFromXml;
    private readonly List<(ActionNodeViewModel Vm, int Start, int End)> _lineRanges = [];
    private string? _trackedVarName;
    private string? _pendingRenameFrom;
    private string? _pendingRenameTo;

    public ObservableCollection<ActionNodeViewModel> ActionTree { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(GuidedPanelTitle))]
    public partial ActionNodeViewModel? SelectedAction { get; set; }

    [ObservableProperty]
    public partial string CurrentXmlText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? XmlValidationError { get; set; }

    [ObservableProperty]
    public partial string FilterText { get; set; } = string.Empty;

    public bool HasSelection => SelectedAction is not null;

    public string GuidedPanelTitle => SelectedAction is null ? "Guided" : $"Guided — {SelectedAction.HumanTypeName}";

    public bool IsFiltering => FilterText.Length > 0;

    public string FilterSummary
    {
        get
        {
            if (!IsFiltering) return string.Empty;
            int matches = CountMatches(ActionTree);
            int total = CountAll(ActionTree);
            return $"{matches} of {total}";
        }
    }

    [ObservableProperty]
    public partial bool HasPendingRename { get; private set; }

    [ObservableProperty]
    public partial string PendingRenameMessage { get; private set; } = string.Empty;

    public IReadOnlyList<VariableEntry> DeclaredVariables { get; private set; } = [];

    /// <summary>1-indexed line range of the selected action within <see cref="CurrentXmlText"/>. (-1,-1) when nothing is selected.</summary>
    public (int Start, int End) SelectedLineRange { get; private set; } = (-1, -1);

    public event EventHandler? Dirtied;

    /// <summary>Fires when the section content returns to match the last saved/loaded state.</summary>
    public event EventHandler? BecameClean;

    /// <summary>Fires when only the selection highlight changed but the XML content itself did not.</summary>
    public event EventHandler? SelectionDecorationChanged;

    private string _cleanXml = string.Empty;

    public ActionListViewModel(EditorViewModelFactory factory)
    {
        _factory = factory;
    }

    public void LoadActions(IEnumerable<ActionNodeModel> models)
    {
        FilterText = string.Empty;
        ActionTree.Clear();
        foreach (var model in models)
        {
            AttachLeadingComment(model);
            var vm = new ActionNodeViewModel(model, _factory);
            vm.Dirtied += (_, _) => RaiseDirty();
            vm.ApplyFilter(FilterText);
            ActionTree.Add(vm);
        }
        SelectedAction = null;
        RefreshXmlFromNode();
        RefreshVariables();
        _cleanXml = CurrentXmlText;
    }

    public List<ActionNodeModel> CollectModels()
    {
        FlushAll();
        return ActionTree.Select(vm => BuildModel(vm)).ToList();
    }

    public void MarkAllActionsClean()
    {
        _cleanXml = CurrentXmlText;
        foreach (var vm in ActionTree)
            vm.MarkClean();
    }

    public void SelectAction(ActionNodeViewModel node)
    {
        FilterText     = string.Empty;
        SelectedAction = node;
    }

    public int CountSoftwareIdReferences(string softwareId)
    {
        int count = 0;
        foreach (var vm in FlattenActionTree())
            count += vm.Model.Node
                        .Descendants(C.Elements.SoftwareRef)
                        .Count(el => string.Equals(
                            (string?)el.Attribute(C.Attributes.Id),
                            softwareId,
                            StringComparison.OrdinalIgnoreCase));
        return count;
    }

    public void ReplaceSoftwareId(string oldId, string newId)
    {
        foreach (var vm in FlattenActionTree())
        {
            foreach (var el in vm.Model.Node
                                  .Descendants(C.Elements.SoftwareRef)
                                  .Where(e => string.Equals(
                                      (string?)e.Attribute(C.Attributes.Id),
                                      oldId,
                                      StringComparison.OrdinalIgnoreCase))
                                  .ToList())
            {
                el.SetAttributeValue(C.Attributes.Id, newId);
            }
        }
        RefreshXmlFromNode();
        RaiseDirty();
    }

    private IEnumerable<ActionNodeViewModel> FlattenActionTree()
    {
        return FlattenNodes(ActionTree);
        static IEnumerable<ActionNodeViewModel> FlattenNodes(IEnumerable<ActionNodeViewModel> nodes)
        {
            foreach (var vm in nodes)
            {
                yield return vm;
                foreach (var child in FlattenNodes(vm.Children))
                    yield return child;
            }
        }
    }

    // ── Cursor-driven selection ───────────────────────────────────────────────

    /// <summary>Called when the Monaco cursor moves to a new line. Updates SelectedAction without re-pushing XML.</summary>
    public void SelectAtLine(int line)
    {
        foreach (var (vm, start, end) in _lineRanges)
        {
            if (line >= start && line <= end)
            {
                if (SelectedAction != vm)
                    SelectedAction = vm;
                return;
            }
        }
    }

    // ── XML sync ──────────────────────────────────────────────────────────────

    /// <summary>Called live as the user types in Monaco.</summary>
    public void OnXmlEdited(string xml)
    {
        _updatingFromXml = true;
        CurrentXmlText = xml;
        if (TryApplyFullXmlToTree(xml))
        {
            ComputeLineRangesFromXml(xml);
            SelectionDecorationChanged?.Invoke(this, EventArgs.Empty);
            SelectedAction?.RefreshEditorViewModel();
        }
        _updatingFromXml = false;
    }

    /// <summary>Called when the XML panel gains focus — flushes the guided form to the node.</summary>
    public void SyncGuidedToXml()
    {
        if (SelectedAction?.EditorViewModel is IActionEditor editor)
            editor.FlushToNode();
        RefreshXmlFromNode();
    }

    public void RefreshXmlFromNode()
    {
        if (_updatingFromXml) return;
        var (xml, start, end) = BuildFullActionsXml();
        SelectedLineRange = (start, end);
        if (xml == CurrentXmlText)
            SelectionDecorationChanged?.Invoke(this, EventArgs.Empty);
        else
            CurrentXmlText = xml;
    }

    // ── Tree apply helpers ────────────────────────────────────────────────────

    private bool TryApplyFullXmlToTree(string xml)
    {
        try
        {
            var root = XElement.Parse(xml);
            var pairs = ActionXml.ExtractNodePairs(root);

            if (pairs.Count == ActionTree.Count)
            {
                // Count unchanged: update each node in-place (preserves VMs and scroll position).
                for (int i = 0; i < pairs.Count; i++)
                {
                    ActionXml.ApplyParsedNode(ActionTree[i].Model.Node, pairs[i].Element);
                    ActionTree[i].Model.Comment = pairs[i].Comment;
                    ActionTree[i].NotifyCommentChanged();
                }
            }
            else
            {
                // Structure changed: rebuild the entire tree from the parsed pairs.
                RebuildTreeFromPairs(pairs);
            }

            XmlValidationError = null;
            RaiseDirty();
            return true;
        }
        catch (Exception ex)
        {
            XmlValidationError = ex.Message;
            return false;
        }
    }

    private void RebuildTreeFromPairs(List<(string? Comment, XElement Element)> pairs)
    {
        // Remember which index was selected so we can restore the closest match.
        int selectedIdx = SelectedAction is not null ? ActionTree.IndexOf(SelectedAction) : -1;

        // Setting SelectedAction = null triggers OnSelectedActionChanged which unsubscribes
        // the old selection's Dirtied handler. RefreshXmlFromNode is a no-op here because
        // _updatingFromXml is true.
        SelectedAction = null;
        ActionTree.Clear();

        foreach (var (comment, el) in pairs)
        {
            var model = ActionXml.BuildModel(el);
            model.Comment = comment;
            var vm = new ActionNodeViewModel(model, _factory);
            vm.Dirtied += (_, _) => RaiseDirty();
            vm.ApplyFilter(FilterText);
            ActionTree.Add(vm);
        }

        if (ActionTree.Count > 0 && selectedIdx >= 0)
            SelectedAction = ActionTree[Math.Min(selectedIdx, ActionTree.Count - 1)];
    }



    // ── Full document XML builder ─────────────────────────────────────────────

    private (string xml, int startLine, int endLine) BuildFullActionsXml()
    {
        // Rendering and range computation live in ActionXml, where they are
        // tested against the inbound direction; this only maps the ranges onto
        // the view models they belong to.
        var (xml, ranges) = ActionXml.BuildActionsXml(ActionTree.Select(vm => vm.Model).ToList());

        _lineRanges.Clear();
        int selStart = -1, selEnd = -1;

        for (int i = 0; i < Math.Min(ranges.Count, ActionTree.Count); i++)
        {
            var (start, end) = ranges[i];
            var vm = ActionTree[i];

            _lineRanges.Add((vm, start, end));
            if (vm == SelectedAction) (selStart, selEnd) = (start, end);
        }

        return (xml, selStart, selEnd);
    }

    // Computes _lineRanges and SelectedLineRange from the user's raw XML text using
    // IXmlLineInfo so the decoration tracks actual cursor position without reformatting.
    private void ComputeLineRangesFromXml(string xml)
    {
        _lineRanges.Clear();
        SelectedLineRange = (-1, -1);

        // Range computation lives in ActionXml so it can be tested; this only
        // zips the ranges onto the view models they belong to.
        var ranges = ActionXml.ComputeElementLineRanges(xml);

        for (int i = 0; i < Math.Min(ranges.Count, ActionTree.Count); i++)
        {
            var (start, end) = ranges[i];
            var vm = ActionTree[i];

            _lineRanges.Add((vm, start, end));
            if (vm == SelectedAction)
                SelectedLineRange = (start, end);
        }
    }


    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddAction(string typeName)
    {
        var node = new XElement(C.Elements.Action,
            new XAttribute(C.Attributes.Type, typeName));
        var model = new ActionNodeModel { Node = node };
        var vm = new ActionNodeViewModel(model, _factory);
        vm.Dirtied += (_, _) => RaiseDirty();
        vm.ApplyFilter(FilterText);
        InsertAfterSelection(vm);
        SelectedAction = vm;
        RaiseDirty();
    }

    [RelayCommand]
    private void AddGroup()
    {
        var node = new XElement(C.Elements.ActionGroup,
            new XAttribute(C.Attributes.Name, "New Group"));
        var model = new ActionNodeModel { Node = node };
        var vm = new ActionNodeViewModel(model, _factory);
        vm.Dirtied += (_, _) => RaiseDirty();
        vm.ApplyFilter(FilterText);
        InsertAfterSelection(vm);
        SelectedAction = vm;
        RaiseDirty();
    }

    private void InsertAfterSelection(ActionNodeViewModel vm)
    {
        if (SelectedAction is null)
        {
            ActionTree.Add(vm);
            return;
        }
        var owning = FindOwningList(ActionTree, SelectedAction) ?? ActionTree;
        int idx = owning.IndexOf(SelectedAction);
        owning.Insert(idx + 1, vm);
    }

    public string? GetSelectedActionXml()
        => SelectedAction?.Model.Node.ToString();

    public bool TryPasteActionXml(string xml)
    {
        XElement el;
        try { el = XElement.Parse(xml); }
        catch { return false; }

        var localName = el.Name.LocalName;
        if (!localName.Equals(C.Elements.Action,      StringComparison.OrdinalIgnoreCase) &&
            !localName.Equals(C.Elements.ActionGroup, StringComparison.OrdinalIgnoreCase))
            return false;

        var model = ActionXml.BuildModel(el);
        var vm    = new ActionNodeViewModel(model, _factory);
        vm.Dirtied += (_, _) => RaiseDirty();
        vm.ApplyFilter(FilterText);
        InsertAfterSelection(vm);
        SelectedAction = vm;
        RaiseDirty();
        return true;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RemoveAction()
    {
        if (SelectedAction is null) return;
        RemoveFromTree(ActionTree, SelectedAction);
        SelectedAction = null;
        RaiseDirty();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveUp()
    {
        if (SelectedAction is null) return;
        var list = FindOwningList(ActionTree, SelectedAction) ?? ActionTree;
        var idx = list.IndexOf(SelectedAction);
        if (idx > 0) { list.Move(idx, idx - 1); RaiseDirty(); }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveDown()
    {
        if (SelectedAction is null) return;
        var list = FindOwningList(ActionTree, SelectedAction) ?? ActionTree;
        var idx = list.IndexOf(SelectedAction);
        if (idx >= 0 && idx < list.Count - 1) { list.Move(idx, idx + 1); RaiseDirty(); }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DuplicateAction()
    {
        if (SelectedAction is null) return;
        var copy = new XElement(SelectedAction.Model.Node);
        var model = ActionXml.BuildModel(copy);
        var vm = new ActionNodeViewModel(model, _factory);
        vm.Dirtied += (_, _) => RaiseDirty();
        vm.ApplyFilter(FilterText);
        var owningList = FindOwningList(ActionTree, SelectedAction) ?? ActionTree;
        int idx = owningList.IndexOf(SelectedAction);
        owningList.Insert(idx + 1, vm);
        SelectedAction = vm;
        RaiseDirty();
    }

    partial void OnSelectedActionChanged(ActionNodeViewModel? value)
    {
        if (_previousSelection is not null)
            _previousSelection.Dirtied -= OnSelectedDirtied;

        _previousSelection = value;

        if (value is not null)
            value.Dirtied += OnSelectedDirtied;

        RefreshXmlFromNode();

        _trackedVarName = value is not null ? GetDeclaredVariable(value) : null;
        DismissPendingRename();

        RemoveActionCommand.NotifyCanExecuteChanged();
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
        DuplicateActionCommand.NotifyCanExecuteChanged();
    }

    partial void OnFilterTextChanged(string value)
    {
        foreach (var vm in ActionTree)
            vm.ApplyFilter(value);
        OnPropertyChanged(nameof(IsFiltering));
        OnPropertyChanged(nameof(FilterSummary));
    }

    private void OnSelectedDirtied(object? sender, EventArgs e)
    {
        if (_updatingFromXml) return;
        if (SelectedAction?.EditorViewModel is IActionEditor editor)
            editor.FlushToNode();

        // Re-evaluate the selected leaf's dirtiness now that the node is up to date.
        SelectedAction?.ReevaluateLeafDirtiness();

        // Update group IsDirty bottom-up so parent badges reflect child state.
        PropagateGroupDirtiness(ActionTree);

        // If the full section XML returned to its saved state, signal clean.
        RefreshXmlFromNode();
        if (CurrentXmlText == _cleanXml)
        {
            foreach (var vm in ActionTree) vm.MarkClean();
            BecameClean?.Invoke(this, EventArgs.Empty);
            return;
        }

        // Re-notify display properties now that the XElement is flushed so the tree
        // label reflects the current edit rather than the previous one.
        SelectedAction?.NotifyDisplayChanged();

        // _trackedVarName is set once on selection and never updated here so it always
        // represents the original name. Compare every edit against that anchor.
        if (SelectedAction is null) return;
        string? currentVar = GetDeclaredVariable(SelectedAction);

        // Bootstrap: _trackedVarName can be null when a legacy TSVar uses the Name attribute
        // (GetDeclaredVariable only reads Variable). After the first flush migrates Name→Variable,
        // capture the real original name and skip the offer on this stabilising edit.
        if (_trackedVarName is null)
        {
            _trackedVarName = currentVar;
            return;
        }

        if (currentVar != _trackedVarName)
        {
            if (!string.IsNullOrEmpty(currentVar))
            {
                int count = CountVariableReferences(_trackedVarName!);
                if (count > 0)
                    SetPendingRename(_trackedVarName!, currentVar!, count);
                else
                    DismissPendingRename();
            }
            else
            {
                DismissPendingRename();
            }
        }
        else
        {
            DismissPendingRename();
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void RaiseDirty()
    {
        Dirtied?.Invoke(this, EventArgs.Empty);
        if (IsFiltering)
            OnPropertyChanged(nameof(FilterSummary));
        RefreshVariables();
    }

    private static void PropagateGroupDirtiness(IEnumerable<ActionNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.IsGroup)
            {
                PropagateGroupDirtiness(node.Children);
                node.IsDirty = node.Children.Any(c => c.HasAnyDirtyDescendant());
            }
        }
    }

    private void FlushAll()
    {
        foreach (var vm in ActionTree)
            vm.FlushEditsToNode();
    }

    private static ActionNodeModel BuildModel(ActionNodeViewModel vm)
    {
        var model = vm.Model;
        if (vm.IsGroup)
            model.Children.Clear();
        foreach (var child in vm.Children)
        {
            if (vm.IsGroup)
                model.Children.Add(BuildModel(child));
        }
        return model;
    }

    private static bool RemoveFromTree(ObservableCollection<ActionNodeViewModel> list, ActionNodeViewModel target)
    {
        if (list.Remove(target)) return true;
        foreach (var item in list)
            if (RemoveFromTree(item.Children, target)) return true;
        return false;
    }

    private string? _preRenameSnapshot;

    public bool HasRenameSnapshot => _preRenameSnapshot is not null;

    [RelayCommand]
    private void AcceptRename()
    {
        if (string.IsNullOrEmpty(_pendingRenameFrom) || string.IsNullOrEmpty(_pendingRenameTo)) return;
        string oldTag = $"%{_pendingRenameFrom}%";
        string newTag = $"%{_pendingRenameTo}%";
        _preRenameSnapshot = CurrentXmlText;
        OnXmlEdited(CurrentXmlText.Replace(oldTag, newTag, StringComparison.OrdinalIgnoreCase));
        DismissPendingRename();
    }

    [RelayCommand]
    private void UndoRename()
    {
        if (_preRenameSnapshot is null) return;
        OnXmlEdited(_preRenameSnapshot);
        _preRenameSnapshot = null;
    }

    public void ClearRenameSnapshot() => _preRenameSnapshot = null;

    public void DismissPendingRename()
    {
        HasPendingRename = false;
        PendingRenameMessage = string.Empty;
        _pendingRenameFrom = null;
        _pendingRenameTo = null;
    }

    private void SetPendingRename(string from, string to, int count)
    {
        _pendingRenameFrom = from;
        _pendingRenameTo = to;
        PendingRenameMessage = $"Found {count} reference(s) to %{from}%. Rename to %{to}%?";
        HasPendingRename = true;
    }

    private int CountVariableReferences(string varName)
        => CountVariableReferences(varName, CurrentXmlText);

    private static int CountVariableReferences(string varName, string xml)
    {
        var tag = $"%{varName}%";
        int count = 0, idx = 0;
        while ((idx = xml.IndexOf(tag, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            idx += tag.Length;
        }
        return count;
    }

    private static string? GetDeclaredVariable(ActionNodeViewModel vm)
    {
        if (vm.IsGroup) return null;
        return vm.TypeName switch
        {
            C.ActionTypes.TSVar or C.ActionTypes.RegRead or C.ActionTypes.WmiRead or
            C.ActionTypes.FileRead or C.ActionTypes.Rest or C.ActionTypes.FromJson or
            C.ActionTypes.ToJson or C.ActionTypes.RandomString
                => (string?)vm.Model.Node.Attribute(C.Attributes.Variable),
            C.ActionTypes.ExternalCall
                => (string?)vm.Model.Node.Attribute(C.Attributes.ExitCodeVariable),
            _ => null
        };
    }

    private void RefreshVariables()
    {
        var vars = new List<VariableEntry>();
        int pos = 1;
        foreach (var vm in ActionTree)
            CollectVariables(vm, ref pos, vars);
        var xml = CurrentXmlText;
        foreach (var v in vars)
        {
            v.RefCount = CountVariableReferences(v.Name, xml);
            var tag = $"%{v.Name}%";
            int idx = 1;
            foreach (var vm in ActionTree)
                ScanForVariableUsages(vm, tag, v.Usages, ref idx);
        }
        DeclaredVariables = vars;
        OnPropertyChanged(nameof(DeclaredVariables));
    }

    private static void ScanForVariableUsages(
        ActionNodeViewModel vm, string tag, List<VariableUsage> usages, ref int actionIndex)
    {
        // Scan root action attributes
        foreach (var attr in vm.Model.Node.Attributes())
        {
            if (attr.Value.Contains(tag, StringComparison.OrdinalIgnoreCase))
                usages.Add(new(vm, actionIndex, FriendlyAttributeName(attr.Name.LocalName)));
        }
        // For leaf actions, scan all descendant elements (Switch cases, Input fields, etc.).
        // Groups skip this because their child actions are visited as separate vm.Children.
        if (!vm.IsGroup)
        {
            foreach (var descendant in vm.Model.Node.Descendants())
            {
                var elName = descendant.Name.LocalName;
                foreach (var attr in descendant.Attributes())
                {
                    if (attr.Value.Contains(tag, StringComparison.OrdinalIgnoreCase))
                        usages.Add(new(vm, actionIndex,
                            $"{elName} · {FriendlyAttributeName(attr.Name.LocalName)}"));
                }
            }
        }
        actionIndex++;
        foreach (var childVm in vm.Children)
            ScanForVariableUsages(childVm, tag, usages, ref actionIndex);
    }

    private static string FriendlyAttributeName(string xmlName) => xmlName switch
    {
        "Condition"        => "Condition",
        "OnValue"          => "On Value",
        "Default"          => "Default",
        "Variable"         => "Variable",
        "ExitCodeVariable" => "Exit Code Var",
        "Title"            => "Title",
        "Value"            => "Value",
        "Text"             => "Text",
        "Description"      => "Description",
        "WarnDescription"  => "Warn Description",
        "ErrorDescription" => "Error Description",
        _                  => xmlName,
    };

    private static void CollectVariables(ActionNodeViewModel vm, ref int pos, List<VariableEntry> vars)
    {
        if (!vm.IsGroup)
        {
            var varAttr = (string?)vm.Model.Node.Attribute(C.Attributes.Variable);
            if (!string.IsNullOrWhiteSpace(varAttr))
                vars.Add(new(varAttr!, vm.TypeName, pos));

            var exitVar = (string?)vm.Model.Node.Attribute(C.Attributes.ExitCodeVariable);
            if (!string.IsNullOrWhiteSpace(exitVar))
                vars.Add(new(exitVar!, vm.TypeName, pos));

            if (vm.TypeName == C.ActionTypes.UserInput)
            {
                foreach (var child in vm.Model.Node.Elements())
                {
                    var fieldVar = (string?)child.Attribute(C.Attributes.Variable);
                    if (!string.IsNullOrWhiteSpace(fieldVar))
                        vars.Add(new(fieldVar!, child.Name.LocalName, pos));
                }
            }
        }
        pos++;
        foreach (var child in vm.Children)
            CollectVariables(child, ref pos, vars);
    }

    private static int CountMatches(IEnumerable<ActionNodeViewModel> nodes)
    {
        int count = 0;
        foreach (var vm in nodes)
        {
            if (vm.IsMatch) count++;
            count += CountMatches(vm.Children);
        }
        return count;
    }

    private static int CountAll(IEnumerable<ActionNodeViewModel> nodes)
    {
        int count = 0;
        foreach (var vm in nodes)
        {
            count++;
            count += CountAll(vm.Children);
        }
        return count;
    }

    public static ObservableCollection<ActionNodeViewModel>? FindOwningList(
        ObservableCollection<ActionNodeViewModel> list, ActionNodeViewModel target)
    {
        if (list.Contains(target)) return list;
        foreach (var item in list)
        {
            var found = FindOwningList(item.Children, target);
            if (found is not null) return found;
        }
        return null;
    }

    public void MoveActionTo(
        ActionNodeViewModel vm,
        ObservableCollection<ActionNodeViewModel> sourceCollection,
        ObservableCollection<ActionNodeViewModel> targetCollection,
        int targetIndex)
    {
        sourceCollection.Remove(vm);
        targetIndex = Math.Min(targetIndex, targetCollection.Count);
        targetCollection.Insert(targetIndex, vm);
        RefreshXmlFromNode();
        RaiseDirty();
    }


    // Strips outer whitespace and per-line indentation from XComment.Value so the Note TextBox
    // sees clean text without leading spaces/newlines that a block-style comment introduces.

    private static void AttachLeadingComment(ActionNodeModel model)
    {
        var comments = new List<string>();
        var node = model.Node.PreviousNode;
        while (node is not null)
        {
            if (node is XComment comment)
            {
                comments.Insert(0, ActionXml.NormalizeComment(comment.Value));
                node = node.PreviousNode;
            }
            else if (node is XText text && string.IsNullOrWhiteSpace(text.Value))
                node = node.PreviousNode;
            else
                break;
        }
        if (comments.Count > 0)
            model.Comment = string.Join("\n", comments);
    }
}
