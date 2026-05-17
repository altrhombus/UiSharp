using System.Management;
using UIpp.Core.Actions;
using UIpp.Core.Configuration;
using UIpp.Core.Logging;

namespace UIpp.Windows.Actions;

[ActionType(XmlConstants.ActionTypes.WmiRead)]
public sealed class ActionWmiRead(ActionData data) : ActionBase(data)
{
    public override ActionResult Go()
    {
        var ns           = SubstAttr(XmlConstants.Attributes.Namespace) ?? XmlConstants.Defaults.Namespace;
        var cls          = SubstAttr(XmlConstants.Attributes.Class);
        var property     = SubstAttr(XmlConstants.Attributes.Property);
        var keyQualifier = SubstAttr(XmlConstants.Attributes.KeyQualifier);
        var variable     = Attr(XmlConstants.Attributes.Variable) ?? XmlConstants.Defaults.Variable;
        var defaultValue = Attr(XmlConstants.Attributes.Default);
        var query        = SubstAttr(XmlConstants.Attributes.Query);

        // C++: property is required; need class (for property lookup) or a WQL query string.
        if (string.IsNullOrWhiteSpace(property))
            return ActionResult.Next;
        if (string.IsNullOrWhiteSpace(cls) && string.IsNullOrWhiteSpace(query))
            return ActionResult.Next;

        string? wmiValue = null;

        try
        {
            var scope = new ManagementScope(ns);
            scope.Connect();

            if (!string.IsNullOrWhiteSpace(cls))
            {
                // C++: class-based query has priority over the Query attribute.
                // KeyQualifier maps to the WHERE clause (e.g. "Name='Spooler'").
                var wql = string.IsNullOrWhiteSpace(keyQualifier)
                    ? $"SELECT {property} FROM {cls}"
                    : $"SELECT {property} FROM {cls} WHERE {keyQualifier}";
                using var s = new ManagementObjectSearcher(scope, new ObjectQuery(wql));
                using var col = s.Get();
                wmiValue = col.Cast<ManagementObject>()
                              .Select(o => o[property]?.ToString())
                              .FirstOrDefault(v => v is { Length: > 0 });
            }
            else
            {
                // WQL query-only mode (class is empty).
                using var s = new ManagementObjectSearcher(scope, new ObjectQuery(query!));
                using var col = s.Get();
                wmiValue = col.Cast<ManagementObject>()
                              .Select(o => o[property]?.ToString())
                              .FirstOrDefault(v => v is { Length: > 0 });
            }
        }
        catch (Exception ex)
        {
            Data.Log.Write($"WmiRead: failed querying {ns}:{cls ?? query}: {ex.Message}", LogSeverity.Warning);
        }

        // C++: use Default when no non-empty value was found.
        if (wmiValue is null && !string.IsNullOrEmpty(defaultValue))
            wmiValue = defaultValue;

        // C++: only set variable when a value was found or defaulted.
        if (wmiValue is not null)
        {
            Data.TsEnv.Set(variable, wmiValue);
            Data.Log.Write($"WmiRead: {ns}:{cls ?? query}.{property} → {variable}={wmiValue}");
        }

        return ActionResult.Next;
    }
}
