using UiSharp.Core.Logging;

namespace UiSharp.Diagnostics.Checks;

/// <summary>
/// Where the runtime is, and whether it can write what it needs to.
///
/// The startup crash that stopped the runtime dead in every task sequence was
/// exactly this: a log directory read as a file path. These record what was
/// actually resolved so the next such fault is visible in a report rather than
/// discovered by a failed deployment.
/// </summary>
public sealed class EnvironmentChecks : ISelfCheck
{
    public string Area => "Environment";

    public IEnumerable<CheckResult> Run(SelfTestContext context)
    {
        yield return CheckResult.Info(Area, "Task-sequence environment",
            context.Env.InTS ? "available" : "not available (local fallback in use)");

        yield return CheckResult.Info(Area, "Log directory",
            context.Env.LogDirectory is { Length: > 0 } dir
                ? dir
                : "(none reported; the temp directory is used)");

        yield return CheckResult.Info(Area, "Log file in use",
            context.Log.FilePath ?? "(none — output is being discarded)");

        // The log is the only channel a deployment leaves behind, so an
        // unwritable one is worth reporting even though startup survives it.
        if (context.Log.FilePath is { Length: > 0 } logFile)
        {
            yield return File.Exists(logFile)
                ? CheckResult.Pass(Area, "The log file exists and is being written")
                : CheckResult.Fail(Area, "The log file exists and is being written",
                    $"{logFile} was not created");
        }
        else
        {
            yield return CheckResult.Fail(Area, "The log file exists and is being written",
                "no log file was opened, so this run leaves no record behind");
        }

        yield return WriteProbe(context.ScratchDirectory);

        yield return CheckResult.Info(Area, "Temp directory", Path.GetTempPath());

        yield return CheckResult.Info(Area, "Runtime version",
            typeof(EnvironmentChecks).Assembly.GetName().Version?.ToString() ?? "(unknown)");
    }

    private CheckResult WriteProbe(string directory)
    {
        // A crash report has to go somewhere; if nothing is writable the
        // operator needs to know before a crash proves it.
        var path = Path.Combine(directory, "write-probe.txt");

        try
        {
            File.WriteAllText(path, "probe");
            var read = File.ReadAllText(path);
            File.Delete(path);

            return read == "probe"
                ? CheckResult.Pass(Area, "Files can be written and read back")
                : CheckResult.Fail(Area, "Files can be written and read back",
                    $"wrote 'probe' but read back '{read}'");
        }
        catch (Exception ex)
        {
            return CheckResult.Fail(Area, "Files can be written and read back",
                $"{path}: {ex.Message}");
        }
    }
}
