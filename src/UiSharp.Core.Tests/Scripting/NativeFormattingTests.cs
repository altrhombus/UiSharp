using UiSharp.Core.Scripting;

namespace UiSharp.Core.Tests.Scripting;

/// <summary>
/// The parts of the VBScript surface that cannot be matched exactly, and the
/// functions that are refused on purpose.
///
/// The runtime publishes with InvariantGlobalization so a WinPE image carries no
/// ICU data and behaviour does not vary by machine — the right trade for a
/// deployment tool. VBScript instead reads the system's regional settings,
/// including Windows short-date overrides that .NET ignores regardless. So
/// anything rendered per locale is deterministic here rather than identical to
/// VBScript, and the differential suite excludes exactly those cases.
/// </summary>
public class NativeFormattingTests
{
    private readonly NativeConditionEvaluator _eval = new();
    private readonly IReadOnlyDictionary<string, string> _empty = new Dictionary<string, string>();

    private string? Value(string expr) => _eval.TryEvaluateValue(expr, out var v) ? v : null;
    private ConditionResult Run(string expr) => _eval.TryEvaluate(expr, _empty);

    // -------------------------------------------------------------------------
    // Deterministic formatting
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("FormatNumber(1234.5678)", "1,234.57")]
    [InlineData("FormatNumber(1234.5678, 1)", "1,234.6")]
    [InlineData("FormatNumber(1234.5678, 0)", "1,235")]
    [InlineData("FormatNumber(0.5)", "0.50")]
    public void FormatNumber_IsInvariantAndGrouped(string expr, string expected) =>
        Assert.Equal(expected, Value(expr));

    [Theory]
    [InlineData("FormatPercent(0.125)", "12.50%")]
    [InlineData("FormatPercent(0.125, 1)", "12.5%")]
    [InlineData("FormatPercent(1)", "100.00%")]
    public void FormatPercent_HasNoSpaceBeforeTheSign(string expr, string expected) =>
        // Built by hand: the invariant "P" format inserts a space, VBScript does not.
        Assert.Equal(expected, Value(expr));

    [Theory]
    [InlineData(@"FormatDateTime(""3/4/2021"", 2)", "03/04/2021")]
    [InlineData(@"FormatDateTime(""3/4/2021 13:45:12"", 4)", "13:45")]
    public void FormatDateTime_UsesInvariantPatterns(string expr, string expected) =>
        // Deterministic rather than locale-following. A machine whose short-date
        // format is customised will see VBScript render these differently.
        Assert.Equal(expected, Value(expr));

    // -------------------------------------------------------------------------
    // Refused on purpose
    // -------------------------------------------------------------------------

    [Fact]
    public void FormatCurrency_IsRefusedRatherThanGuessed()
    {
        // The symbol and its placement ARE the locale; a dollar sign would be
        // wrong everywhere else, and the invariant sign is useless to anyone.
        var result = Run("FormatCurrency(1234.5) <> \"\"");

        Assert.False(result.IsReliable);
        Assert.Contains(result.Problems,
            d => d.Detail.Contains("FormatCurrency") && d.Detail.Contains("FormatNumber"));
    }

    // A modal dialog in an unattended task sequence does not give a wrong
    // answer, it stops the deployment until somebody notices.
    [Theory]
    [InlineData(@"InputBox(""Name?"")")]
    [InlineData(@"MsgBox(""Hello"")")]
    [InlineData(@"LoadPicture(""x.bmp"")")]
    public void InteractiveFunctions_AreRefusedWithTheReason(string expr)
    {
        var result = Run(expr);

        Assert.False(result.Value);
        Assert.Contains(result.Problems,
            d => d.Detail.Contains("waits for user interaction"));
    }

    [Theory]
    [InlineData(@"GetRef(""f"")")]
    [InlineData("GetLocale()")]
    [InlineData("SetLocale(1033)")]
    [InlineData("ScriptEngine()")]
    [InlineData("ScriptEngineMajorVersion()")]
    public void ScriptHostIntrospection_IsReportedNotGuessed(string expr)
    {
        // Reporting the wrong engine name, or a version for an engine that is
        // not running, would be worse than declining.
        var result = Run(expr);

        Assert.False(result.Value);
        Assert.Contains(result.Problems,
            d => d.Kind == ConditionDiagnosticKind.RequiresComHost);
    }

    // -------------------------------------------------------------------------
    // Non-deterministic by nature
    // -------------------------------------------------------------------------

    [Fact]
    public void Rnd_ProducesAValueInRangeButCannotBeCompared()
    {
        // Present for completeness; no two engines can agree on it, so it is
        // absent from the differential corpus.
        for (var i = 0; i < 20; i++)
        {
            var value = Value("Rnd()");
            Assert.NotNull(value);
            Assert.InRange(double.Parse(value!, System.Globalization.CultureInfo.InvariantCulture), 0.0, 1.0);
        }
    }

    [Fact]
    public void Timer_IsSecondsSinceMidnight()
    {
        var value = Value("Timer()");

        Assert.NotNull(value);
        Assert.InRange(
            double.Parse(value!, System.Globalization.CultureInfo.InvariantCulture),
            0.0, 86400.0);
    }

    // -------------------------------------------------------------------------
    // Everything documented is now either implemented or reported
    // -------------------------------------------------------------------------

    [Fact]
    public void NoDocumentedFunctionIsSilentlyUnknown()
    {
        // An unimplemented function returns an empty string, which is falsy —
        // so the only protection against a silently wrong condition is that it
        // is always reported. Nothing here may pass unreported.
        string[] everyVbScriptFunction =
        [
            @"Abs(-1)", @"Array(1)", @"Asc(""A"")", @"Atn(0)", @"CBool(1)", @"CByte(1)",
            @"CCur(1)", @"CDate(""1/1/2020"")", @"CDbl(""1"")", @"Chr(65)", @"CInt(""1"")",
            @"CLng(1)", @"Cos(0)", @"CSng(1)", @"CStr(1)", @"Date()",
            @"DateAdd(""d"",1,""1/1/2020"")", @"DateDiff(""d"",""1/1/2020"",""1/2/2020"")",
            @"DatePart(""d"",""1/1/2020"")", @"DateSerial(2020,1,1)", @"DateValue(""1/1/2020"")",
            @"Day(""1/1/2020"")", @"Exp(0)", @"Filter(Array(""a""),""a"")", @"Fix(1.5)",
            @"FormatDateTime(""1/1/2020"")", @"FormatNumber(1)", @"FormatPercent(1)",
            @"Hex(255)", @"Hour(""1/1/2020 1:00:00"")", @"InStr(""ab"",""b"")",
            @"InStrRev(""ab"",""b"")", @"Int(1.5)", @"IsArray(Array(1))",
            @"IsDate(""1/1/2020"")", @"IsEmpty("""")", @"IsNull(""x"")", @"IsNumeric(""1"")",
            @"IsObject(""x"")", @"Join(Array(""a""))", @"LBound(Array(1))", @"LCase(""A"")",
            @"Left(""ab"",1)", @"Len(""ab"")", @"Log(1)", @"LTrim("" a"")", @"Mid(""abc"",2,1)",
            @"Minute(""1/1/2020 1:02:00"")", @"Month(""1/1/2020"")", @"MonthName(1)", @"Now()",
            @"Oct(8)", @"Replace(""ab"",""a"",""c"")", @"RGB(1,2,3)", @"Right(""ab"",1)",
            @"Rnd()", @"Round(1.5)", @"RTrim(""a "")", @"Second(""1/1/2020 1:02:03"")",
            @"Sgn(-1)", @"Sin(0)", @"Space(1)", @"Split(""a,b"","","")", @"Sqr(4)",
            @"StrComp(""a"",""b"")", @"String(1,""x"")", @"StrReverse(""ab"")", @"Tan(0)",
            @"Time()", @"Timer()", @"TimeSerial(1,2,3)", @"TimeValue(""1/1/2020 1:02:03"")",
            @"Trim("" a "")", @"TypeName(1)", @"UBound(Array(1))", @"UCase(""a"")",
            @"VarType(1)", @"Weekday(""1/1/2020"")", @"WeekdayName(1)", @"Year(""1/1/2020"")",
        ];

        var unreported = everyVbScriptFunction
            .Where(expr => Run(expr).Diagnostics
                .Any(d => d.Kind == ConditionDiagnosticKind.UnknownFunction))
            .ToList();

        Assert.Empty(unreported);
    }
}
