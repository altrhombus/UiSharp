using System.Xml.Linq;
using UiSharp.Core.Dialogs;
using UiSharp.Core.Ldap;
using UiSharp.Core.Logging;
using UiSharp.Core.Scripting;
using UiSharp.Core.Software;
using UiSharp.Core.Variables;

namespace UiSharp.Core.Actions;

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
    public IDefaultValueProvider? DefaultValueProvider { get; init; }
}
