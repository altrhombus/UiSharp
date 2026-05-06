using System.Xml.Linq;
using UIpp.Core.Configuration;
using UIpp.Core.Scripting;
using UIpp.Core.Variables;

namespace UIpp.Core.Dialogs;

public static class InputFieldParser
{
    private static readonly IReadOnlyDictionary<string, string> EmptyVars =
        new Dictionary<string, string>();

    // Parses input field child elements of an <Action Type="Input"> node.
    // Elements whose Condition evaluates false are excluded.
    // InputChoice elements with no choices (after condition filtering) are excluded.
    public static IReadOnlyList<InputFieldSpec> Parse(
        XElement actionNode, ITSEnv env, IConditionEvaluator conditions)
    {
        var result = new List<InputFieldSpec>();

        foreach (var el in actionNode.Elements())
        {
            var name = el.Name.LocalName;

            if (!IsKnownInputElement(name)) continue;

            var condition = (string?)el.Attribute(XmlConstants.Attributes.Condition) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(condition) &&
                !conditions.Evaluate(env.Substitute(condition), EmptyVars))
                continue;

            var question = env.Substitute(
                (string?)el.Attribute(XmlConstants.Attributes.Question) ?? XmlConstants.Defaults.Question);

            InputFieldSpec? spec = null;

            if (IsText(name))
                spec = ParseText(el, env, question);
            else if (IsChoice(name))
                spec = ParseChoice(el, env, conditions, question);
            else if (IsCheckbox(name))
                spec = ParseCheckbox(el, env, question);
            else if (name.Equals(XmlConstants.InputTypes.Info, StringComparison.OrdinalIgnoreCase))
                spec = ParseInfo(el, env);

            if (spec is not null)
                result.Add(spec);
        }

        return result;
    }

    // -------------------------------------------------------------------------
    // Type helpers

    private static bool IsKnownInputElement(string name) =>
        IsText(name) || IsChoice(name) || IsCheckbox(name) ||
        name.Equals(XmlConstants.InputTypes.Info, StringComparison.OrdinalIgnoreCase);

    private static bool IsText(string name) =>
        name.Equals(XmlConstants.InputTypes.Text,    StringComparison.OrdinalIgnoreCase) ||
        name.Equals(XmlConstants.InputTypes.TextOld, StringComparison.OrdinalIgnoreCase);

    private static bool IsChoice(string name) =>
        name.Equals(XmlConstants.InputTypes.Choice,    StringComparison.OrdinalIgnoreCase) ||
        name.Equals(XmlConstants.InputTypes.ChoiceOld, StringComparison.OrdinalIgnoreCase);

    private static bool IsCheckbox(string name) =>
        name.Equals(XmlConstants.InputTypes.Checkbox,    StringComparison.OrdinalIgnoreCase) ||
        name.Equals(XmlConstants.InputTypes.CheckboxOld, StringComparison.OrdinalIgnoreCase);

    // -------------------------------------------------------------------------
    // Attribute helpers

    private static string Attr(XElement el, string name, string def = "") =>
        (string?)el.Attribute(name) ?? def;

    private static bool BoolAttr(XElement el, string name, bool def = false) =>
        Attr(el, name, def ? "True" : "False").Equals("True", StringComparison.OrdinalIgnoreCase);

    private static string ResolveDefault(XElement el, string variable, ITSEnv env)
    {
        var fromEnv = env.Get(variable);
        return string.IsNullOrEmpty(fromEnv)
            ? env.Substitute(Attr(el, XmlConstants.Attributes.Default))
            : fromEnv;
    }

    // -------------------------------------------------------------------------
    // Per-type parsers

    private static InputTextSpec ParseText(XElement el, ITSEnv env, string question)
    {
        var variable = env.Substitute(Attr(el, XmlConstants.Attributes.Variable, XmlConstants.Defaults.Variable));
        return new InputTextSpec
        {
            Question         = question,
            Variable         = variable,
            DefaultValue     = ResolveDefault(el, variable, env),
            Hint             = env.Substitute(Attr(el, XmlConstants.Attributes.Hint)),
            Prompt           = env.Substitute(Attr(el, XmlConstants.Attributes.Prompt)),
            Regex            = Attr(el, XmlConstants.Attributes.RegEx),
            Required         = BoolAttr(el, XmlConstants.Attributes.Required, true),
            Password         = BoolAttr(el, XmlConstants.Attributes.Password),
            HorizontalScroll = BoolAttr(el, XmlConstants.Attributes.HScroll),
            ForceCase        = Attr(el, XmlConstants.Attributes.ForceCase),
            ReadOnly         = BoolAttr(el, XmlConstants.Attributes.ReadOnly),
            AdValidate       = Attr(el, XmlConstants.Attributes.AdValidate),
        };
    }

    private static InputChoiceSpec? ParseChoice(
        XElement el, ITSEnv env, IConditionEvaluator conditions, string question)
    {
        var choices = BuildChoices(el, env, conditions);
        if (choices.Count == 0) return null;

        var variable = env.Substitute(Attr(el, XmlConstants.Attributes.Variable, XmlConstants.Defaults.Variable));
        return new InputChoiceSpec
        {
            Question     = question,
            Variable     = variable,
            DefaultValue = ResolveDefault(el, variable, env),
            Choices      = choices,
            AltVariable  = env.Substitute(Attr(el, XmlConstants.Attributes.AlternateVariable)),
            Required     = BoolAttr(el, XmlConstants.Attributes.Required),
            AutoComplete = BoolAttr(el, XmlConstants.Attributes.AutoComplete),
            Sort         = BoolAttr(el, XmlConstants.Attributes.Sort, true),
            DropDownSize = int.TryParse(Attr(el, XmlConstants.Attributes.DropDownSize, "5"), out var sz) ? sz : 5,
        };
    }

    private static List<ChoiceOption> BuildChoices(
        XElement parent, ITSEnv env, IConditionEvaluator conditions)
    {
        var choices = new List<ChoiceOption>();

        // Individual <Choice> elements
        foreach (var choiceEl in parent.Elements(XmlConstants.Elements.Choice))
        {
            var cond = (string?)choiceEl.Attribute(XmlConstants.Attributes.Condition) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(cond) && !conditions.Evaluate(env.Substitute(cond), EmptyVars))
                continue;

            var option = Attr(choiceEl, XmlConstants.Attributes.Option);
            var value  = Attr(choiceEl, XmlConstants.Attributes.Value, option);
            var alt    = Attr(choiceEl, XmlConstants.Attributes.AlternateValue);
            choices.Add(new ChoiceOption(option, value, alt));
        }

        // <ChoiceList> elements (comma/semicolon delimited strings)
        foreach (var listEl in parent.Elements(XmlConstants.Elements.ChoiceList))
        {
            var cond = (string?)listEl.Attribute(XmlConstants.Attributes.Condition) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(cond) && !conditions.Evaluate(env.Substitute(cond), EmptyVars))
                continue;

            var optionList = env.Substitute(Attr(listEl, XmlConstants.Attributes.OptionList));
            var valueList  = env.Substitute(Attr(listEl, XmlConstants.Attributes.ValueList, optionList));
            var altList    = env.Substitute(Attr(listEl, XmlConstants.Attributes.AlternateValueList));

            var options = SplitList(optionList);
            var values  = SplitList(valueList);
            var alts    = SplitList(altList);

            for (int i = 0; i < options.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(options[i])) continue;
                choices.Add(new ChoiceOption(
                    options[i],
                    i < values.Count ? values[i] : string.Empty,
                    i < alts.Count   ? alts[i]   : string.Empty));
            }
        }

        return choices;
    }

    private static InputCheckboxSpec ParseCheckbox(XElement el, ITSEnv env, string question)
    {
        var variable = env.Substitute(Attr(el, XmlConstants.Attributes.Variable, XmlConstants.Defaults.Variable));
        return new InputCheckboxSpec
        {
            Question       = question,
            Variable       = variable,
            DefaultValue   = ResolveDefault(el, variable, env),
            CheckedValue   = Attr(el, XmlConstants.Attributes.CheckedValue,   "True"),
            UncheckedValue = Attr(el, XmlConstants.Attributes.UncheckedValue, "False"),
        };
    }

    private static InputInfoSpec ParseInfo(XElement el, ITSEnv env)
    {
        var text = env.Substitute(el.Value.Trim())
            .Replace("\\t", "\t")
            .Replace("\\r", "\r")
            .Replace("\\n", "\n");

        return new InputInfoSpec
        {
            Question      = text,
            TextColor     = Attr(el, XmlConstants.Attributes.Color),
            NumberOfLines = int.TryParse(Attr(el, XmlConstants.Attributes.NumberOfLines, "1"), out var n) ? n : 1,
        };
    }

    private static List<string> SplitList(string input) =>
        [.. input.Split([',', ';'], StringSplitOptions.TrimEntries)];
}
