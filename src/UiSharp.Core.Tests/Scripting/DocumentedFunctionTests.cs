using UiSharp.Core.Scripting;

namespace UiSharp.Core.Tests.Scripting;

/// <summary>
/// The functions the UI++ documentation promises.
///
/// The prerequisites page points config authors at the full VBScript function
/// reference and lists eight as "often used with UI++": InStr, Left, Len, Mid,
/// Replace, Split, StrComp and Trim. Two of those did not work — Split was
/// rejected outright and StrComp was absent — which no amount of reading the
/// sample configs would have shown, because none of them happens to use either.
///
/// Behaviour here is verified against the real engine by the differential suite
/// in UiSharp.Windows.Tests; these tests pin it cross-platform.
/// </summary>
public class DocumentedFunctionTests
{
    private readonly NativeConditionEvaluator _eval = new();
    private readonly IReadOnlyDictionary<string, string> _empty = new Dictionary<string, string>();

    private string? Value(string expr) => _eval.TryEvaluateValue(expr, out var v) ? v : null;
    private ConditionResult Run(string expr) => _eval.TryEvaluate(expr, _empty);

    // -------------------------------------------------------------------------
    // StrComp
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(@"StrComp(""a"", ""b"")", "-1")]
    [InlineData(@"StrComp(""b"", ""a"")", "1")]
    [InlineData(@"StrComp(""a"", ""a"")", "0")]
    // Binary by default, so case differs...
    [InlineData(@"StrComp(""a"", ""A"")", "1")]
    // ...unless text comparison is asked for.
    [InlineData(@"StrComp(""a"", ""A"", 1)", "0")]
    public void StrComp_ReturnsTheSignOfTheComparison(string expr, string expected) =>
        Assert.Equal(expected, Value(expr));

    // -------------------------------------------------------------------------
    // Split and the array family
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(@"Split(""a,b,c"", "","")(0)", "a")]
    [InlineData(@"Split(""a,b,c"", "","")(2)", "c")]
    [InlineData(@"Split(""one::two"", ""::"")(1)", "two")]
    public void Split_SplitsOnTheGivenDelimiter(string expr, string expected) =>
        Assert.Equal(expected, Value(expr));

    [Fact]
    public void Split_DefaultsToASpaceDelimiter() =>
        Assert.Equal("b", Value(@"Split(""a b c"")(1)"));

    [Fact]
    public void Split_HonoursALimit()
    {
        // The final element keeps the unsplit remainder.
        Assert.Equal("b,c", Value(@"Split(""a,b,c"", "","", 2)(1)"));
        Assert.Equal("1", Value(@"UBound(Split(""a,b,c"", "","", 2))"));
    }

    [Fact]
    public void Split_WithNoMatch_YieldsTheWholeString()
    {
        Assert.Equal("abc", Value(@"Split(""abc"", "","")(0)"));
        Assert.Equal("0", Value(@"UBound(Split(""abc"", "",""))"));
    }

    [Fact]
    public void Split_OfEmptyText_YieldsOneEmptyElement() =>
        Assert.Equal("0", Value(@"UBound(Split("""", "",""))"));

    [Theory]
    [InlineData(@"UBound(Split(""a,b,c"", "",""))", "2")]
    [InlineData(@"LBound(Split(""a,b,c"", "",""))", "0")]
    public void Bounds_AreZeroBased(string expr, string expected) =>
        Assert.Equal(expected, Value(expr));

    [Theory]
    [InlineData(@"Join(Split(""a,b,c"", "",""), ""-"")", "a-b-c")]
    [InlineData(@"Join(Split(""a,b,c"", "",""))", "a b c")]
    public void Join_RejoinsWithTheGivenDelimiter(string expr, string expected) =>
        Assert.Equal(expected, Value(expr));

    [Theory]
    [InlineData(@"Join(Filter(Split(""ab,cd,ce"", "",""), ""c""), ""|"")", "cd|ce")]
    [InlineData(@"Join(Filter(Split(""ab,cd,ce"", "",""), ""c"", False), ""|"")", "ab")]
    public void Filter_KeepsOrExcludesMatchingElements(string expr, string expected) =>
        Assert.Equal(expected, Value(expr));

    [Theory]
    [InlineData(@"IsArray(Split(""a,b"", "",""))", true)]
    [InlineData(@"IsArray(""a,b"")", false)]
    [InlineData("IsArray(42)", false)]
    public void IsArray_DistinguishesArraysFromScalars(string expr, bool expected) =>
        Assert.Equal(expected, Run(expr).Value);

    // -------------------------------------------------------------------------
    // Arrays are not scalars
    // -------------------------------------------------------------------------

    [Fact]
    public void AnArrayIsNotAValue()
    {
        // Nothing useful can be written to a task-sequence variable, and
        // VBScript raises a type mismatch rather than stringifying it.
        Assert.Null(Value(@"Split(""a,b"", "","")"));
    }

    [Fact]
    public void AnArrayIsNotATruthValue() =>
        Assert.False(Run(@"Split(""a,b"", "","")").Value);

    [Theory]
    [InlineData(@"Split(""a,b"", "","")(9)")]
    [InlineData(@"Split(""a,b"", "","")(-1)")]
    public void AnOutOfRangeIndex_IsReported(string expr)
    {
        var result = Run(expr);

        Assert.False(result.IsReliable);
        Assert.Contains(result.Problems, d => d.Kind == ConditionDiagnosticKind.EvaluationError);
    }

    // -------------------------------------------------------------------------
    // Dates
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(@"DateAdd(""d"", 1, ""1/2/2020"")", "1/3/2020")]
    [InlineData(@"DateAdd(""d"", -1, ""1/2/2020"")", "1/1/2020")]
    [InlineData(@"DateAdd(""m"", 2, ""1/2/2020"")", "3/2/2020")]
    [InlineData(@"DateAdd(""yyyy"", 1, ""1/2/2020"")", "1/2/2021")]
    [InlineData(@"DateAdd(""ww"", 1, ""1/2/2020"")", "1/9/2020")]
    public void DateAdd_ShiftsByTheGivenInterval(string expr, string expected) =>
        Assert.Equal(expected, Value(expr));

    // The result must be parseable by the other date functions, or it cannot be
    // composed — which is the whole point of returning a date.
    [Fact]
    public void DateAdd_RoundTripsThroughTheOtherDateFunctions()
    {
        Assert.Equal("2025", Value(@"Year(DateAdd(""yyyy"", 5, ""1/2/2020""))"));
        Assert.Equal("2", Value(@"Month(DateAdd(""m"", 13, ""1/2/2020""))"));
    }

    [Theory]
    [InlineData(@"DateDiff(""d"", ""1/1/2020"", ""1/31/2020"")", "30")]
    [InlineData(@"DateDiff(""m"", ""1/1/2020"", ""6/1/2020"")", "5")]
    [InlineData(@"DateDiff(""yyyy"", ""1/1/2020"", ""1/1/2024"")", "4")]
    [InlineData(@"DateDiff(""ww"", ""1/1/2020"", ""1/29/2020"")", "4")]
    public void DateDiff_MeasuresInTheGivenInterval(string expr, string expected) =>
        Assert.Equal(expected, Value(expr));

    [Theory]
    [InlineData(@"DatePart(""yyyy"", ""3/4/2021"")", "2021")]
    [InlineData(@"DatePart(""m"", ""3/4/2021"")", "3")]
    [InlineData(@"DatePart(""d"", ""3/4/2021"")", "4")]
    [InlineData(@"DatePart(""q"", ""8/4/2021"")", "3")]
    public void DatePart_ExtractsTheGivenComponent(string expr, string expected) =>
        Assert.Equal(expected, Value(expr));

    [Theory]
    [InlineData(@"Hour(""3/4/2021 13:45:12"")", "13")]
    [InlineData(@"Minute(""3/4/2021 13:45:12"")", "45")]
    [InlineData(@"Second(""3/4/2021 13:45:12"")", "12")]
    public void TimeParts_AreExtracted(string expr, string expected) =>
        Assert.Equal(expected, Value(expr));

    [Theory]
    [InlineData(@"IsDate(""3/4/2021"")", true)]
    [InlineData(@"IsDate(""not a date"")", false)]
    public void IsDate_RecognisesParseableDates(string expr, bool expected) =>
        Assert.Equal(expected, Run(expr).Value);

    [Theory]
    [InlineData("MonthName(3)", "March")]
    [InlineData("MonthName(3, True)", "Mar")]
    [InlineData("WeekdayName(1)", "Sunday")]
    [InlineData("WeekdayName(1, True)", "Sun")]
    public void NamedDateParts_ReadWell(string expr, string expected) =>
        Assert.Equal(expected, Value(expr));

    // -------------------------------------------------------------------------
    // Remaining conversions
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Oct(8)", "10")]
    [InlineData("Oct(64)", "100")]
    [InlineData("Log(1)", "0")]
    [InlineData("Exp(0)", "1")]
    [InlineData(@"String(3, ""x"")", "xxx")]
    public void RemainingConversions(string expr, string expected) =>
        Assert.Equal(expected, Value(expr));

    // -------------------------------------------------------------------------
    // Shapes a real config would use
    // -------------------------------------------------------------------------

    [Fact]
    public void CountingItemsInADelimitedVariable()
    {
        // The idiom Split exists for: a ChoiceList or a department list.
        Assert.True(Run(@"UBound(Split(""Fire,IST,HR"", "","")) = 2").Value);
    }

    [Fact]
    public void TestingMembershipOfADelimitedVariable()
    {
        Assert.True(Run(@"UBound(Filter(Split(""Fire,IST,HR"", "",""), ""IST"")) = 0").Value);
        Assert.True(Run(@"UBound(Filter(Split(""Fire,IST,HR"", "",""), ""Legal"")) = -1").Value);
    }

    [Fact]
    public void CheckingAnAgeInDays() =>
        Assert.True(Run(@"DateDiff(""d"", ""1/1/2020"", ""1/1/2021"") > 300").Value);

    [Fact]
    public void NoneOfTheseAreReportedAsUnsupported()
    {
        string[] documented =
        [
            @"InStr(""abc"", ""b"")",
            @"Left(""abc"", 1)",
            @"Len(""abc"")",
            @"Mid(""abc"", 2, 1)",
            @"Replace(""abc"", ""b"", ""x"")",
            @"UBound(Split(""a,b"", "",""))",
            @"StrComp(""a"", ""b"")",
            @"Trim(""  a  "")",
        ];

        foreach (var expr in documented)
        {
            var result = Run(expr);
            Assert.True(result.IsReliable, $"{expr} reported: {result.DescribeProblems()}");
        }
    }
}
