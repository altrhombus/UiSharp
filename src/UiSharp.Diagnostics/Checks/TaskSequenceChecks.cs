namespace UiSharp.Diagnostics.Checks;

/// <summary>
/// Reading, writing and enumerating task-sequence variables.
///
/// This is the path with the worst track record. Inside a task sequence
/// variables live in the ConfigMgr environment object, and code that assumed
/// otherwise shipped twice: variable files wrote an empty file because the
/// local dictionary was enumerated instead, and the same misunderstanding put
/// the log in the wrong place. None of it is reachable from a unit test.
///
/// The variables written here are prefixed and removed afterwards, so a
/// self-test run leaves the deployment's own variables untouched.
/// </summary>
public sealed class TaskSequenceChecks : ISelfCheck
{
    public string Area => "Task sequence";

    private const string Prefix = "UiSharpSelfTest";

    public IEnumerable<CheckResult> Run(SelfTestContext context)
    {
        var env = context.Env;
        var name = Prefix + "Value";

        // ---- set and read back
        CheckResult roundTrip;
        try
        {
            env.Set(name, "hello");
            var read = env.Get(name);

            roundTrip = read == "hello"
                ? CheckResult.Pass(Area, "A variable can be set and read back")
                : CheckResult.Fail(Area, "A variable can be set and read back",
                    $"set 'hello', read '{read}'");
        }
        catch (Exception ex)
        {
            roundTrip = CheckResult.Fail(Area, "A variable can be set and read back", ex.Message);
        }

        yield return roundTrip;

        // ---- Exists
        yield return env.Exists(name)
            ? CheckResult.Pass(Area, "A set variable reports as existing")
            : CheckResult.Fail(Area, "A set variable reports as existing",
                $"{name} was set but Exists() said no");

        yield return env.Exists(Prefix + "NeverSet")
            ? CheckResult.Fail(Area, "An unset variable reports as absent",
                "Exists() said yes for a variable that was never set")
            : CheckResult.Pass(Area, "An unset variable reports as absent");

        // ---- substitution against the live environment
        var substituted = env.Substitute($"[%{name}%]");

        yield return substituted == "[hello]"
            ? CheckResult.Pass(Area, "%Variable% is substituted from the environment")
            : CheckResult.Fail(Area, "%Variable% is substituted from the environment",
                $"expected '[hello]', got '{substituted}'");

        // An unresolved token must survive intact rather than becoming empty —
        // conditions rely on being able to tell the difference.
        var unresolved = env.Substitute($"%{Prefix}NeverSet%");

        yield return unresolved == $"%{Prefix}NeverSet%"
            ? CheckResult.Pass(Area, "An unset variable is left as a literal %Token%")
            : CheckResult.Fail(Area, "An unset variable is left as a literal %Token%",
                $"got '{unresolved}'");

        // ---- enumeration, the one that shipped broken
        yield return EnumerationCheck(context, name);

        // ---- numeric overload
        try
        {
            env.Set(Prefix + "Number", 42UL);

            yield return env.Get(Prefix + "Number") == "42"
                ? CheckResult.Pass(Area, "A numeric variable is stored as text")
                : CheckResult.Fail(Area, "A numeric variable is stored as text",
                    $"got '{env.Get(Prefix + "Number")}'");
        }
        finally
        {
            Cleanup(env);
        }
    }

    private CheckResult EnumerationCheck(SelfTestContext context, string name)
    {
        // Saving variables to a file depends on this. Inside a task sequence it
        // goes through SMS_TSEnvironment.GetVariables(); a run that cannot
        // enumerate writes an empty file and says nothing.
        var path = Path.Combine(context.ScratchDirectory, "enumerate.dat");

        try
        {
            context.Env.SaveToFile(path);

            if (!File.Exists(path))
                return CheckResult.Fail(Area, "Variables can be enumerated for saving",
                    "SaveToFile produced no file");

            var text = File.ReadAllText(path);

            return text.Contains(name, StringComparison.OrdinalIgnoreCase)
                ? CheckResult.Pass(Area, "Variables can be enumerated for saving",
                    $"{text.Length} bytes written")
                : CheckResult.Fail(Area, "Variables can be enumerated for saving",
                    $"{name} was set but is absent from the saved file — the environment " +
                    "is probably not being enumerated");
        }
        catch (Exception ex)
        {
            return CheckResult.Fail(Area, "Variables can be enumerated for saving", ex.Message);
        }
    }

    private static void Cleanup(Core.Variables.ITSEnv env)
    {
        // Blanking is the most that can be done: the environment object has no
        // delete. Leaving the deployment's own state alone matters more than
        // tidiness here.
        foreach (var suffix in new[] { "Value", "Number" })
        {
            try { env.Set(Prefix + suffix, string.Empty); } catch { }
        }
    }
}
