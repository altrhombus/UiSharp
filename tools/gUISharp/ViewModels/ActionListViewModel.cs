using System.Collections.ObjectModel;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels;

public sealed partial class ActionListViewModel : ObservableObject
{
    private readonly EditorViewModelFactory _factory;
    private ActionNodeViewModel? _previousSelection;
    private bool _updatingFromXml;
    private bool _xmlDirtyForGuided;
    private readonly List<(ActionNodeViewModel Vm, int Start, int End)> _lineRanges = [];
    private string? _trackedVarName;
    private string? _pendingRenameFrom;
    private string? _pendingRenameTo;

    public ObservableCollection<ActionNodeViewModel> ActionTree { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    public partial ActionNodeViewModel? SelectedAction { get; set; }

    [ObservableProperty]
    public partial string CurrentXmlText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? XmlValidationError { get; set; }

    [ObservableProperty]
    public partial string FilterText { get; set; } = string.Empty;

    public bool HasSelection => SelectedAction is not null;

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

    /// <summary>Fires when only the selection highlight changed but the XML content itself did not.</summary>
    public event EventHandler? SelectionDecorationChanged;

    public ActionListViewModel(EditorViewModelFactory factory)
    {
        _factory = factory;
    }

    public void LoadActions(IEnumerable<ActionNodeModel> models)
    {
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
    }

    public List<ActionNodeModel> CollectModels()
    {
        FlushAll();
        return ActionTree.Select(vm => BuildModel(vm)).ToList();
    }

    // ── Cursor-driven selection ───────────────────────────────────────────────

    /// <summary>Called when the Monaco cursor moves to a new line. Updates SelectedAction without re-pushing XML.</summary>
    public void SelectActionAtLine(int line)
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
            _xmlDirtyForGuided = true;
            ComputeLineRangesFromXml(xml);
            SelectionDecorationChanged?.Invoke(this, EventArgs.Empty);
        }
        _updatingFromXml = false;
    }

    /// <summary>Called when the guided panel gains focus. Only refreshes the editor VM when
    /// the XML was actually edited since the last sync — no-op otherwise.</summary>
    public void SyncXmlToGuided()
    {
        if (!_xmlDirtyForGuided) return;
        _xmlDirtyForGuided = false;
        SelectedAction?.RefreshEditorViewModel();
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
            var pairs = ExtractNodePairs(root);

            if (pairs.Count == ActionTree.Count)
            {
                // Count unchanged: update each node in-place (preserves VMs and scroll position).
                for (int i = 0; i < pairs.Count; i++)
                {
                    ApplyParsedNode(ActionTree[i].Model.Node, pairs[i].El);
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

    private void RebuildTreeFromPairs(List<(string? Comment, XElement El)> pairs)
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
            var model = BuildModelFromElement(el);
            model.Comment = comment;
            var vm = new ActionNodeViewModel(model, _factory);
            vm.Dirtied += (_, _) => RaiseDirty();
            vm.ApplyFilter(FilterText);
            ActionTree.Add(vm);
        }

        if (ActionTree.Count > 0 && selectedIdx >= 0)
            SelectedAction = ActionTree[Math.Min(selectedIdx, ActionTree.Count - 1)];
    }

    private static ActionNodeModel BuildModelFromElement(XElement el)
    {
        var model = new ActionNodeModel { Node = el };
        if (model.IsGroup)
        {
            foreach (var (comment, child) in ExtractNodePairs(el))
            {
                var childModel = BuildModelFromElement(child);
                childModel.Comment = comment;
                model.Children.Add(childModel);
            }
        }
        return model;
    }

    private static void ApplyParsedNode(XElement target, XElement parsed)
    {
        target.Name = parsed.Name;
        target.RemoveAll();
        foreach (var attr in parsed.Attributes())
            target.Add(new XAttribute(attr));
        foreach (var child in parsed.Nodes())
            target.Add(CloneXNode(child));
    }

    // ── Full document XML builder ─────────────────────────────────────────────

    private (string xml, int startLine, int endLine) BuildFullActionsXml()
    {
        _lineRanges.Clear();
        var sb = new StringBuilder();
        sb.AppendLine("<Actions>");
        int line = 2;
        int selStart = -1, selEnd = -1;

        AppendVmLines(ActionTree, sb, "  ", ref line, ref selStart, ref selEnd);

        sb.Append("</Actions>");
        return (sb.ToString(), selStart, selEnd);
    }

    private void AppendVmLines(
        IEnumerable<ActionNodeViewModel> nodes,
        StringBuilder sb,
        string indent,
        ref int line,
        ref int selStart,
        ref int selEnd)
    {
        foreach (var vm in nodes)
        {
            if (!string.IsNullOrEmpty(vm.Model.Comment))
            {
                var commentXml = "<!--" + vm.Model.Comment + "-->";
                sb.Append(indent);
                sb.AppendLine(commentXml);
                line += commentXml.Count(c => c == '\n') + 1;
            }

            var raw = vm.Model.Node.ToString();
            var rawLines = raw.Split('\n');
            int vmStart = line;

            if (vm == SelectedAction) selStart = line;

            foreach (var rawLine in rawLines)
            {
                sb.Append(indent);
                sb.AppendLine(rawLine.TrimEnd('\r'));
                line++;
            }

            int vmEnd = line - 1;
            _lineRanges.Add((vm, vmStart, vmEnd));

            if (vm == SelectedAction) selEnd = vmEnd;
        }
    }

    // Computes _lineRanges and SelectedLineRange from the user's raw XML text using
    // IXmlLineInfo so the decoration tracks actual cursor position without reformatting.
    private void ComputeLineRangesFromXml(string xml)
    {
        _lineRanges.Clear();
        SelectedLineRange = (-1, -1);
        try
        {
            var root = XElement.Parse(xml, LoadOptions.SetLineInfo);
            var children = root.Elements().ToList();
            int totalLines = xml.Split('\n').Length;

            for (int i = 0; i < Math.Min(children.Count, ActionTree.Count); i++)
            {
                var el = children[i];
                var vm = ActionTree[i];
                var li = (IXmlLineInfo)el;
                int startLine = li.HasLineInfo() ? li.LineNumber : 1;

                int endLine;
                if (i + 1 < children.Count)
                {
                    var nextLi = (IXmlLineInfo)children[i + 1];
                    endLine = nextLi.HasLineInfo() ? nextLi.LineNumber - 1 : startLine;
                }
                else
                {
                    endLine = Math.Max(startLine, totalLines - 1);
                }

                _lineRanges.Add((vm, startLine, endLine));
                if (vm == SelectedAction)
                    SelectedLineRange = (startLine, endLine);
            }
        }
        catch { }
    }

    private static XNode CloneXNode(XNode node) => node switch
    {
        XElement el               => new XElement(el),
        XCData cd                 => new XCData(cd.Value),
        XText txt                 => new XText(txt.Value),
        XComment c                => new XComment(c.Value),
        XProcessingInstruction pi => new XProcessingInstruction(pi.Target, pi.Data),
        _                         => new XText(node.ToString())
    };

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
        var model = BuildModelFromElement(copy);
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
        RefreshXmlFromNode();

        if (!HasPendingRename && SelectedAction is not null)
        {
            string? currentVar = GetDeclaredVariable(SelectedAction);
            if (currentVar != _trackedVarName
                && !string.IsNullOrEmpty(_trackedVarName)
                && !string.IsNullOrEmpty(currentVar))
            {
                int count = CountVariableReferences(_trackedVarName!);
                if (count > 0)
                    SetPendingRename(_trackedVarName!, currentVar!, count);
            }
            _trackedVarName = currentVar;
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

    [RelayCommand]
    private void AcceptRename()
    {
        if (string.IsNullOrEmpty(_pendingRenameFrom) || string.IsNullOrEmpty(_pendingRenameTo)) return;
        string oldTag = $"%{_pendingRenameFrom}%";
        string newTag = $"%{_pendingRenameTo}%";
        OnXmlEdited(CurrentXmlText.Replace(oldTag, newTag, StringComparison.OrdinalIgnoreCase));
        DismissPendingRename();
    }

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
                usages.Add(new(vm.DisplayLabel, actionIndex, FriendlyAttributeName(attr.Name.LocalName)));
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
                        usages.Add(new(vm.DisplayLabel, actionIndex,
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

    private static ObservableCollection<ActionNodeViewModel>? FindOwningList(
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

    private static List<(string? Comment, XElement El)> ExtractNodePairs(XElement root)
    {
        var result = new List<(string? Comment, XElement El)>();
        string? pendingComment = null;
        foreach (var node in root.Nodes())
        {
            if (node is XComment comment)
                pendingComment = pendingComment is null ? comment.Value : pendingComment + "\n" + comment.Value;
            else if (node is XElement el)
            {
                result.Add((pendingComment, el));
                pendingComment = null;
            }
            // Whitespace XText nodes between elements are silently skipped
        }
        return result;
    }

    private static void AttachLeadingComment(ActionNodeModel model)
    {
        var comments = new List<string>();
        var node = model.Node.PreviousNode;
        while (node is not null)
        {
            if (node is XComment comment)
            {
                comments.Insert(0, comment.Value);
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

public sealed class VariableEntry
{
    public VariableEntry() { }

    public VariableEntry(string name, string sourceType, int index)
        => (Name, SourceType, Index) = (name, sourceType, index);

    public string Name       { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public int    Index      { get; set; }
    public int    RefCount   { get; set; }
    public List<VariableUsage> Usages { get; set; } = [];
    public string NameLabel  => $"%{Name}%";
    public string IndexLabel => $"#{Index}";
    public string RefLabel   => RefCount == 0 ? "unused" : RefCount == 1 ? "1 ref" : $"{RefCount} refs";
    public bool   IsUnused   => RefCount == 0;
    public bool   HasRefs    => RefCount > 0;
}

public sealed class VariableUsage
{
    public VariableUsage() { }
    public VariableUsage(string actionLabel, int actionIndex, string field)
        => (ActionLabel, ActionIndex, Field) = (actionLabel, actionIndex, field);
    public string ActionLabel { get; set; } = string.Empty;
    public int    ActionIndex { get; set; }
    public string Field       { get; set; } = string.Empty;
    public string IndexLabel  => $"#{ActionIndex}";
}
