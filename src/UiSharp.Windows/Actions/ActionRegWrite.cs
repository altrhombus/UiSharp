using Microsoft.Win32;
using UiSharp.Core.Actions;
using UiSharp.Core.Configuration;
using UiSharp.Core.Logging;

namespace UiSharp.Windows.Actions;

[ActionType(XmlConstants.ActionTypes.RegWrite)]
public sealed class ActionRegWrite(ActionData data) : ActionBase(data)
{
    public override ActionResult Go()
    {
        var hive      = Attr(XmlConstants.Attributes.Hive);
        var key       = Attr(XmlConstants.Attributes.Key);
        var valueName = Attr(XmlConstants.Attributes.Value);
        // Value data is the inner text of the <Action> element (matches C++ child_value()).
        var valueData = Data.TsEnv.Substitute(Data.ActionNode.Value.Trim());
        var valueType = Attr(XmlConstants.Attributes.RegValueType) ?? "REG_SZ";
        var reg64     = BoolAttr(XmlConstants.Attributes.Reg64, def: true); // original defaults to 64-bit

        // C++ requires both key and value name to be non-empty.
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(valueName))
            return ActionResult.Next;

        var view = reg64 ? RegistryView.Registry64 : RegistryView.Registry32;
        using var baseKey = OpenBaseKey(hive, view);
        if (baseKey is null)
        {
            Data.Log.Write($"RegWrite: unknown hive '{hive}'.", LogSeverity.Warning);
            return ActionResult.Next;
        }

        try
        {
            using var regKey = baseKey.CreateSubKey(key, writable: true);
            if (regKey is null)
            {
                Data.Log.Write($"RegWrite: could not create/open '{key}'.", LogSeverity.Warning);
                return ActionResult.Next;
            }

            switch (valueType.ToUpperInvariant())
            {
                case "REG_DWORD":
                    if (uint.TryParse(valueData, out var dword))
                        regKey.SetValue(valueName, (int)dword, RegistryValueKind.DWord);
                    break;
                case "REG_QWORD":
                    if (ulong.TryParse(valueData, out var qword))
                        regKey.SetValue(valueName, (long)qword, RegistryValueKind.QWord);
                    break;
                case "REG_EXPAND_SZ":
                    regKey.SetValue(valueName, valueData, RegistryValueKind.ExpandString);
                    break;
                case "REG_MULTI_SZ":
                    regKey.SetValue(valueName, valueData.Split('\n'), RegistryValueKind.MultiString);
                    break;
                default: // REG_SZ
                    regKey.SetValue(valueName, valueData, RegistryValueKind.String);
                    break;
            }

            Data.Log.Write($"RegWrite: {hive}\\{key}\\{valueName} = {valueData} ({valueType})");
        }
        catch (Exception ex)
        {
            Data.Log.Write($"RegWrite: failed writing '{key}': {ex.Message}", LogSeverity.Warning);
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
