using UiSharp.Diagnostics;

namespace UiSharp.Diagnostics.Tests;

public class SelfTestReportTests
{
    private static SelfTestReport Report(bool inTaskSequence, params CheckResult[] results) =>
        new(results, TimeSpan.FromSeconds(2), inTaskSequence);

    [Fact]
    public void Summary_counts_each_outcome()
    {
        var report = Report(true,
            CheckResult.Pass("A", "one"),
            CheckResult.Pass("A", "two"),
            CheckResult.Fail("A", "three", "because"),
            CheckResult.Skip("A", "four", "not here"),
            CheckResult.Info("A", "five", "a fact"));

        Assert.Equal(2, report.Passed);
        Assert.Equal(1, report.Failed);
        Assert.Equal(1, report.Skipped);
        Assert.False(report.AllPassed);
        Assert.Contains("2 passed, 1 failed, 1 skipped", report.Summary);
    }

    [Fact]
    public void Info_results_are_not_counted_as_passes()
    {
        // An Info line records a fact about the machine. Counting it as a pass
        // would inflate the number a reader uses to decide whether to trust the
        // run.
        var report = Report(true, CheckResult.Info("A", "Temp directory", @"C:\Temp"));

        Assert.Equal(0, report.Passed);
        Assert.True(report.AllPassed);
    }

    [Fact]
    public void Failures_are_listed_before_everything_else_with_their_detail()
    {
        var text = Report(true,
            CheckResult.Pass("A", "a passing check"),
            CheckResult.Fail("B", "a failing check", "expected 1, got 2")).ToText();

        var failuresAt = text.IndexOf("FAILURES", StringComparison.Ordinal);
        var allAt      = text.IndexOf("ALL CHECKS", StringComparison.Ordinal);

        Assert.True(failuresAt >= 0);
        Assert.True(failuresAt < allAt);
        Assert.Contains("expected 1, got 2", text);
    }

    [Fact]
    public void A_clean_report_has_no_failures_section()
    {
        var text = Report(true, CheckResult.Pass("A", "fine")).ToText();

        Assert.DoesNotContain("FAILURES", text);
    }

    [Fact]
    public void The_header_says_when_the_run_was_not_inside_a_task_sequence()
    {
        // The whole point of the instrument is what it proves about a real
        // deployment; a run outside one must not read as the same evidence.
        Assert.Contains("NOT running inside a task sequence",
            Report(false, CheckResult.Pass("A", "fine")).ToText());

        Assert.Contains("Running inside a task sequence.",
            Report(true, CheckResult.Pass("A", "fine")).ToText());
    }

    [Fact]
    public void Info_detail_stays_on_one_line()
    {
        // Detail can carry a value read off the machine, and a stray newline
        // would make the rest of it look like a separate check.
        var text = Report(true, CheckResult.Info("A", "Value", "first\nsecond")).ToText();

        Assert.Contains("info  Value: first second", text);
    }

    [Fact]
    public void Areas_keep_their_order_and_group_their_checks()
    {
        var text = Report(true,
            CheckResult.Pass("First",  "a"),
            CheckResult.Pass("Second", "b"),
            CheckResult.Pass("First",  "c")).ToText();

        var first  = text.IndexOf("  First", StringComparison.Ordinal);
        var second = text.IndexOf("  Second", StringComparison.Ordinal);
        var c      = text.IndexOf("  c", StringComparison.Ordinal);

        Assert.True(first < second);
        Assert.True(c < second); // grouped under First, not repeated later
    }
}
