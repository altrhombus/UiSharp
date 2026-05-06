using UIpp.Core.Configuration;
using UIpp.Core.Logging;

namespace UIpp.Core.Actions.Impl;

[ActionType(XmlConstants.ActionTypes.SaveItems)]
public sealed class ActionSaveItems(ActionData data) : ActionBase(data)
{
    public override ActionResult Go()
    {
        var destPath = Data.TsEnv.Substitute(Attr(XmlConstants.Attributes.Path));
        var items    = Attr(XmlConstants.Attributes.Items);

        if (string.IsNullOrEmpty(destPath) || string.IsNullOrEmpty(items))
            return ActionResult.Next;

        foreach (var rawToken in items.Split([',', ';'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var token = Data.TsEnv.Substitute(rawToken);

            const string tsVarsPrefix = "TSVariables";
            if (token.Equals(tsVarsPrefix, StringComparison.OrdinalIgnoreCase) ||
                token.StartsWith(tsVarsPrefix + ":", StringComparison.OrdinalIgnoreCase))
            {
                var filename = token.Length > tsVarsPrefix.Length + 1
                    ? token[(tsVarsPrefix.Length + 1)..]
                    : "UI++ Variable Dump.txt";

                try
                {
                    Directory.CreateDirectory(destPath);
                    Data.TsEnv.DumpToFile(Path.Combine(destPath, filename));
                }
                catch (Exception ex)
                {
                    Data.Log.Write($"SaveItems: failed to write variables file: {ex.Message}", LogSeverity.Warning);
                }
            }
            else if (token.Equals("UILOG", StringComparison.OrdinalIgnoreCase))
            {
                TryCopy(Data.TsEnv.LogPath, destPath);
            }
            else if (token.Equals("SMSTSLOG", StringComparison.OrdinalIgnoreCase))
            {
                // ConfigMgr smsts*.log — skip on non-Windows; on Windows the caller
                // can locate logs via %_SMSTSLogPath%.
                Data.Log.Write("SaveItems: SMSTSLOG not implemented in UIpp.Core.", LogSeverity.Warning);
            }
            else
            {
                TryCopy(Environment.ExpandEnvironmentVariables(token), destPath);
            }
        }

        return ActionResult.Next;
    }

    private void TryCopy(string? src, string destDir)
    {
        if (string.IsNullOrEmpty(src)) return;
        try
        {
            Directory.CreateDirectory(destDir);
            File.Copy(src, Path.Combine(destDir, Path.GetFileName(src)), overwrite: true);
        }
        catch (Exception ex)
        {
            Data.Log.Write($"SaveItems: cannot copy '{src}': {ex.Message}", LogSeverity.Warning);
        }
    }
}
