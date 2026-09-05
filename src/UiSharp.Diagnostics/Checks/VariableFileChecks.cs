using UiSharp.Core.Variables;

namespace UiSharp.Diagnostics.Checks;

/// <summary>
/// The files behind <c>&lt;Action Type="Vars"&gt;</c> and the SaveItems dump,
/// exercised against the live environment rather than a test double.
/// </summary>
public sealed class VariableFileChecks : ISelfCheck
{
    public string Area => "Variable files";

    private const string Prefix = "UiSharpSelfTestFile";

    public IEnumerable<CheckResult> Run(SelfTestContext context)
    {
        var env = context.Env;
        var path = Path.Combine(context.ScratchDirectory, "vars.dat");

        env.Set(Prefix + "Plain", "value");
        env.Set(Prefix + "Multiline", "first\nsecond");
        env.Set(Prefix + "Equals", "a=b");
        env.Set("X" + Prefix, "collected");

        var saveFailure = Attempt(() => env.SaveToFile(path));

        if (saveFailure is not null)
        {
            yield return CheckResult.Fail(Area, "Variables can be saved", saveFailure);
            yield break;
        }

        yield return File.Exists(path)
            ? CheckResult.Pass(Area, "Variables can be saved", $"{new FileInfo(path).Length} bytes")
            : CheckResult.Fail(Area, "Variables can be saved", "no file was produced");

        var saved = File.Exists(path) ? File.ReadAllText(path) : string.Empty;

        yield return saved.TrimStart().StartsWith('{')
            ? CheckResult.Pass(Area, "The saved file is JSON")
            : CheckResult.Fail(Area, "The saved file is JSON",
                $"begins with '{Truncate(saved, 40)}'");

        // X-prefixed names are facts about the machine, not the operator's data.
        yield return saved.Contains("X" + Prefix, StringComparison.Ordinal)
            ? CheckResult.Fail(Area, "Collected variables are left out of the file",
                $"X{Prefix} was saved")
            : CheckResult.Pass(Area, "Collected variables are left out of the file");

        // ---- round trip through a fresh environment
        var loaded = new LocalTSEnv();

        var loadFailure = Attempt(() => loaded.LoadFromFile(path));

        if (loadFailure is not null)
        {
            yield return CheckResult.Fail(Area, "Saved variables can be loaded back", loadFailure);
            yield break;
        }

        yield return loaded.Get(Prefix + "Plain") == "value"
            ? CheckResult.Pass(Area, "Saved variables can be loaded back")
            : CheckResult.Fail(Area, "Saved variables can be loaded back",
                $"expected 'value', got '{loaded.Get(Prefix + "Plain")}'");

        // The reason the format is JSON: a line-per-variable file corrupts on
        // these two, and the reload then invents variables from the remainder.
        yield return loaded.Get(Prefix + "Multiline") == "first\nsecond"
            ? CheckResult.Pass(Area, "A value containing newlines survives")
            : CheckResult.Fail(Area, "A value containing newlines survives",
                $"got '{Truncate(loaded.Get(Prefix + "Multiline"), 40)}'");

        yield return loaded.Get(Prefix + "Equals") == "a=b"
            ? CheckResult.Pass(Area, "A value containing '=' survives")
            : CheckResult.Fail(Area, "A value containing '=' survives",
                $"got '{loaded.Get(Prefix + "Equals")}'");

        // ---- the human-readable dump is a different thing
        var dumpPath = Path.Combine(context.ScratchDirectory, "dump.txt");

        try
        {
            env.DumpToFile(dumpPath);

            var lines = File.ReadAllLines(dumpPath);

            yield return lines.Any(l => l.StartsWith(Prefix + "Plain=", StringComparison.Ordinal))
                ? CheckResult.Pass(Area, "The dump is one name=value per line",
                    $"{lines.Length} lines")
                : CheckResult.Fail(Area, "The dump is one name=value per line",
                    $"{lines.Length} lines, none matching {Prefix}Plain=");
        }
        finally
        {
            foreach (var suffix in new[] { "Plain", "Multiline", "Equals" })
            {
                try { env.Set(Prefix + suffix, string.Empty); } catch { }
            }

            try { env.Set("X" + Prefix, string.Empty); } catch { }
        }

        // ---- a damaged file must not stop a deployment
        var damaged = Path.Combine(context.ScratchDirectory, "damaged.dat");
        File.WriteAllText(damaged, "{ not valid json");

        CheckResult damagedResult;
        try
        {
            new LocalTSEnv().LoadFromFile(damaged);
            damagedResult = CheckResult.Pass(Area, "A damaged file is ignored rather than fatal");
        }
        catch (Exception ex)
        {
            damagedResult = CheckResult.Fail(Area, "A damaged file is ignored rather than fatal",
                $"{ex.GetType().Name}: {ex.Message}");
        }

        yield return damagedResult;
    }

    /// <summary>
    /// Runs an action, returning null on success or a description of what went
    /// wrong. An iterator cannot yield from inside a catch, and swallowing the
    /// exception to work around that would lose the one thing worth reporting.
    /// </summary>
    private static string? Attempt(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return $"{ex.GetType().Name}: {ex.Message}";
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value.Replace("\n", "\\n") : value[..max].Replace("\n", "\\n") + "…";
}
