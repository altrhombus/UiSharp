using System.Xml.Linq;
using UiSharp.Core.Configuration;
using UiSharp.Core.Scripting;

namespace UiSharp.Core.Actions;

// Walks the <Actions> element tree depth-first, evaluating conditions and running each action.
// Mirrors the CUiSharpApp::Process() while-loop from the C++ original.
public sealed class ActionProcessor(
    ActionFactory        factory,
    IConditionEvaluator  defaultEvaluator,
    IConditionEvaluator? vbscriptEvaluator = null)
{
    private static readonly IReadOnlyDictionary<string, string> EmptyVars =
        new Dictionary<string, string>();

    // actionsElement: the <Actions> XElement (direct parent of Action/ActionGroup nodes).
    // baseData: shared context for all actions; ActionNode is updated per-iteration.
    public ActionResult Run(XElement actionsElement, ActionData baseData)
    {
        var cursor        = actionsElement.Elements().FirstOrDefault();
        var lastGuiAction = new Stack<XElement>();

        while (cursor is not null)
        {
            var localName = cursor.Name.LocalName;
            var isAction  = localName.Equals(XmlConstants.Elements.Action,      StringComparison.OrdinalIgnoreCase);
            var isGroup   = localName.Equals(XmlConstants.Elements.ActionGroup, StringComparison.OrdinalIgnoreCase);

            if (isAction || isGroup)
            {
                var condition  = (string?)cursor.Attribute(XmlConstants.Attributes.Condition) ?? string.Empty;
                var engineAttr = (string?)cursor.Attribute(XmlConstants.Attributes.ConditionEngine);
                var evaluator  = ResolveEvaluator(engineAttr);

                var conditionPasses = EvaluateCondition(condition, evaluator, baseData);

                if (isGroup)
                {
                    if (conditionPasses)
                    {
                        var firstChild = cursor.Elements().FirstOrDefault();
                        if (firstChild is not null)
                        {
                            cursor = firstChild;
                            continue;
                        }
                    }
                }
                else // isAction
                {
                    var typeName = (string?)cursor.Attribute(XmlConstants.Attributes.Type) ?? string.Empty;

                    if (conditionPasses)
                    {
                        var allowBack    = lastGuiAction.Count > 0;
                        var allowRefresh = IsRefreshable(cursor);

                        baseData.GlobalDialogTraits.AllowBack    = allowBack;
                        baseData.GlobalDialogTraits.AllowRefresh = allowRefresh;
                        baseData.ActionNode = cursor;

                        var action = factory.Create(typeName, baseData);

                        if (action is null)
                        {
                            baseData.Log.Write(
                                $"Unknown action type '{typeName}' — skipping.",
                                Logging.LogSeverity.Warning);
                        }
                        else
                        {
                            var result = action.Go();

                            switch (result)
                            {
                                case ActionResult.Back when allowBack:
                                    // Defensive pop: if current is somehow still on the stack, remove it.
                                    if (lastGuiAction.Count > 0 && lastGuiAction.Peek() == cursor)
                                        lastGuiAction.Pop();
                                    if (lastGuiAction.Count > 0)
                                        cursor = lastGuiAction.Pop();
                                    continue;

                                case ActionResult.Refresh when allowRefresh:
                                    // Jump to the parent ActionGroup; the next loop iteration re-evaluates it.
                                    cursor = cursor.Parent!;
                                    continue;

                                case ActionResult.Cancel:
                                    return ActionResult.Cancel;

                                case ActionResult.Next:
                                    if (action.IsGuiAction)
                                        lastGuiAction.Push(cursor);
                                    break;

                                // Back/Refresh when not allowed: fall through and advance normally.
                            }
                        }
                    }
                }
            }

            cursor = Advance(cursor);
        }

        return ActionResult.Next;
    }

    // -------------------------------------------------------------------------

    private IConditionEvaluator ResolveEvaluator(string? engineAttr)
    {
        if (!string.IsNullOrWhiteSpace(engineAttr) &&
            engineAttr.Equals(XmlConstants.Values.ConditionEngineVbscript,
                              StringComparison.OrdinalIgnoreCase) &&
            vbscriptEvaluator is not null)
        {
            return vbscriptEvaluator;
        }
        return defaultEvaluator;
    }

    private static bool EvaluateCondition(string condition, IConditionEvaluator evaluator, ActionData data)
    {
        if (string.IsNullOrWhiteSpace(condition)) return true;
        var substituted = data.TsEnv.Substitute(condition);
        var context = data.ActionNode.Attribute(XmlConstants.Attributes.Type)?.Value
                      ?? data.ActionNode.Name.LocalName;
        return evaluator.EvaluateLogged(substituted, data.Log, $"<{context}>");
    }

    // An action is refreshable if it is inside an ActionGroup and is not that group's first child.
    private static bool IsRefreshable(XElement actionNode)
    {
        var parent = actionNode.Parent;
        return parent is not null
            && parent.Name.LocalName.Equals(XmlConstants.Elements.ActionGroup, StringComparison.OrdinalIgnoreCase)
            && parent.Elements().FirstOrDefault() != actionNode;
    }

    // Advance the cursor: next sibling → parent ActionGroup's next sibling → null (done).
    private static XElement? Advance(XElement current)
    {
        var next = current.ElementsAfterSelf().FirstOrDefault();
        if (next is not null) return next;

        var parent = current.Parent;
        if (parent is not null &&
            parent.Name.LocalName.Equals(XmlConstants.Elements.ActionGroup, StringComparison.OrdinalIgnoreCase))
        {
            return parent.ElementsAfterSelf().FirstOrDefault();
        }

        return null;
    }
}
