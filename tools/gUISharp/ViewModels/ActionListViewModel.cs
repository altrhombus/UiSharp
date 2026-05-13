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

    public ObservableCollection<ActionNodeViewModel> ActionTree { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    public partial ActionNodeViewModel? SelectedAction { get; set; }

    [ObservableProperty]
    public partial string CurrentXmlText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? XmlValidationError { get; set; }

    public bool HasSelection => SelectedAction is not null;

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
            var vm = new ActionNodeViewModel(model, _factory);
            vm.Dirtied += (_, _) => RaiseDirty();
            ActionTree.Add(vm);
        }
        SelectedAction = null;
        RefreshXmlFromNode();
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
            var children = root.Elements().ToList();

            if (children.Count == ActionTree.Count)
            {
                // Count unchanged: update each node in-place (preserves VMs and scroll position).
                for (int i = 0; i < children.Count; i++)
                    ApplyParsedNode(ActionTree[i].Model.Node, children[i]);
            }
            else
            {
                // Structure changed: rebuild the entire tree from the parsed elements.
                RebuildTreeFromElements(children);
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

    private void RebuildTreeFromElements(List<XElement> elements)
    {
        // Remember which index was selected so we can restore the closest match.
        int selectedIdx = SelectedAction is not null ? ActionTree.IndexOf(SelectedAction) : -1;

        // Setting SelectedAction = null triggers OnSelectedActionChanged which unsubscribes
        // the old selection's Dirtied handler. RefreshXmlFromNode is a no-op here because
        // _updatingFromXml is true.
        SelectedAction = null;
        ActionTree.Clear();

        foreach (var el in elements)
        {
            var model = BuildModelFromElement(el);
            var vm = new ActionNodeViewModel(model, _factory);
            vm.Dirtied += (_, _) => RaiseDirty();
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
            foreach (var child in el.Elements())
                model.Children.Add(BuildModelFromElement(child));
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
        ActionTree.Add(vm);
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
        ActionTree.Add(vm);
        RaiseDirty();
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

    partial void OnSelectedActionChanged(ActionNodeViewModel? value)
    {
        if (_previousSelection is not null)
            _previousSelection.Dirtied -= OnSelectedDirtied;

        _previousSelection = value;

        if (value is not null)
            value.Dirtied += OnSelectedDirtied;

        RefreshXmlFromNode();

        RemoveActionCommand.NotifyCanExecuteChanged();
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    private void OnSelectedDirtied(object? sender, EventArgs e)
    {
        if (_updatingFromXml) return;
        if (SelectedAction?.EditorViewModel is IActionEditor editor)
            editor.FlushToNode();
        RefreshXmlFromNode();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void RaiseDirty() => Dirtied?.Invoke(this, EventArgs.Empty);

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
}
