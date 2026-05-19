namespace GUISharp.ViewModels;

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
    public VariableUsage(ActionNodeViewModel node, int actionIndex, string field)
    {
        ActionNode  = node;
        ActionLabel = node.DisplayLabel;
        ActionIndex = actionIndex;
        Field       = field;
    }
    public ActionNodeViewModel? ActionNode  { get; set; }
    public string               ActionLabel { get; set; } = string.Empty;
    public int                  ActionIndex { get; set; }
    public string               Field       { get; set; } = string.Empty;
    public string               IndexLabel  => $"#{ActionIndex}";
    public bool                 CanNavigate => ActionNode is not null;
}
