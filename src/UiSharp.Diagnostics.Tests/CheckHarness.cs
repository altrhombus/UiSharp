using System.Reflection;
using UiSharp.Core.Actions;
using UiSharp.Core.Logging;
using UiSharp.Core.Variables;
using UiSharp.Diagnostics;

namespace UiSharp.Diagnostics.Tests;

/// <summary>
/// Runs one check the way the runner does, so a test sees what a real run sees.
///
/// The checks are the thing under test here, not the machine: if a check reports
/// a failure on a healthy development box then the check is wrong, or the
/// behaviour it describes has regressed. Either way the test should fail.
/// </summary>
internal static class CheckHarness
{
    /// <summary>
    /// The same action registry the runtime builds. Assembled by loading the
    /// assemblies rather than referencing their types so this project does not
    /// take a compile-time dependency on the dialogs.
    /// </summary>
    public static ActionFactory Factory { get; } = BuildFactory();

    public static IReadOnlyList<CheckResult> Run(ISelfCheck check, ITSEnv? env = null)
    {
        var scratch = Path.Combine(Path.GetTempPath(), "uisharp-checktest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);

        // A real log, not NullLog: the runtime always has one, and the checks
        // are entitled to report on where it went.
        var log = CMTraceLog.TryOpen(scratch, out _);

        try
        {
            var context = new SelfTestContext(env ?? new LocalTSEnv(), log, scratch, Factory);

            // Materialised inside the try so the scratch directory still exists
            // while the iterator runs.
            return check.Run(context).ToList();
        }
        finally
        {
            (log as IDisposable)?.Dispose();
            try { Directory.Delete(scratch, recursive: true); } catch { }
        }
    }

    public static void AssertNoFailures(IReadOnlyList<CheckResult> results)
    {
        var failures = results.Where(r => r.Outcome == CheckOutcome.Fail).ToList();

        Assert.True(failures.Count == 0,
            "The check reported failures on a healthy machine:" + Environment.NewLine +
            string.Join(Environment.NewLine,
                failures.Select(f => $"  [{f.Area}] {f.Name}: {f.Detail}")));
    }

    private static ActionFactory BuildFactory()
    {
        var factory = new ActionFactory();

        factory.RegisterFromAssembly(typeof(ActionBase).Assembly);
        factory.RegisterFromAssembly(typeof(Windows.Actions.ActionRegRead).Assembly);

        // UiSharp.UI is referenced by the test project purely so it is present
        // beside the test assembly; loading it by name keeps the dependency out
        // of the diagnostics themselves.
        try { factory.RegisterFromAssembly(Assembly.Load("UiSharp.UI")); }
        catch (Exception ex) { throw new InvalidOperationException(
            "UiSharp.UI could not be loaded, so the dialog action types are missing " +
            "from the factory under test.", ex); }

        return factory;
    }
}
