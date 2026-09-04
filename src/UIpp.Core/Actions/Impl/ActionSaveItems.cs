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

        if (string.IsNullOrWhiteSpace(destPath) || string.IsNullOrWhiteSpace(items))
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
                // Ask the log where it is, as the original does
                // (Actions.cpp:996), rather than guessing from the environment.
                TryCopy(Data.Log.FilePath, destPath);
            }
            else if (token.Equals("SMSTSLOG", StringComparison.OrdinalIgnoreCase))
            {
                // Copy smsts*.log from the ConfigMgr log path (%_SMSTSLogPath%).
                var logPath = Data.TsEnv.Substitute("%_SMSTSLogPath%");
                if (string.IsNullOrWhiteSpace(logPath) || !Directory.Exists(logPath))
                {
                    Data.Log.Write(
                        "SaveItems: SMSTSLOG — %_SMSTSLogPath% is not set or directory does not exist.",
                        LogSeverity.Warning);
                }
                else
                {
                    foreach (var f in Directory.GetFiles(logPath, "smsts*.log",
                                 SearchOption.TopDirectoryOnly))
                        TryCopy(f, destPath);
                }
            }
            else
            {
                // Support wildcard patterns like "%temp%\*.log".
                TryCopyGlob(token, destPath);
            }
        }

        return ActionResult.Next;
    }

    private void TryCopyGlob(string srcPattern, string destDir)
    {
        var expanded = Environment.ExpandEnvironmentVariables(srcPattern);
        var dir      = Path.GetDirectoryName(expanded);
        var pattern  = Path.GetFileName(expanded);

        if (string.IsNullOrEmpty(pattern)) return;

        if (pattern.Contains('*') || pattern.Contains('?'))
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;
            foreach (var f in Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly))
                TryCopy(f, destDir);
        }
        else
        {
            TryCopy(expanded, destDir);
        }
    }

    private void TryCopy(string? src, string destDir)
    {
        if (string.IsNullOrWhiteSpace(src)) return;
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
