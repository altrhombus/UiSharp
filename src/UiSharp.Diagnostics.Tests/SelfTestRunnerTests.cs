using UiSharp.Core.Actions;
using UiSharp.Core.Logging;
using UiSharp.Core.Variables;
using UiSharp.Diagnostics;

namespace UiSharp.Diagnostics.Tests;

public class SelfTestRunnerTests
{
    private sealed class StubCheck(string area, Func<SelfTestContext, IEnumerable<CheckResult>> body)
        : ISelfCheck
    {
        public string Area => area;
        public IEnumerable<CheckResult> Run(SelfTestContext context) => body(context);
    }

    private static SelfTestReport Run(params ISelfCheck[] checks) =>
        new SelfTestRunner(checks).Run(new LocalTSEnv(), NullLog.Instance, new ActionFactory());

    [Fact]
    public void Results_from_every_check_reach_the_report()
    {
        var report = Run(
            new StubCheck("A", _ => [CheckResult.Pass("A", "one")]),
            new StubCheck("B", _ => [CheckResult.Pass("B", "two"), CheckResult.Fail("B", "three", "no")]));

        Assert.Equal(3, report.Results.Count);
        Assert.Equal(2, report.Passed);
        Assert.Equal(1, report.Failed);
    }

    [Fact]
    public void A_check_that_throws_becomes_a_failure_and_the_rest_still_run()
    {
        // A self-test that dies half way through tells you less than one that
        // reports what broke, so the runner must not let an exception escape.
        var report = Run(
            new StubCheck("Throwing", _ => throw new InvalidOperationException("boom")),
            new StubCheck("Later",    _ => [CheckResult.Pass("Later", "still ran")]));

        var failure = Assert.Single(report.Results, r => r.Outcome == CheckOutcome.Fail);

        Assert.Equal("Throwing", failure.Area);
        Assert.Contains("boom", failure.Detail);
        Assert.Contains(report.Results, r => r.Area == "Later" && r.Outcome == CheckOutcome.Pass);
    }

    [Fact]
    public void A_check_that_throws_part_way_through_keeps_what_it_already_reported()
    {
        // The checks are iterators: results produced before the throw are worth
        // keeping, because they are usually the context for the failure.
        var report = Run(new StubCheck("A", Partial));

        Assert.Equal(1, report.Passed);
        Assert.Equal(1, report.Failed);

        static IEnumerable<CheckResult> Partial(SelfTestContext _)
        {
            yield return CheckResult.Pass("A", "got this far");
            throw new InvalidOperationException("then stopped");
        }
    }

    [Fact]
    public void The_scratch_directory_exists_while_checks_run_and_is_gone_afterwards()
    {
        string? scratch = null;

        Run(new StubCheck("A", context =>
        {
            scratch = context.ScratchDirectory;
            Assert.True(Directory.Exists(scratch));
            File.WriteAllText(Path.Combine(scratch, "left-behind.txt"), "x");
            return [CheckResult.Pass("A", "wrote a file")];
        }));

        Assert.NotNull(scratch);
        Assert.False(Directory.Exists(scratch));
    }

    [Fact]
    public void The_report_records_whether_the_run_was_inside_a_task_sequence()
    {
        Assert.False(Run(new StubCheck("A", _ => [])).InTaskSequence);
    }

    [Fact]
    public void Every_area_in_the_standard_set_is_named_once()
    {
        // Two checks sharing an area name would interleave in the report, which
        // reads as one area contradicting itself.
        var checks = SelfTestRunner.Standard().Checks;

        Assert.NotEmpty(checks);
        Assert.Equal(checks.Select(c => c.Area).Distinct().Count(), checks.Count);
        Assert.All(checks, c => Assert.False(string.IsNullOrWhiteSpace(c.Area)));
    }
}
