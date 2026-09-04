using UiSharp.Core.Scripting;

namespace UiSharp.Core.Tests.Scripting;

/// <summary>
/// The native engine substitutes an empty string for anything it cannot evaluate,
/// and an empty string is falsy — so without diagnostics an unsupported construct
/// is indistinguishable from a genuinely false condition. These tests pin down
/// which constructs are reported, and (just as importantly) that supported ones
/// are not.
/// </summary>
public class NativeConditionDiagnosticsTests
{
    private readonly NativeConditionEvaluator _eval = new();
    private readonly IReadOnlyDictionary<string, string> _empty =
        new Dictionary<string, string>();

    private ConditionResult Run(string expr) => _eval.TryEvaluate(expr, _empty);

    // ----------------------------------------------------------
    // No false positives — everything the engine genuinely supports
    // must come back clean, or the signal is worthless.
    // ----------------------------------------------------------

    [Theory]
    // Comparisons
    [InlineData("'LAPTOP' = 'LAPTOP'")]
    [InlineData("'LAPTOP' <> 'DESKTOP'")]
    [InlineData("'A' < 'B'")]
    [InlineData("'A' <= 'A'")]
    [InlineData("'B' >= 'A'")]
    [InlineData("1 = 1")]
    [InlineData("2 > 1")]
    [InlineData("-5 < 0")]
    [InlineData("3.14 > 3")]
    // Boolean operators
    [InlineData("'A' = 'A' AND '1' = '1'")]
    [InlineData("'A' = 'B' OR '1' = '1'")]
    [InlineData("NOT '1' = '2'")]
    [InlineData("NOT NOT '1' = '1'")]
    [InlineData("('A' = 'A' OR 'B' = 'C') AND NOT 1 = 2")]
    // Mod
    [InlineData("10 Mod 3 = 1")]
    // String built-ins
    [InlineData("InStr('SRV-001', 'SRV') > 0")]
    [InlineData("InStrRev('a/b/c', '/') = 4")]
    [InlineData("UCase('abc') = 'ABC'")]
    [InlineData("LCase('ABC') = 'abc'")]
    [InlineData("Len('abcd') = 4")]
    [InlineData("Mid('abcdef', 2, 3) = 'bcd'")]
    [InlineData("Left('abcdef', 2) = 'ab'")]
    [InlineData("Right('abcdef', 2) = 'ef'")]
    [InlineData("Trim('  ab  ') = 'ab'")]
    [InlineData("LTrim('  ab') = 'ab'")]
    [InlineData("RTrim('ab  ') = 'ab'")]
    [InlineData("Replace('a-b', '-', '_') = 'a_b'")]
    // Type / numeric built-ins
    [InlineData("IsNumeric('42')")]
    [InlineData("IsNull('x') = False")]
    [InlineData("IsEmpty('')")]
    [InlineData("Str(42) = '42'")]
    [InlineData("Int(3.9) = 3")]
    [InlineData("Abs(-3) = 3")]
    [InlineData("CBool('x')")]
    [InlineData("CInt('3.6') = 4")]
    [InlineData("CDbl('3.5') = 3.5")]
    // Date built-ins
    [InlineData("Year('1/2/2020') = 2020")]
    [InlineData("Month('1/2/2020') = 1")]
    [InlineData("Day('1/2/2020') = 2")]
    [InlineData("Len(Now()) > 0")]
    [InlineData("Len(Date()) > 0")]
    [InlineData("Len(Time()) > 0")]
    // Nested calls
    [InlineData("UCase(Left('abcdef', 3)) = 'ABC'")]
    [InlineData("InStr(UCase('srv-01'), 'SRV') = 1")]
    // Empty / whitespace conditions are treated as "no condition"
    [InlineData("")]
    [InlineData("   ")]
    public void SupportedExpressions_ReportNothing(string expr)
    {
        var result = Run(expr);
        Assert.True(result.IsReliable,
            $"expected no diagnostics for \"{expr}\" but got: {result.Describe()}");
    }

    // Real conditions taken verbatim from the original project's own sample
    // configs, after variable substitution.
    [Theory]
    [InlineData("\"LAPTOP\" = \"\"")]
    [InlineData("\"False\" = \"False\"")]
    [InlineData("2048 >= 1024")]
    [InlineData("True AND True AND True = True")]
    [InlineData("\"True\" = \"True\"")]
    public void OriginalSampleConditions_ReportNothing(string expr)
    {
        var result = Run(expr);
        Assert.True(result.IsReliable,
            $"expected no diagnostics for \"{expr}\" but got: {result.Describe()}");
    }

    // ----------------------------------------------------------
    // COM / VBScript-only constructs
    // ----------------------------------------------------------

    [Theory]
    // A ProgID the compatibility shim has no equivalent for.
    [InlineData("CreateObject('Scripting.Dictionary')")]
    // Constructs with no native equivalent at all.
    [InlineData("GetObject('winmgmts:')")]
    [InlineData("Eval('1 = 1')")]
    [InlineData("Execute('x = 1')")]
    public void ComConstructs_ReportRequiresComHost(string expr)
    {
        var result = Run(expr);
        Assert.Contains(result.Problems,
            d => d.Kind == ConditionDiagnosticKind.RequiresComHost);
    }

    // The condition that actually appears in UI++/UI++5.xml. It is handled by the
    // COM compatibility shim rather than needing a script host, so the config
    // runs unchanged in a WinPE image without WinPE-Scripting.
    [Fact]
    public void FileSystemObjectCondition_FromSampleConfig_EvaluatesThroughTheShim()
    {
        var result = Run(
            "CreateObject(\"Scripting.FileSystemObject\").FileExists(\"C:\\Windows\")");

        // Nothing blocks evaluation any more — the compatibility shim handles it...
        Assert.True(result.IsReliable, result.DescribeProblems());

        // ...and the advisory names the native replacement to migrate to.
        Assert.Contains(result.Advice,
            d => d.Kind == ConditionDiagnosticKind.ComCompatibilityShim);
    }

    // ----------------------------------------------------------
    // Unknown functions — typos and unreplicated built-ins
    // ----------------------------------------------------------

    [Theory]
    [InlineData("UCse('abc') = 'ABC'")]              // typo for UCase
    [InlineData("SomeFunctionThatDoesNotExist('x')")]
    [InlineData("FormatNumber(1234) = '1,234'")]     // real VBScript built-in, not replicated
    public void UnknownFunctions_AreReported(string expr)
    {
        var result = Run(expr);
        Assert.Contains(result.Diagnostics,
            d => d.Kind == ConditionDiagnosticKind.UnknownFunction);
    }

    [Fact]
    public void UnknownFunction_DiagnosticNamesTheFunction()
    {
        var result = Run("UCse('abc') = 'ABC'");
        Assert.Contains(result.Diagnostics, d => d.Detail.Contains("UCse"));
    }

    // ----------------------------------------------------------
    // Constructs the engine recognises but cannot represent
    // ----------------------------------------------------------

    [Fact]
    public void Split_IsReportedAsUnsupported()
    {
        var result = Run("Split('a,b', ',') <> ''");
        Assert.Contains(result.Diagnostics,
            d => d.Kind == ConditionDiagnosticKind.UnsupportedConstruct);
    }

    // Member access works now, but only on an object a supported CreateObject
    // produced — not on a plain string.
    [Fact]
    public void MemberAccess_OnANonObject_IsReported()
    {
        var result = Run("'abc'.Length = 3");

        Assert.False(result.Value);
        Assert.Contains(result.Problems,
            d => d.Kind == ConditionDiagnosticKind.UnsupportedConstruct &&
                 d.Detail.Contains("requires an object"));
    }

    // '&' concatenation is now supported, so it must NOT be reported.
    [Fact]
    public void Ampersand_Concatenation_IsSupported()
    {
        var result = Run("'a' & 'b' = 'ab'");
        Assert.True(result.Value, result.Describe());
        Assert.True(result.IsReliable, result.Describe());
    }

    // ----------------------------------------------------------
    // Partly-consumed expressions
    // ----------------------------------------------------------

    [Fact]
    public void UnconsumedInput_IsReported()
    {
        var result = Run("1 = 1 garbage 2 = 2");
        Assert.Contains(result.Diagnostics,
            d => d.Kind == ConditionDiagnosticKind.TrailingInput);
    }

    [Fact]
    public void TrailingInput_DiagnosticShowsWhereItStopped()
    {
        var result = Run("1 = 1 garbage");
        var diag = Assert.Single(result.Diagnostics,
            d => d.Kind == ConditionDiagnosticKind.TrailingInput);
        Assert.Contains("garbage", diag.Detail);
    }

    // ----------------------------------------------------------
    // Value behaviour is unchanged by the diagnostics work
    // ----------------------------------------------------------

    [Theory]
    [InlineData("1 = 1", true)]
    [InlineData("1 = 2", false)]
    [InlineData("CreateObject('X')", false)]
    [InlineData("UCse('abc') = 'ABC'", false)]
    // Diagnostics now fail the condition closed, matching VBScript raising an
    // error and C++ treating that as false.
    [InlineData("'abc'.Length = 3", false)]
    public void Evaluate_AgreesWithTryEvaluateValue(string expr, bool expected)
    {
        Assert.Equal(expected, _eval.Evaluate(expr, _empty));
        Assert.Equal(expected, Run(expr).Value);
    }

    // ----------------------------------------------------------
    // Conditions fail closed
    // ----------------------------------------------------------

    // VBScript raises an error for anything it cannot evaluate, and C++
    // EvalCondition treats a failed Eval as false (ActionHelper.cpp:89). The
    // native engine must do the same rather than returning whatever its parse
    // left behind.
    [Theory]
    // An unresolved variable leaves %Token% in place; the bare '%' used to
    // parse as a truthy string, so preflight checks on missing data PASSED.
    [InlineData("%XHWMemory% >= 1024")]
    [InlineData("%XCPUPAE% AND %XCPUNX% AND %XCPUSSE2% = True")]
    // Free text is not an expression at all.
    [InlineData("Adobe Reader DC 2019")]
    [InlineData("Please choose a volume")]
    // Constructs the engine cannot honour.
    [InlineData("GetObject('winmgmts:') = 1")]
    [InlineData("FormatNumber(1234) = '1,234'")]
    [InlineData("'abc'.Length = 3")]
    [InlineData("1 / 0 = 0")]
    public void UnevaluatableCondition_IsFalseAndReported(string expr)
    {
        var result = Run(expr);

        Assert.False(result.Value, $"expected false for \"{expr}\"");
        Assert.False(result.IsReliable, $"expected a diagnostic for \"{expr}\"");
    }

    // True/False are VBScript keywords, not bare identifiers. Before this was
    // handled, "False" parsed as the non-empty string "False" and was truthy.
    [Theory]
    [InlineData("True", true)]
    [InlineData("False", false)]
    [InlineData("True AND False", false)]
    [InlineData("True AND True", true)]
    [InlineData("True OR False", true)]
    [InlineData("NOT False", true)]
    [InlineData("NOT True", false)]
    public void BooleanKeywords_AreLiterals(string expr, bool expected)
    {
        var result = Run(expr);

        Assert.Equal(expected, result.Value);
        Assert.True(result.IsReliable, result.Describe());
    }

    // ----------------------------------------------------------
    // Default interface implementation — engines that do not opt in
    // report nothing rather than failing to compile or throwing.
    // ----------------------------------------------------------

    private sealed class AlwaysTrueEvaluator : IConditionEvaluator
    {
        public bool Evaluate(string expression, IReadOnlyDictionary<string, string> variables) => true;
    }

    [Fact]
    public void EngineWithoutDiagnosticSupport_ReportsNothing()
    {
        IConditionEvaluator engine = new AlwaysTrueEvaluator();
        var result = engine.TryEvaluate("anything at all", _empty);

        Assert.True(result.Value);
        Assert.True(result.IsReliable);
    }
}
