namespace UIpp.Core.Software;

public sealed class Package(
    string id,
    string label,
    string info,
    string pkgId,
    string programName,
    string includeIds,
    string excludeIds,
    int    orderIndex) : ISoftware
{
    public string Id          { get; } = id;
    public string Type        { get; } = "Package";
    public string Label       { get; } = label;
    public string Info        { get; } = info;
    public string PkgId       { get; } = pkgId;
    public string ProgramName { get; } = programName;
    public string IncludeIds  { get; } = includeIds;
    public string ExcludeIds  { get; } = excludeIds;
    public int    OrderIndex  { get; } = orderIndex;

    // Returns the CM package ID — set as the TS variable value by AppTree.
    public string GetVariableValue() => PkgId;
}
