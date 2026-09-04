using Microsoft.Win32;
using UiSharp.Core.Actions;
using UiSharp.Core.Configuration;
using UiSharp.Core.Logging;

namespace UiSharp.Windows.Actions;

[ActionType(XmlConstants.ActionTypes.RegRead)]
public sealed class ActionRegRead(ActionData data) : ActionBase(data)
{
    public override ActionResult Go()
    {
        var hive         = Attr(XmlConstants.Attributes.Hive);
        var key          = Attr(XmlConstants.Attributes.Key);
        var valueName    = Attr(XmlConstants.Attributes.Value);
        var variable     = Attr(XmlConstants.Attributes.Variable);
        var defaultValue = Attr(XmlConstants.Attributes.Default);
        var reg64        = BoolAttr(XmlConstants.Attributes.Reg64, def: true); // original defaults to 64-bit

        // C++ requires both key and value name to be non-empty.
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(valueName) ||
            string.IsNullOrWhiteSpace(variable))
            return ActionResult.Next;

        var view = reg64 ? RegistryView.Registry64 : RegistryView.Registry32;
        using var baseKey = OpenBaseKey(hive, view);
        if (baseKey is null)
        {
            Data.Log.Write($"RegRead: unknown hive '{hive}'.", LogSeverity.Warning);
            return ActionResult.Next;
        }

        try
        {
            using var regKey = baseKey.OpenSubKey(key, writable: false);
            var raw    = regKey?.GetValue(valueName);
            var result = raw switch
            {
                null   => string.Empty,
                byte[] bytes => BitConverter.ToString(bytes).Replace("-", ""),
                _      => raw.ToString() ?? string.Empty,
            };

            // C++: if value is empty string and a Default is specified, use it.
            if (result.Length == 0 && defaultValue.Length > 0)
                result = defaultValue;

            Data.TsEnv.Set(variable, result);
            Data.Log.Write($"RegRead: {hive}\\{key}\\{valueName} → {variable}={result}");
        }
        catch (Exception ex)
        {
            Data.Log.Write($"RegRead: failed reading '{key}': {ex.Message}", LogSeverity.Warning);

            // C++: sets variable to Default value when key read fails and Default is specified.
            if (defaultValue.Length > 0)
                Data.TsEnv.Set(variable, defaultValue);
        }

        return ActionResult.Next;
    }

    private static RegistryKey? OpenBaseKey(string? hive, RegistryView view) =>
        hive?.ToUpperInvariant() switch
        {
            "HKLM" or "HKEY_LOCAL_MACHINE"  => RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,  view),
            "HKCU" or "HKEY_CURRENT_USER"   => RegistryKey.OpenBaseKey(RegistryHive.CurrentUser,   view),
            "HKCR" or "HKEY_CLASSES_ROOT"   => RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot,   view),
            "HKU"  or "HKEY_USERS"          => RegistryKey.OpenBaseKey(RegistryHive.Users,         view),
            "HKCC" or "HKEY_CURRENT_CONFIG" => RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, view),
            _                               => null,
        };
}
