using System.Xml.Linq;
using UIpp.Core.Dialogs;
using UIpp.Core.Ldap;
using UIpp.Core.Logging;
using UIpp.Core.Scripting;
using UIpp.Core.Software;
using UIpp.Core.Variables;

namespace UIpp.Core.Actions;

public sealed class ActionData
{
    public required XElement ActionNode              { get; set; }
    public required IConditionEvaluator Conditions  { get; init; }
    public required ITSEnv TsEnv                    { get; init; }
    public required ICMLog Log                      { get; init; }
    public required DialogTraits GlobalDialogTraits { get; init; }
    public bool InTS                                { get; init; }
    public bool InWinPE                             { get; init; }
    public IReadOnlyDictionary<string, ISoftware>? Software { get; init; }
    public ILdap? Ldap                              { get; init; }
    public XElement? Messages                       { get; init; }
}
