using System.Xml.Linq;
using UIpp.Core.Configuration;

namespace UIpp.Core.Actions;

public interface IAction
{
    ActionResult Go();
    bool IsGuiAction { get; }
}

public abstract class ActionBase(ActionData data) : IAction
{
    protected readonly ActionData Data = data;

    protected static readonly IReadOnlyDictionary<string, string> NoVars =
        new Dictionary<string, string>();

    public abstract ActionResult Go();
    public virtual bool IsGuiAction => false;

    // Attribute helpers -------------------------------------------------------

    protected static string Attr(XElement el, string name, string? def = null) =>
        (string?)el.Attribute(name) ?? def ?? string.Empty;

    protected string Attr(string name, string? def = null) =>
        Attr(Data.ActionNode, name, def);

    protected string SubstAttr(string name, string? def = null) =>
        Data.TsEnv.Substitute(Attr(name, def));

    protected static bool BoolAttr(XElement el, string name, bool def = false)
    {
        var v = (string?)el.Attribute(name);
        if (string.IsNullOrWhiteSpace(v)) return def;
        return v.Equals("true", StringComparison.OrdinalIgnoreCase)
            || v.Equals("yes",  StringComparison.OrdinalIgnoreCase)
            || v == "1";
    }

    protected bool BoolAttr(string name, bool def = false) =>
        BoolAttr(Data.ActionNode, name, def);

    // Condition helpers -------------------------------------------------------

    protected bool EvalCondition(XElement el) =>
        EvalCondition((string?)el.Attribute(XmlConstants.Attributes.Condition));

    protected bool EvalCondition(string? condition)
    {
        if (string.IsNullOrWhiteSpace(condition)) return true;
        return Data.Conditions.Evaluate(Data.TsEnv.Substitute(condition), NoVars);
    }
}
