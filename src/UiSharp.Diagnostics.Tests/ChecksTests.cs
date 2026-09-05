using UiSharp.Core.Variables;
using UiSharp.Diagnostics;
using UiSharp.Diagnostics.Checks;

namespace UiSharp.Diagnostics.Tests;

/// <summary>
/// Each check, run here.
///
/// These are two tests in one. They prove the check itself works — that it can
/// run to completion and reports something — and they turn the check into a
/// regression test of the behaviour it describes, because a check reporting a
/// failure on a healthy machine means either the check or the runtime is wrong.
/// </summary>
public class ChecksTests
{
    [Fact]
    public void Environment_checks_pass()
    {
        var results = CheckHarness.Run(new EnvironmentChecks());

        CheckHarness.AssertNoFailures(results);

        // The log is the only thing a deployment leaves behind, so its location
        // must always be reported even when nothing failed.
        Assert.Contains(results, r => r.Name == "Log file in use");
    }

    [Fact]
    public void Task_sequence_checks_pass_against_the_local_environment()
    {
        var env     = new LocalTSEnv();
        var results = CheckHarness.Run(new TaskSequenceChecks(), env);

        CheckHarness.AssertNoFailures(results);
    }

    [Fact]
    public void Task_sequence_checks_leave_no_values_behind()
    {
        // A self-test run inside a deployment must not change what that
        // deployment then decides.
        var env = new LocalTSEnv();
        env.Set("Keep", "mine");

        CheckHarness.Run(new TaskSequenceChecks(), env);

        Assert.Equal("mine", env.Get("Keep"));
        Assert.All(env.GetAll().Where(kv => kv.Key.StartsWith("UiSharpSelfTest")),
            kv => Assert.Equal(string.Empty, kv.Value));
    }

    [Fact]
    public void Variable_file_checks_pass()
    {
        CheckHarness.AssertNoFailures(CheckHarness.Run(new VariableFileChecks()));
    }

    [Fact]
    public void Condition_engine_checks_pass()
    {
        CheckHarness.AssertNoFailures(CheckHarness.Run(new ConditionEngineChecks()));
    }

    [Fact]
    public void Action_pipeline_checks_pass()
    {
        var results = CheckHarness.Run(new ActionPipelineChecks());

        CheckHarness.AssertNoFailures(results);

        // The one that catches trimming. If it ever reports Skip or vanishes,
        // the self-test has stopped watching the thing it was built for.
        var discovery = Assert.Single(results, r => r.Name == "Every action type is discoverable");
        Assert.Equal(CheckOutcome.Pass, discovery.Outcome);
    }

    [Fact]
    public void Platform_checks_pass()
    {
        CheckHarness.AssertNoFailures(CheckHarness.Run(new PlatformChecks()));
    }

    [Fact]
    public void Every_result_carries_its_area_and_a_name()
    {
        foreach (var check in SelfTestRunner.Standard().Checks)
        {
            foreach (var result in CheckHarness.Run(check))
            {
                Assert.Equal(check.Area, result.Area);
                Assert.False(string.IsNullOrWhiteSpace(result.Name));
            }
        }
    }

    [Fact]
    public void Every_failure_explains_itself()
    {
        // A report that says only that something failed is not worth carrying
        // out of a WinPE session.
        foreach (var check in SelfTestRunner.Standard().Checks)
        {
            foreach (var result in CheckHarness.Run(check)
                         .Where(r => r.Outcome is CheckOutcome.Fail or CheckOutcome.Skip))
            {
                Assert.False(string.IsNullOrWhiteSpace(result.Detail),
                    $"[{result.Area}] {result.Name} reported {result.Outcome} with no detail");
            }
        }
    }
}
