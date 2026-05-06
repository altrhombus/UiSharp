using UIpp.Core.Configuration;
using UIpp.Core.Logging;

namespace UIpp.Core.Actions.Impl;

[ActionType(XmlConstants.ActionTypes.DefaultValues)]
public sealed class ActionDefaultValues(ActionData data) : ActionBase(data)
{
    public override ActionResult Go()
    {
        var types  = Attr(XmlConstants.Attributes.DefaultValueTypes, XmlConstants.Defaults.DefaultValueAll);
        var getAll = types.Contains(XmlConstants.Defaults.DefaultValueAll, StringComparison.OrdinalIgnoreCase);

        var provider = Data.DefaultValueProvider;
        if (provider is null)
        {
            Data.Log.Write(
                "DefaultValues: no provider registered — Windows-only categories unavailable.",
                LogSeverity.Warning);
            return ActionResult.Next;
        }

        foreach (var category in XmlConstants.DefaultValueCategories.Ordered)
        {
            if (!getAll && !types.Contains(category, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!provider.SupportedCategories.Contains(category))
            {
                Data.Log.Write(
                    $"DefaultValues: category '{category}' not supported by current provider.",
                    LogSeverity.Warning);
                continue;
            }

            try
            {
                provider.Collect(category, Data.TsEnv, Data.Log);
            }
            catch (Exception ex)
            {
                Data.Log.Write(
                    $"DefaultValues: error collecting '{category}': {ex.Message}",
                    LogSeverity.Error);
            }
        }

        return ActionResult.Next;
    }
}
