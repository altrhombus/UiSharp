using System.Xml.Linq;
using UiSharp.Core.Dialogs;
using UiSharp.Core.Scripting;
using UiSharp.Core.Variables;

namespace UiSharp.Core.Tests.Dialogs;

public class PreflightEvaluatorTests
{
    private static readonly IConditionEvaluator Cond = new NativeConditionEvaluator();
    private static readonly ITSEnv EmptyEnv = new LocalTSEnv();

    private static XElement ActionEl(string inner) =>
        XElement.Parse($"""<Action Type="Preflight">{inner}</Action>""");

    private static LocalTSEnv Env(params (string k, string v)[] vars)
    {
        var e = new LocalTSEnv();
        foreach (var (k, v) in vars) e.Set(k, v);
        return e;
    }

    // -------------------------------------------------------------------------
    // ParseChecks
    // -------------------------------------------------------------------------

    [Fact]
    public void ParseChecks_ReturnsAllChecks()
    {
        var el = ActionEl("""
            <Check Text="C1" CheckCondition="'A'='A'" />
            <Check Text="C2" CheckCondition="'B'='B'" />
            """);
        var checks = PreflightEvaluator.ParseChecks(el, EmptyEnv, Cond);
        Assert.Equal(2, checks.Count);
        Assert.Equal("C1", checks[0].Text);
        Assert.Equal("C2", checks[1].Text);
    }

    [Fact]
    public void ParseChecks_ConditionFalse_Excluded()
    {
        var el = ActionEl("""
            <Check Text="shown"  CheckCondition="'A'='A'" />
            <Check Text="hidden" CheckCondition="'A'='A'" Condition="'x'='y'" />
            """);
        var checks = PreflightEvaluator.ParseChecks(el, EmptyEnv, Cond);
        Assert.Single(checks);
        Assert.Equal("shown", checks[0].Text);
    }

    [Fact]
    public void ParseChecks_TextSubstituted()
    {
        var env = Env(("AppName", "MyApp"));
        var el  = ActionEl("""<Check Text="%AppName% installed?" CheckCondition="'A'='A'" />""");
        var checks = PreflightEvaluator.ParseChecks(el, env, Cond);
        Assert.Equal("MyApp installed?", checks[0].Text);
    }

    [Fact]
    public void ParseChecks_NonCheckElements_Ignored()
    {
        var el     = ActionEl("""<Other /><Check Text="C" CheckCondition="'A'='A'" />""");
        var checks = PreflightEvaluator.ParseChecks(el, EmptyEnv, Cond);
        Assert.Single(checks);
    }

    // -------------------------------------------------------------------------
    // Evaluate — Pass
    // -------------------------------------------------------------------------

    [Fact]
    public void Evaluate_TrueCheck_NoWarn_IsPass()
    {
        var check = new PreflightCheckSpec { Text = "T", CheckCondition = "'A'='A'" };
        var results = PreflightEvaluator.Evaluate([check], Cond, EmptyEnv);
        Assert.Equal(PreflightStatus.Pass, results[0].Status);
    }

    [Fact]
    public void Evaluate_EmptyCheckCondition_IsPass()
    {
        var check = new PreflightCheckSpec { Text = "T", CheckCondition = "" };
        var results = PreflightEvaluator.Evaluate([check], Cond, EmptyEnv);
        Assert.Equal(PreflightStatus.Pass, results[0].Status);
    }

    // -------------------------------------------------------------------------
    // Evaluate — Fail
    // -------------------------------------------------------------------------

    [Fact]
    public void Evaluate_FalseCheck_IsFail()
    {
        var check = new PreflightCheckSpec { Text = "T", CheckCondition = "'A'='B'" };
        var results = PreflightEvaluator.Evaluate([check], Cond, EmptyEnv);
        Assert.Equal(PreflightStatus.Fail, results[0].Status);
    }

    // -------------------------------------------------------------------------
    // Evaluate — Warn
    // -------------------------------------------------------------------------

    [Fact]
    public void Evaluate_CheckPasses_WarnFails_IsWarn()
    {
        var check = new PreflightCheckSpec
        {
            Text           = "T",
            CheckCondition = "'A'='A'",
            WarnCondition  = "'A'='B'",   // false → warn
        };
        var results = PreflightEvaluator.Evaluate([check], Cond, EmptyEnv);
        Assert.Equal(PreflightStatus.Warn, results[0].Status);
    }

    [Fact]
    public void Evaluate_CheckPasses_WarnPasses_IsPass()
    {
        var check = new PreflightCheckSpec
        {
            Text           = "T",
            CheckCondition = "'A'='A'",
            WarnCondition  = "'A'='A'",  // true → pass
        };
        var results = PreflightEvaluator.Evaluate([check], Cond, EmptyEnv);
        Assert.Equal(PreflightStatus.Pass, results[0].Status);
    }

    [Fact]
    public void Evaluate_CheckFails_WarnIgnored()
    {
        // Even if WarnCondition would pass, a failed CheckCondition = Fail, not Warn
        var check = new PreflightCheckSpec
        {
            Text           = "T",
            CheckCondition = "'A'='B'",
            WarnCondition  = "'A'='A'",
        };
        var results = PreflightEvaluator.Evaluate([check], Cond, EmptyEnv);
        Assert.Equal(PreflightStatus.Fail, results[0].Status);
    }

    // -------------------------------------------------------------------------
    // Evaluate — env substitution
    // -------------------------------------------------------------------------

    [Fact]
    public void Evaluate_ConditionUsesEnvVars()
    {
        var env   = Env(("DiskGB", "50"));
        var check = new PreflightCheckSpec
        {
            Text           = "Disk space",
            CheckCondition = "%DiskGB% >= 30",  // 50 >= 30 → true
        };
        var results = PreflightEvaluator.Evaluate([check], Cond, env);
        Assert.Equal(PreflightStatus.Pass, results[0].Status);
    }

    // -------------------------------------------------------------------------
    // ActiveDescription
    // -------------------------------------------------------------------------

    [Fact]
    public void ActiveDescription_Fail_UsesErrorDescription()
    {
        var check = new PreflightCheckSpec
        {
            Text             = "T",
            CheckCondition   = "'A'='B'",
            Description      = "Generic",
            ErrorDescription = "Specific error",
        };
        var result = new PreflightResult(check, PreflightStatus.Fail);
        Assert.Equal("Specific error", result.ActiveDescription);
    }

    [Fact]
    public void ActiveDescription_Fail_FallsBackToDescription()
    {
        var check  = new PreflightCheckSpec { Text = "T", CheckCondition = "", Description = "Desc" };
        var result = new PreflightResult(check, PreflightStatus.Fail);
        Assert.Equal("Desc", result.ActiveDescription);
    }

    [Fact]
    public void ActiveDescription_Warn_UsesWarnDescription()
    {
        var check = new PreflightCheckSpec
        {
            Text            = "T",
            CheckCondition  = "",
            WarnDescription = "Warn msg",
        };
        var result = new PreflightResult(check, PreflightStatus.Warn);
        Assert.Equal("Warn msg", result.ActiveDescription);
    }

    [Fact]
    public void ActiveDescription_Pass_UsesDescription()
    {
        var check  = new PreflightCheckSpec { Text = "T", CheckCondition = "", Description = "OK" };
        var result = new PreflightResult(check, PreflightStatus.Pass);
        Assert.Equal("OK", result.ActiveDescription);
    }

    // -------------------------------------------------------------------------
    // AnyFailed / AnyWarned helpers
    // -------------------------------------------------------------------------

    [Fact]
    public void AnyFailed_TrueWhenFail()
    {
        var check   = new PreflightCheckSpec { Text = "T", CheckCondition = "'A'='B'" };
        var results = PreflightEvaluator.Evaluate([check], Cond, EmptyEnv);
        Assert.True(PreflightEvaluator.AnyFailed(results));
    }

    [Fact]
    public void AnyFailed_FalseWhenAllPass()
    {
        var check   = new PreflightCheckSpec { Text = "T", CheckCondition = "'A'='A'" };
        var results = PreflightEvaluator.Evaluate([check], Cond, EmptyEnv);
        Assert.False(PreflightEvaluator.AnyFailed(results));
    }

    [Fact]
    public void AnyWarned_TrueWhenWarn()
    {
        var check = new PreflightCheckSpec
        {
            Text = "T", CheckCondition = "'A'='A'", WarnCondition = "'A'='B'",
        };
        var results = PreflightEvaluator.Evaluate([check], Cond, EmptyEnv);
        Assert.True(PreflightEvaluator.AnyWarned(results));
    }

    // -------------------------------------------------------------------------
    // Round-trip: Parse → Evaluate
    // -------------------------------------------------------------------------

    [Fact]
    public void RoundTrip_ParseThenEvaluate()
    {
        var env = Env(("OSBuild", "22621"));
        var el  = ActionEl("""
            <Check Text="OS build OK"
                   CheckCondition="%OSBuild% >= 19041"
                   WarnCondition="%OSBuild% >= 22000"
                   Description="OS version check"
                   ErrorDescription="OS too old" />
            """);
        var checks  = PreflightEvaluator.ParseChecks(el, env, Cond);
        var results = PreflightEvaluator.Evaluate(checks, Cond, env);
        Assert.Equal(PreflightStatus.Pass, results[0].Status);  // 22621 >= 22000
    }
}
