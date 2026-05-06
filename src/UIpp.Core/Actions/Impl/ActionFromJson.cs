using System.Text.Json;
using UIpp.Core.Configuration;
using UIpp.Core.Logging;

namespace UIpp.Core.Actions.Impl;

[ActionType(XmlConstants.ActionTypes.FromJson)]
public sealed class ActionFromJson(ActionData data) : ActionBase(data)
{
    public override ActionResult Go()
    {
        var jsonStr = Data.TsEnv.Substitute(Data.ActionNode.Value.Trim());
        if (string.IsNullOrWhiteSpace(jsonStr)) return ActionResult.Next;

        try
        {
            using var doc = JsonDocument.Parse(jsonStr);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    var name  = Data.TsEnv.Substitute(prop.Name);
                    var value = Data.TsEnv.Substitute(prop.Value.GetString() ?? string.Empty);
                    Data.TsEnv.Set(name, value);
                }
                else
                {
                    Data.Log.Write(
                        $"FromJSON: '{prop.Name}' has non-string value — skipping.",
                        LogSeverity.Warning);
                }
            }
        }
        catch (JsonException ex)
        {
            Data.Log.Write($"FromJSON: parse error — {ex.Message}", LogSeverity.Warning);
        }

        return ActionResult.Next;
    }
}
