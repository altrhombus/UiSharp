using System.Xml.Linq;
using UIpp.Core.Actions;
using UIpp.Core.Dialogs;
using UIpp.Core.Logging;
using UIpp.Core.Scripting;
using UIpp.Core.Variables;

namespace UIpp.Core.Tests.Actions.Impl;

// Shared infrastructure for action implementation tests.
internal sealed class NullLog : ICMLog
{
    public readonly List<string> Messages = [];
    public void Write(string message, LogSeverity severity = LogSeverity.Info, string component = "UIpp")
        => Messages.Add(message);
}

internal static class ActionTestData
{
    public static (LocalTSEnv env, NullLog log, ActionData data) Make(XElement node)
    {
        var env  = new LocalTSEnv();
        var log  = new NullLog();
        var data = new ActionData
        {
            ActionNode         = node,
            Conditions         = new NativeConditionEvaluator(),
            TsEnv              = env,
            Log                = log,
            GlobalDialogTraits = new DialogTraits(),
        };
        return (env, log, data);
    }

    // Build a minimal <Action Type="..."> element from inline XML.
    public static XElement ActionEl(string xml) => XElement.Parse(xml);
}
