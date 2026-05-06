using System.Diagnostics;
using UIpp.Core.Configuration;
using UIpp.Core.Logging;

namespace UIpp.Core.Actions.Impl;

[ActionType(XmlConstants.ActionTypes.ExternalCall)]
public sealed class ActionExternalCall(ActionData data) : ActionBase(data)
{
    public override ActionResult Go()
    {
        var commandLine = Data.TsEnv.Substitute(Data.ActionNode.Value.Trim());
        var exitVar     = Attr(XmlConstants.Attributes.ExitCodeVariable);

        if (!int.TryParse(Attr(XmlConstants.Attributes.MaxRunTime,
                XmlConstants.Defaults.MaxRunTime.ToString()), out var maxRunTime))
        {
            maxRunTime = XmlConstants.Defaults.MaxRunTime;
        }

        if (string.IsNullOrWhiteSpace(commandLine)) return ActionResult.Next;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName        = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
                Arguments       = OperatingSystem.IsWindows()
                                    ? $"/c {commandLine}"
                                    // Wrap as a single -c argument so the shell sees the
                                    // whole command line as one string (not split on spaces).
                                    : $"-c \"{commandLine.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                CreateNoWindow  = true,
            };

            using var proc = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start process.");

            var completed = proc.WaitForExit(maxRunTime * 1000);

            if (!completed)
            {
                proc.Kill(entireProcessTree: true);
                Data.Log.Write(
                    $"ExternalCall: process exceeded {maxRunTime}s — terminated.",
                    LogSeverity.Warning);
            }

            if (!string.IsNullOrEmpty(exitVar))
                Data.TsEnv.Set(exitVar, completed ? proc.ExitCode.ToString() : "-1");
        }
        catch (Exception ex)
        {
            Data.Log.Write($"ExternalCall: {ex.Message}", LogSeverity.Error);
        }

        return ActionResult.Next;
    }
}
