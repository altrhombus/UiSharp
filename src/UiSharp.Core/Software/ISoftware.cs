namespace UiSharp.Core.Software;

public interface ISoftware
{
    string Id           { get; }
    string Type         { get; }
    string Label        { get; }
    string Info         { get; }
    string IncludeIds   { get; }
    string ExcludeIds   { get; }
    int    OrderIndex   { get; }
    string GetVariableValue();
}
