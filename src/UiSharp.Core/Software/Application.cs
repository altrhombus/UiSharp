namespace UiSharp.Core.Software;

public sealed class Application(
    string id,
    string label,
    string info,
    string appName,
    string includeIds,
    string excludeIds,
    int    orderIndex) : ISoftware
{
    public string Id         { get; } = id;
    public string Type       { get; } = "Application";
    public string Label      { get; } = label;
    public string Info       { get; } = info;
    public string AppName    { get; } = appName;
    public string IncludeIds { get; } = includeIds;
    public string ExcludeIds { get; } = excludeIds;
    public int    OrderIndex { get; } = orderIndex;

    // Returns the CM application name — set as the TS variable value by AppTree.
    public string GetVariableValue() => AppName;
}
