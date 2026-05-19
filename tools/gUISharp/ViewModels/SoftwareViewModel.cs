using System.Collections.ObjectModel;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UIpp.Core.Configuration;
using UIpp.Core.Software;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels;

public sealed partial class SoftwareViewModel : ObservableObject, IXmlEditorSource
{
    private bool _updatingFromXml;
    private readonly List<(SoftwareItemViewModel Item, int Start, int End)> _lineRanges = [];

    public ObservableCollection<SoftwareItemViewModel> Items { get; } = [];

    public bool HasItems => Items.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    public partial SoftwareItemViewModel? SelectedItem { get; set; }

    public bool HasSelection => SelectedItem is not null;

    // ── IXmlEditorSource ─────────────────────────────────────────────────────

    [ObservableProperty]
    public partial string CurrentXmlText { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string? XmlValidationError { get; private set; }

    public (int Start, int End) SelectedLineRange { get; private set; } = (-1, -1);

    public event EventHandler? SelectionDecorationChanged;

    public event EventHandler? Dirtied;

    public void OnXmlEdited(string xml)
    {
        _updatingFromXml = true;
        CurrentXmlText = xml;
        if (TryApplyXmlToItems(xml))
        {
            ComputeLineRangesFromXml(xml);
            SelectionDecorationChanged?.Invoke(this, EventArgs.Empty);
        }
        _updatingFromXml = false;
        Dirtied?.Invoke(this, EventArgs.Empty);
    }

    public void SelectAtLine(int line)
    {
        foreach (var (item, start, end) in _lineRanges)
        {
            if (line >= start && line <= end)
            {
                if (SelectedItem != item) SelectedItem = item;
                return;
            }
        }
    }

    public void SyncXmlToGuided() { }

    public void SyncGuidedToXml() => RefreshXmlFromItems();

    // ── Construction ─────────────────────────────────────────────────────────

    public SoftwareViewModel()
    {
        Items.CollectionChanged += (_, e) =>
        {
            if (e.OldItems is not null)
                foreach (SoftwareItemViewModel vm in e.OldItems)
                    vm.PropertyChanged -= OnItemPropertyChanged;
            if (e.NewItems is not null)
                foreach (SoftwareItemViewModel vm in e.NewItems)
                    vm.PropertyChanged += OnItemPropertyChanged;
            OnPropertyChanged(nameof(HasItems));
            if (!_updatingFromXml)
            {
                RefreshXmlFromItems();
                Dirtied?.Invoke(this, EventArgs.Empty);
            }
        };
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_updatingFromXml) return;
        if (e.PropertyName == nameof(SoftwareItemViewModel.IsDirty)) return;
        if (sender is SoftwareItemViewModel item) item.IsDirty = true;
        RefreshXmlFromItems();
        Dirtied?.Invoke(this, EventArgs.Empty);
    }

    public void MarkAllItemsClean()
    {
        foreach (var item in Items)
            item.IsDirty = false;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void LoadFrom(IEnumerable<ISoftware> software, XElement? softwareElement = null)
    {
        _updatingFromXml = true;
        Items.Clear();
        foreach (var sw in software)
            Items.Add(SoftwareItemViewModel.FromSoftware(sw));
        _updatingFromXml = false;

        if (softwareElement is not null)
            AttachLeadingComments(softwareElement);

        RefreshXmlFromItems();
    }

    public List<ISoftware> CollectSoftware()
    {
        var result = new List<ISoftware>();
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].OrderIndex = i;
            result.Add(Items[i].ToSoftware());
        }
        return result;
    }

    public IReadOnlyDictionary<string, string?> GetSoftwareComments() =>
        Items.Where(i => !string.IsNullOrEmpty(i.Comment))
             .ToDictionary(i => i.Id, i => i.Comment);

    // ── Commands ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddApplication()
    {
        var vm = new SoftwareItemViewModel
        {
            IsApplication = true,
            Id            = Guid.NewGuid().ToString("D").ToUpper(),
            Label         = "New Application",
            OrderIndex    = Items.Count,
        };
        Items.Add(vm);
        SelectedItem = vm;
    }

    [RelayCommand]
    private void AddPackage()
    {
        var vm = new SoftwareItemViewModel
        {
            IsApplication = false,
            Id            = Guid.NewGuid().ToString("D").ToUpper(),
            Label         = "New Package",
            OrderIndex    = Items.Count,
        };
        Items.Add(vm);
        SelectedItem = vm;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RemoveItem()
    {
        if (SelectedItem is null) return;
        Items.Remove(SelectedItem);
        SelectedItem = null;
    }

    partial void OnSelectedItemChanged(SoftwareItemViewModel? value)
    {
        RemoveItemCommand.NotifyCanExecuteChanged();
        if (!_updatingFromXml)
        {
            var (xml, start, end) = BuildSoftwareXml();
            SelectedLineRange = (start, end);
            if (xml == CurrentXmlText)
                SelectionDecorationChanged?.Invoke(this, EventArgs.Empty);
            else
                CurrentXmlText = xml;
        }
    }

    public void ImportItems(IEnumerable<CmSelectableItem> items)
    {
        foreach (var item in items)
        {
            var vm = item.IsApp
                ? new SoftwareItemViewModel
                  {
                      IsApplication = true,
                      Id            = Guid.NewGuid().ToString("D").ToUpper(),
                      Label         = item.Name,
                      AppName       = item.Name,
                      OrderIndex    = Items.Count,
                  }
                : new SoftwareItemViewModel
                  {
                      IsApplication = false,
                      Id            = Guid.NewGuid().ToString("D").ToUpper(),
                      Label         = item.Name,
                      PkgId         = item.PkgId,
                      OrderIndex    = Items.Count,
                  };
            Items.Add(vm);
        }
    }

    // ── XML sync helpers ──────────────────────────────────────────────────────

    private void RefreshXmlFromItems()
    {
        if (_updatingFromXml) return;
        var (xml, start, end) = BuildSoftwareXml();
        SelectedLineRange = (start, end);
        if (xml == CurrentXmlText)
            SelectionDecorationChanged?.Invoke(this, EventArgs.Empty);
        else
            CurrentXmlText = xml;
    }

    private (string xml, int startLine, int endLine) BuildSoftwareXml()
    {
        _lineRanges.Clear();
        var sb = new StringBuilder();
        sb.AppendLine("<Software>");
        int line = 2;
        int selStart = -1, selEnd = -1;

        foreach (var item in Items)
        {
            int itemStart = line;

            if (!string.IsNullOrWhiteSpace(item.Comment))
            {
                if (item.Comment.Contains('\n'))
                {
                    sb.Append("  "); sb.AppendLine("<!--"); line++;
                    foreach (var commentLine in item.Comment.Split('\n'))
                    {
                        sb.Append("  "); sb.Append("  "); sb.AppendLine(commentLine); line++;
                    }
                    sb.Append("  "); sb.AppendLine("-->"); line++;
                }
                else
                {
                    sb.Append("  "); sb.AppendLine($"<!-- {item.Comment.Trim()} -->"); line++;
                }
            }

            var raw = item.ToXElement().ToString();
            foreach (var rawLine in raw.Split('\n'))
            {
                sb.Append("  ");
                sb.AppendLine(rawLine.TrimEnd('\r'));
                line++;
            }

            int itemEnd = line - 1;
            _lineRanges.Add((item, itemStart, itemEnd));
            if (item == SelectedItem) { selStart = itemStart; selEnd = itemEnd; }
        }

        sb.Append("</Software>");
        return (sb.ToString(), selStart, selEnd);
    }

    private bool TryApplyXmlToItems(string xml)
    {
        try
        {
            var root = XElement.Parse(xml);
            var pairs = ExtractNodePairs(root);

            if (pairs.Count == Items.Count)
            {
                for (int i = 0; i < pairs.Count; i++)
                {
                    ApplyXmlToItem(Items[i], pairs[i].El);
                    Items[i].Comment = pairs[i].Comment;
                }
            }
            else
            {
                RebuildItemsFromXml(pairs);
            }

            XmlValidationError = null;
            return true;
        }
        catch (Exception ex)
        {
            XmlValidationError = ex.Message;
            return false;
        }
    }

    private void RebuildItemsFromXml(List<(string? Comment, XElement El)> pairs)
    {
        int selectedIdx = SelectedItem is not null ? Items.IndexOf(SelectedItem) : -1;
        SelectedItem = null;
        Items.Clear();

        for (int i = 0; i < pairs.Count; i++)
        {
            var vm = new SoftwareItemViewModel { OrderIndex = i };
            ApplyXmlToItem(vm, pairs[i].El);
            vm.Comment = pairs[i].Comment;
            Items.Add(vm);
        }

        if (Items.Count > 0 && selectedIdx >= 0)
            SelectedItem = Items[Math.Min(selectedIdx, Items.Count - 1)];
    }

    private static void ApplyXmlToItem(SoftwareItemViewModel vm, XElement el)
    {
        vm.IsApplication = el.Name.LocalName == C.Elements.Application;
        vm.Id          = (string?)el.Attribute(C.Attributes.Id)           ?? string.Empty;
        vm.Label       = (string?)el.Attribute(C.Attributes.Label)        ?? string.Empty;
        vm.Info        = (string?)el.Attribute(C.Attributes.SoftwareInfo) ?? string.Empty;
        vm.IncludeIds  = (string?)el.Attribute(C.Attributes.IncludeId)    ?? string.Empty;
        vm.ExcludeIds  = (string?)el.Attribute(C.Attributes.ExcludeId)    ?? string.Empty;
        if (vm.IsApplication)
            vm.AppName = (string?)el.Attribute(C.Attributes.AppName) ?? string.Empty;
        else
        {
            vm.PkgId       = (string?)el.Attribute(C.Attributes.PkgId)       ?? string.Empty;
            vm.ProgramName = (string?)el.Attribute(C.Attributes.ProgramName) ?? string.Empty;
        }
    }

    private void ComputeLineRangesFromXml(string xml)
    {
        _lineRanges.Clear();
        SelectedLineRange = (-1, -1);
        try
        {
            var root = XElement.Parse(xml, LoadOptions.SetLineInfo);
            var children = root.Elements().ToList();
            for (int i = 0; i < Math.Min(children.Count, Items.Count); i++)
            {
                var el = children[i];
                var item = Items[i];
                var li = (IXmlLineInfo)el;
                int startLine = li.HasLineInfo() ? li.LineNumber : 1;
                int elementLineCount = el.ToString().Split('\n').Length;
                int endLine = startLine + elementLineCount - 1;
                _lineRanges.Add((item, startLine, endLine));
                if (item == SelectedItem)
                    SelectedLineRange = (startLine, endLine);
            }
        }
        catch { }
    }

    private void AttachLeadingComments(XElement softwareElement)
    {
        // Build Id → comment map by scanning the element's node sequence.
        var commentMap = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        string? pendingComment = null;
        foreach (var node in softwareElement.Nodes())
        {
            if (node is XComment comment)
            {
                var normalized = NormalizeComment(comment.Value);
                pendingComment = pendingComment is null ? normalized : pendingComment + "\n" + normalized;
            }
            else if (node is XElement el)
            {
                if (pendingComment is not null)
                {
                    var id = (string?)el.Attribute(C.Attributes.Id);
                    if (!string.IsNullOrEmpty(id))
                        commentMap[id] = pendingComment;
                }
                pendingComment = null;
            }
        }
        foreach (var item in Items)
        {
            if (commentMap.TryGetValue(item.Id, out var c))
                item.Comment = c;
        }
    }

    private static List<(string? Comment, XElement El)> ExtractNodePairs(XElement root)
    {
        var result = new List<(string? Comment, XElement El)>();
        string? pendingComment = null;
        foreach (var node in root.Nodes())
        {
            if (node is XComment comment)
            {
                var normalized = NormalizeComment(comment.Value);
                pendingComment = pendingComment is null ? normalized : pendingComment + "\n" + normalized;
            }
            else if (node is XElement el)
            {
                result.Add((pendingComment, el));
                pendingComment = null;
            }
        }
        return result;
    }

    private static string NormalizeComment(string rawValue)
    {
        var lines = rawValue
            .Split('\n')
            .Select(l => l.Trim())
            .SkipWhile(string.IsNullOrEmpty)
            .ToList();
        while (lines.Count > 0 && string.IsNullOrEmpty(lines[^1]))
            lines.RemoveAt(lines.Count - 1);
        return string.Join("\n", lines);
    }
}
