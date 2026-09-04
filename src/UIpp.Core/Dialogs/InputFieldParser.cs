using System.Xml.Linq;
using UIpp.Core.Configuration;
using UIpp.Core.Logging;
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
        XElement actionNode, ITSEnv env, IConditionEvaluator conditions, ICMLog? log = null)
    {
        var result = new List<InputFieldSpec>();

        foreach (var el in actionNode.Elements())
        {
            var name = el.Name.LocalName;

            if (!IsKnownInputElement(name)) continue;

            var condition = RawAttr(el, XmlConstants.Attributes.Condition);
            if (!string.IsNullOrWhiteSpace(condition) &&
                !conditions.EvaluateLogged(env.Substitute(condition), log, $"input field <{name}> Condition"))
                continue;

            var question = Attr(el, env, XmlConstants.Attributes.Question, XmlConstants.Defaults.Question);

            InputFieldSpec? spec = null;

            if (IsText(name))
                spec = ParseText(el, env, question);
            else if (IsChoice(name))
                spec = ParseChoice(el, env, conditions, question, log);
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

    // Mirrors C++ GetXMLAttribute (UI++/Actions/IAction.cpp:21): the value is
    // variable-substituted, the default is not, and emptiness is judged on the
    // raw value so a present-but-empty attribute falls back to the default.
    private static string Attr(XElement el, ITSEnv env, string name, string def = "")
    {
        var raw = RawAttr(el, name);
        return raw.Length > 0 ? env.Substitute(raw) : def;
    }

    // For values substituted later by their consumer - condition expressions.
    private static string RawAttr(XElement el, string name) =>
        (string?)el.Attribute(name) ?? string.Empty;

    // C++ uses FTW::IsTrue(GetXMLAttribute(...)), so the value is substituted
    // before the truthiness test and a variable-valued Required behaves as written.
    private static bool BoolAttr(XElement el, ITSEnv env, string name, bool def = false)
    {
        var v = Attr(el, env, name, def ? "True" : "False");
        return v.Equals("True", StringComparison.OrdinalIgnoreCase)
            || v.Equals("yes",  StringComparison.OrdinalIgnoreCase)
            || v.Equals("1",    StringComparison.Ordinal);
    }

    private static string ResolveDefault(XElement el, string variable, ITSEnv env)
    {
        var fromEnv = env.Get(variable);
        return string.IsNullOrEmpty(fromEnv)
            ? Attr(el, env, XmlConstants.Attributes.Default)
            : fromEnv;
    }

    // -------------------------------------------------------------------------
    // Per-type parsers

    private static InputTextSpec ParseText(XElement el, ITSEnv env, string question)
    {
        var variable = Attr(el, env, XmlConstants.Attributes.Variable, XmlConstants.Defaults.Variable);
        return new InputTextSpec
        {
            Question         = question,
            Variable         = variable,
            DefaultValue     = ResolveDefault(el, variable, env),
            Hint             = Attr(el, env, XmlConstants.Attributes.Hint),
            Prompt           = Attr(el, env, XmlConstants.Attributes.Prompt),
            Regex            = Attr(el, env, XmlConstants.Attributes.RegEx),
            Required         = BoolAttr(el, env, XmlConstants.Attributes.Required, true),
            Password         = BoolAttr(el, env, XmlConstants.Attributes.Password),
            HorizontalScroll = BoolAttr(el, env, XmlConstants.Attributes.HScroll),
            ForceCase        = Attr(el, env, XmlConstants.Attributes.ForceCase),
            ReadOnly         = BoolAttr(el, env, XmlConstants.Attributes.ReadOnly),
            AdValidate       = Attr(el, env, XmlConstants.Attributes.AdValidate),
        };
    }

    private static InputChoiceSpec? ParseChoice(
        XElement el, ITSEnv env, IConditionEvaluator conditions, string question, ICMLog? log)
    {
        var choices = BuildChoices(el, env, conditions, log);
        if (choices.Count == 0) return null;

        var variable = Attr(el, env, XmlConstants.Attributes.Variable, XmlConstants.Defaults.Variable);
        return new InputChoiceSpec
        {
            Question     = question,
            Variable     = variable,
            DefaultValue = ResolveDefault(el, variable, env),
            Choices      = choices,
            AltVariable  = Attr(el, env, XmlConstants.Attributes.AlternateVariable),
            Required     = BoolAttr(el, env, XmlConstants.Attributes.Required),
            AutoComplete = BoolAttr(el, env, XmlConstants.Attributes.AutoComplete),
            Sort         = BoolAttr(el, env, XmlConstants.Attributes.Sort, true),
            DropDownSize = int.TryParse(Attr(el, env, XmlConstants.Attributes.DropDownSize, "5"), out var sz) ? sz : 5,
        };
    }

    private static List<ChoiceOption> BuildChoices(
        XElement parent, ITSEnv env, IConditionEvaluator conditions, ICMLog? log)
    {
        var choices = new List<ChoiceOption>();

        // Individual <Choice> elements
        foreach (var choiceEl in parent.Elements(XmlConstants.Elements.Choice))
        {
            var cond = RawAttr(choiceEl, XmlConstants.Attributes.Condition);
            if (!string.IsNullOrWhiteSpace(cond) &&
                !conditions.EvaluateLogged(env.Substitute(cond), log, "<Choice> Condition"))
                continue;

            // option is already substituted, so passing it as the default for
            // Value stays correct even though defaults are not substituted.
            var option = Attr(choiceEl, env, XmlConstants.Attributes.Option);
            var value  = Attr(choiceEl, env, XmlConstants.Attributes.Value, option);
            var alt    = Attr(choiceEl, env, XmlConstants.Attributes.AlternateValue);
            choices.Add(new ChoiceOption(option, value, alt));
        }

        // <ChoiceList> elements (comma/semicolon delimited strings)
        foreach (var listEl in parent.Elements(XmlConstants.Elements.ChoiceList))
        {
            var cond = RawAttr(listEl, XmlConstants.Attributes.Condition);
            if (!string.IsNullOrWhiteSpace(cond) &&
                !conditions.EvaluateLogged(env.Substitute(cond), log, "<ChoiceList> Condition"))
                continue;

            var optionList = Attr(listEl, env, XmlConstants.Attributes.OptionList);
            var valueList  = Attr(listEl, env, XmlConstants.Attributes.ValueList, optionList);
            var altList    = Attr(listEl, env, XmlConstants.Attributes.AlternateValueList);

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
        var variable = Attr(el, env, XmlConstants.Attributes.Variable, XmlConstants.Defaults.Variable);
        return new InputCheckboxSpec
        {
            Question       = question,
            Variable       = variable,
            DefaultValue   = ResolveDefault(el, variable, env),
            CheckedValue   = Attr(el, env, XmlConstants.Attributes.CheckedValue,   "True"),
            UncheckedValue = Attr(el, env, XmlConstants.Attributes.UncheckedValue, "False"),
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
            TextColor     = Attr(el, env, XmlConstants.Attributes.Color),
            NumberOfLines = int.TryParse(Attr(el, env, XmlConstants.Attributes.NumberOfLines, "1"), out var n) ? n : 1,
        };
    }

    private static List<string> SplitList(string input) =>
        [.. input.Split([',', ';'], StringSplitOptions.TrimEntries)];
}
