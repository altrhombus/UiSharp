using UiSharp.Core.Actions;
using UiSharp.Core.Logging;
using UiSharp.Core.Variables;

namespace UiSharp.Diagnostics;

/// <summary>How a single check turned out.</summary>
public enum CheckOutcome
{
    /// <summary>Behaved as expected.</summary>
    Pass,

    /// <summary>Did not. Something here does not work on this machine.</summary>
    Fail,

    /// <summary>Could not be judged here — see the detail for why.</summary>
    Skip,

    /// <summary>Not a test: a fact about the environment, recorded for context.</summary>
    Info,
}

/// <param name="Area">Grouping for the report, e.g. "Task sequence".</param>
/// <param name="Name">What was checked, phrased as the expectation.</param>
/// <param name="Detail">
/// What actually happened. Always populated for a failure — a report saying only
/// that something failed is not worth carrying out of a WinPE session.
/// </param>
public sealed record CheckResult(string Area, string Name, CheckOutcome Outcome, string Detail)
{
    public static CheckResult Pass(string area, string name, string detail = "") =>
        new(area, name, CheckOutcome.Pass, detail);

    public static CheckResult Fail(string area, string name, string detail) =>
        new(area, name, CheckOutcome.Fail, detail);

    public static CheckResult Skip(string area, string name, string detail) =>
        new(area, name, CheckOutcome.Skip, detail);

    public static CheckResult Info(string area, string name, string detail) =>
        new(area, name, CheckOutcome.Info, detail);
}

/// <summary>What a check is given to work with.</summary>
/// <param name="Env">
/// The live task-sequence environment — the real one when running inside a task
/// sequence, which is the whole point of this exercise.
/// </param>
/// <param name="Log">The runtime's log, so a self-test run is traceable.</param>
/// <param name="ScratchDirectory">
/// A directory the checks may write to, created and removed by the runner.
/// </param>
/// <param name="Factory">
/// The action registry the runtime built for this run. It is passed in rather
/// than rebuilt here so the checks see exactly what the published executable
/// discovered: trimming can drop an action type with no error at all, and a
/// registry assembled locally would not notice.
/// </param>
public sealed record SelfTestContext(
    ITSEnv Env,
    ICMLog Log,
    string ScratchDirectory,
    ActionFactory Factory);

/// <summary>
/// One group of related checks.
///
/// A check must never throw: the runner catches anything that escapes and turns
/// it into a failure, because a self-test that dies half way through tells you
/// less than one that reports what broke.
/// </summary>
public interface ISelfCheck
{
    string Area { get; }

    IEnumerable<CheckResult> Run(SelfTestContext context);
}

/// <summary>
/// The outcome of a whole run, and the report that goes back with the logs.
/// </summary>
public sealed class SelfTestReport(
    IReadOnlyList<CheckResult> results,
    TimeSpan duration,
    bool inTaskSequence)
{
    public IReadOnlyList<CheckResult> Results { get; } = results;
    public TimeSpan Duration { get; } = duration;

    /// <summary>
    /// Whether this ran where it was meant to. A clean run outside a task
    /// sequence proves far less than the same run inside one, and saying so in
    /// the report stops it being read as more evidence than it is.
    /// </summary>
    public bool InTaskSequence { get; } = inTaskSequence;

    public int Passed  => Results.Count(r => r.Outcome == CheckOutcome.Pass);
    public int Failed  => Results.Count(r => r.Outcome == CheckOutcome.Fail);
    public int Skipped => Results.Count(r => r.Outcome == CheckOutcome.Skip);

    public bool AllPassed => Failed == 0;

    public string Summary =>
        $"{Passed} passed, {Failed} failed, {Skipped} skipped in {Duration.TotalSeconds:0.0}s";

    /// <summary>
    /// The report as text. Written for someone reading it on a WinPE console or
    /// pulling it out of a log folder afterwards, so failures come first and
    /// nothing needs a tool to interpret.
    /// </summary>
    public string ToText()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("UiSharp self-test");
        sb.AppendLine($"  {DateTime.Now:yyyy-MM-dd HH:mm:ss} on {Environment.MachineName}");
        sb.AppendLine($"  {Summary}");
        sb.AppendLine(InTaskSequence
            ? "  Running inside a task sequence."
            : "  NOT running inside a task sequence: the local fallback environment is in " +
              "use, so the task-sequence results below prove nothing about a real deployment.");
        sb.AppendLine();

        if (Failed > 0)
        {
            sb.AppendLine("FAILURES");
            sb.AppendLine("--------");
            foreach (var r in Results.Where(r => r.Outcome == CheckOutcome.Fail))
            {
                sb.AppendLine($"  [{r.Area}] {r.Name}");
                sb.AppendLine($"      {r.Detail}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("ALL CHECKS");
        sb.AppendLine("----------");

        foreach (var area in Results.Select(r => r.Area).Distinct())
        {
            sb.AppendLine($"  {area}");

            foreach (var r in Results.Where(r => r.Area == area))
            {
                var mark = r.Outcome switch
                {
                    CheckOutcome.Pass => "pass",
                    CheckOutcome.Fail => "FAIL",
                    CheckOutcome.Skip => "skip",
                    _                 => "info",
                };

                if (r.Outcome == CheckOutcome.Info)
                {
                    sb.AppendLine($"    {mark}  {r.Name}: {Display(r.Detail)}");
                    continue;
                }

                sb.AppendLine($"    {mark}  {r.Name}");

                if (!string.IsNullOrWhiteSpace(r.Detail))
                    sb.AppendLine($"          {r.Detail}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string Display(string detail) =>
        string.IsNullOrWhiteSpace(detail) ? "(none)" : detail.ReplaceLineEndings(" ");
}

/// <summary>
/// Runs the checks and writes the report.
///
/// This exists because the runtime has never been exercised inside a real task
/// sequence, and every serious bug found so far has lived in exactly that path:
/// the log directory, enumerating task-sequence variables, starting up at all.
/// Unit tests cannot reach any of it — only running there can.
/// </summary>
public sealed class SelfTestRunner(IReadOnlyList<ISelfCheck> checks)
{
    /// <summary>The checks this runner will run, in order.</summary>
    public IReadOnlyList<ISelfCheck> Checks { get; } = checks;

    /// <summary>The standard set, in the order the report reads best.</summary>
    public static SelfTestRunner Standard() => new(
    [
        new Checks.EnvironmentChecks(),
        new Checks.TaskSequenceChecks(),
        new Checks.VariableFileChecks(),
        new Checks.ConditionEngineChecks(),
        new Checks.ActionPipelineChecks(),
        new Checks.PlatformChecks(),
    ]);

    public SelfTestReport Run(ITSEnv env, ICMLog log, ActionFactory factory)
    {
        var started = DateTime.UtcNow;
        var results = new List<CheckResult>();

        var scratch = Path.Combine(Path.GetTempPath(), "uisharp-selftest-" + Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(scratch);
        }
        catch (Exception ex)
        {
            results.Add(CheckResult.Fail("Self-test", "A scratch directory can be created",
                $"{scratch}: {ex.Message}"));

            return new SelfTestReport(results, DateTime.UtcNow - started, env.InTS);
        }

        var context = new SelfTestContext(env, log, scratch, factory);

        foreach (var check in Checks)
        {
            try
            {
                results.AddRange(check.Run(context));
            }
            catch (Exception ex)
            {
                // A check that throws is itself a finding, and the rest of the
                // run still has value.
                results.Add(CheckResult.Fail(check.Area, "The checks in this area ran to completion",
                    $"{ex.GetType().Name}: {ex.Message}"));
            }
        }

        try { Directory.Delete(scratch, recursive: true); } catch { /* best effort */ }

        return new SelfTestReport(results, DateTime.UtcNow - started, env.InTS);
    }
}
