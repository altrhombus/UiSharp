namespace UIpp.Core.Dialogs;

public sealed record PreflightCheckSpec
{
    public required string Text            { get; init; }
    public string Description              { get; init; } = string.Empty;
    public string ErrorDescription         { get; init; } = string.Empty;
    public string WarnDescription          { get; init; } = string.Empty;
    public required string CheckCondition  { get; init; }
    public string WarnCondition            { get; init; } = string.Empty;
}

public enum PreflightStatus { Pass, Warn, Fail }

public sealed record PreflightResult(PreflightCheckSpec Check, PreflightStatus Status)
{
    public string ActiveDescription => Status switch
    {
        PreflightStatus.Fail => string.IsNullOrEmpty(Check.ErrorDescription)
            ? Check.Description : Check.ErrorDescription,
        PreflightStatus.Warn => string.IsNullOrEmpty(Check.WarnDescription)
            ? Check.Description : Check.WarnDescription,
        _ => Check.Description,
    };
}
