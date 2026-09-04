using UiSharp.Core.Configuration;

namespace UiSharp.Core.Dialogs;

public sealed record ValidationResult(bool IsValid, string ErrorMessage = "")
{
    public static readonly ValidationResult Ok = new(true);
    public static ValidationResult Fail(string message) => new(false, message);
}

public sealed record ChoiceOption(string Option, string Value, string AltValue);

public abstract class InputFieldSpec
{
    public required string Question { get; init; }
}

public sealed class InputTextSpec : InputFieldSpec
{
    public required string Variable  { get; init; }
    public string DefaultValue       { get; init; } = string.Empty;
    public string Hint               { get; init; } = string.Empty;
    public string Prompt             { get; init; } = string.Empty;
    public string Regex              { get; init; } = string.Empty;
    public bool   Required           { get; init; } = true;
    public bool   Password           { get; init; }
    public bool   HorizontalScroll   { get; init; }
    public string ForceCase          { get; init; } = string.Empty;
    public bool   ReadOnly           { get; init; }
    public string AdValidate         { get; init; } = string.Empty;

    public ValidationResult Validate(string value)
    {
        if (Required && string.IsNullOrWhiteSpace(value))
            return ValidationResult.Fail("This field is required.");

        if (!string.IsNullOrEmpty(Regex) && !string.IsNullOrEmpty(value) &&
            !System.Text.RegularExpressions.Regex.IsMatch(value, Regex))
            return ValidationResult.Fail("Value does not match the required format.");

        return ValidationResult.Ok;
    }

    public string ApplyForceCase(string value) => ForceCase switch
    {
        XmlConstants.Values.Upper => value.ToUpperInvariant(),
        XmlConstants.Values.Lower => value.ToLowerInvariant(),
        _                         => value,
    };
}

public sealed class InputChoiceSpec : InputFieldSpec
{
    public required string Variable                  { get; init; }
    public string DefaultValue                       { get; init; } = string.Empty;
    public required IReadOnlyList<ChoiceOption> Choices { get; init; }
    public string AltVariable                        { get; init; } = string.Empty;
    public bool   Required                           { get; init; }
    public bool   AutoComplete                       { get; init; }
    public bool   Sort                               { get; init; } = true;
    public int    DropDownSize                       { get; init; } = 5;

    public ValidationResult Validate(string value)
    {
        if (Required && string.IsNullOrWhiteSpace(value))
            return ValidationResult.Fail("A selection is required.");
        return ValidationResult.Ok;
    }
}

public sealed class InputCheckboxSpec : InputFieldSpec
{
    public required string Variable  { get; init; }
    public string DefaultValue       { get; init; } = string.Empty;
    public string CheckedValue       { get; init; } = "True";
    public string UncheckedValue     { get; init; } = "False";
}

public sealed class InputInfoSpec : InputFieldSpec
{
    // Question holds the substituted display text; no Variable.
    public string TextColor    { get; init; } = string.Empty;
    public int    NumberOfLines { get; init; } = 1;
}
