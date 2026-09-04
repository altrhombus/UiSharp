using UIpp.Core.Scripting;
using UIpp.Windows.Scripting;
using Xunit.Abstractions;

namespace UIpp.Windows.Tests.Scripting;

/// <summary>
/// Discovery harness: runs the whole corpus through both engines and reports
/// every disagreement. Not an assertion of correctness — it exists so the
/// differences can be enumerated deliberately rather than guessed at.
///
/// Set UIPP_ENGINE_REPORT=1 to make it fail with the full report attached.
/// </summary>
public class EngineDifferenceReportTests(ITestOutputHelper output)
{
    [Fact]
    public void ReportDifferences()
    {
        if (!VBScriptConditionEvaluator.IsAvailable)
        {
            output.WriteLine("VBScript engine unavailable; nothing to compare.");
            return;
        }

        var native = new NativeConditionEvaluator();
        var lines  = new List<string>();

        var all = EngineCorpus.Deterministic
            .Concat(EngineCorpus.ComCompatibility)
            .Concat(EngineCorpus.NotExpressions)
            .Concat(EngineCorpus.RuntimeErrors)
            .ToArray();

        foreach (var expr in all)
        {
            var nativeCond = native.Evaluate(expr, EngineComparison.NoVars);
            var vbCond     = EngineComparison.OnStaThread(
                () => new VBScriptConditionEvaluator().Evaluate(expr, EngineComparison.NoVars));

            var nativeVal = native.TryEvaluateValue(expr, out var nv) ? nv : null;
            var vbVal     = EngineComparison.OnStaThread(() =>
                new VBScriptConditionEvaluator().TryEvaluateValue(expr, out var v) ? v : null);

            if (nativeCond != vbCond)
                lines.Add($"COND  {expr,-52} native={nativeCond,-5} vbscript={vbCond}");

            if (nativeVal != vbVal)
                lines.Add($"VALUE {expr,-52} native={Show(nativeVal),-24} vbscript={Show(vbVal)}");
        }

        output.WriteLine($"Compared {all.Length} expressions; {lines.Count} disagreements.");
        foreach (var l in lines) output.WriteLine(l);

        if (Environment.GetEnvironmentVariable("UIPP_ENGINE_REPORT") == "1")
            Assert.Fail($"{lines.Count} disagreements:\n" + string.Join("\n", lines));
    }

    private static string Show(string? v) => v is null ? "(declined)" : $"\"{v}\"";
}
