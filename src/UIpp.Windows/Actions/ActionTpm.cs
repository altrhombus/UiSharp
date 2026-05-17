using System.Management;
using UIpp.Core.Actions;
using UIpp.Core.Configuration;
using UIpp.Core.Logging;

namespace UIpp.Windows.Actions;

// Reads TPM status from Win32_Tpm and sets standard XTPMxxx variables.
// The "Request" attribute may contain "TakeOwnership", "ClearOwnership" etc.,
// but WMI method invocation on Win32_Tpm requires elevated rights and is complex;
// the most common use case is simply reading status, which is what most configs need.
[ActionType(XmlConstants.ActionTypes.Tpm)]
public sealed class ActionTpm(ActionData data) : ActionBase(data)
{
    private const string TpmNamespace = @"root\cimv2\Security\MicrosoftTpm";
    private const string TpmClass     = "Win32_Tpm";

    public override ActionResult Go()
    {
        try
        {
            var scope = new ManagementScope(TpmNamespace);
            scope.Connect();

            using var searcher = new ManagementObjectSearcher(scope,
                new ObjectQuery($"SELECT * FROM {TpmClass}"));
            using var results = searcher.Get();

            var instances = results.Cast<ManagementObject>().ToList();

            var available = instances.Count > 0;
            Data.TsEnv.Set(XmlConstants.Variables.TpmAvailable, available ? XmlConstants.Values.True : XmlConstants.Values.False);

            if (!available)
            {
                Data.TsEnv.Set(XmlConstants.Variables.TpmEnabled,     XmlConstants.Values.False);
                Data.TsEnv.Set(XmlConstants.Variables.TpmActivated,   XmlConstants.Values.False);
                Data.TsEnv.Set(XmlConstants.Variables.TpmOwned,       XmlConstants.Values.False);
                Data.TsEnv.Set(XmlConstants.Variables.TpmSpecVersion, string.Empty);
                return ActionResult.Next;
            }

            using var tpm = instances[0];

            Data.TsEnv.Set(XmlConstants.Variables.TpmSpecVersion,
                tpm["SpecVersion"]?.ToString() ?? string.Empty);

            // Invoke WMI methods to get status booleans.
            Data.TsEnv.Set(XmlConstants.Variables.TpmEnabled,   InvokeMethod(tpm, scope, "IsEnabled")   ? XmlConstants.Values.True : XmlConstants.Values.False);
            Data.TsEnv.Set(XmlConstants.Variables.TpmActivated, InvokeMethod(tpm, scope, "IsActivated") ? XmlConstants.Values.True : XmlConstants.Values.False);
            Data.TsEnv.Set(XmlConstants.Variables.TpmOwned,     InvokeMethod(tpm, scope, "IsOwned")     ? XmlConstants.Values.True : XmlConstants.Values.False);

            Data.Log.Write($"TPM: available={available}, specVersion={tpm["SpecVersion"]}");
        }
        catch (Exception ex)
        {
            Data.Log.Write($"TPM: failed querying Win32_Tpm: {ex.Message}", LogSeverity.Warning);
            Data.TsEnv.Set(XmlConstants.Variables.TpmAvailable, XmlConstants.Values.False);
        }

        return ActionResult.Next;
    }

    private static bool InvokeMethod(ManagementObject tpm, ManagementScope scope, string methodName)
    {
        try
        {
            using var outParams = tpm.InvokeMethod(methodName, null, null);
            if (outParams is null) return false;
            // Methods return isEnabled/isActivated/isOwned boolean out params.
            var key = methodName[2..]; // "IsEnabled" → "is" + "Enabled" → look for "isEnabled"
            var boolKey = char.ToLowerInvariant(key[0]) + key[1..];
            return outParams[boolKey] is true;
        }
        catch { return false; }
    }
}
