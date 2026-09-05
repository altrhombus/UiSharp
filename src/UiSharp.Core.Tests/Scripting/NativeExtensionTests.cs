using UiSharp.Core.Scripting;

namespace UiSharp.Core.Tests.Scripting;

/// <summary>
/// Functions UiSharp adds that VBScript never had.
///
/// Each exists because something in this codebase went wrong for want of it,
/// and each is a function rather than an engine mode: an existing config
/// behaves exactly as before, and a new one opts in visibly at the point of
/// use. A mode would change what <c>=</c> means for every condition in a
/// document, decided by a line the reader may never see.
///
/// A config using these will not run under the original C++ UI++ or under the
/// vbscript engine. That is the documented trade.
/// </summary>
public class NativeExtensionTests
{
    private readonly NativeConditionEvaluator _eval = new();
    private readonly IReadOnlyDictionary<string, string> _empty = new Dictionary<string, string>();

    private bool Cond(string expr) => _eval.Evaluate(expr, _empty);
    private string? Value(string expr) => _eval.TryEvaluateValue(expr, out var v) ? v : null;
    private ConditionResult Run(string expr) => _eval.TryEvaluate(expr, _empty);

    // -------------------------------------------------------------------------
    // EqualsIgnoreCase
    //
    // VBScript compares strings binary, so "%XHWManufacturer%" = "Lenovo" is
    // false when WMI reports LENOVO. Vendor casing varies by firmware, which
    // makes this the most common way an OSD condition is quietly wrong.
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(@"EqualsIgnoreCase(""LENOVO"", ""Lenovo"")", true)]
    [InlineData(@"EqualsIgnoreCase(""lenovo"", ""LENOVO"")", true)]
    [InlineData(@"EqualsIgnoreCase(""LENOVO"", ""LENOVO"")", true)]
    [InlineData(@"EqualsIgnoreCase(""LENOVO"", ""Dell"")", false)]
    [InlineData(@"EqualsIgnoreCase("""", """")", true)]
    public void EqualsIgnoreCase_IgnoresCase(string expr, bool expected) =>
        Assert.Equal(expected, Cond(expr));

    [Fact]
    public void EqualsIgnoreCase_DoesNotChangeWhatEqualsMeans()
    {
        // The point of a function over a mode: '=' is untouched.
        Assert.False(Cond(@"""LENOVO"" = ""Lenovo"""));
        Assert.True(Cond(@"EqualsIgnoreCase(""LENOVO"", ""Lenovo"")"));
    }

    // -------------------------------------------------------------------------
    // IsSet
    //
    // An unresolved %Token% survives substitution intact. That is what made a
    // preflight check on a missing hardware value silently pass, before
    // conditions began failing closed. VBScript cannot express this question at
    // all: by the time it runs, unset and empty look identical.
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(@"IsSet(""WKS-001"")", true)]
    [InlineData(@"IsSet(""8192"")", true)]
    // Still shaped like a variable reference, so nothing replaced it.
    [InlineData(@"IsSet(""%XHWMemory%"")", false)]
    [InlineData(@"IsSet(""%Anything%"")", false)]
    // Set but empty is also "no value" for a condition's purposes.
    [InlineData(@"IsSet("""")", false)]
    [InlineData(@"IsSet(""   "")", false)]
    public void IsSet_DetectsUnresolvedOrEmptyValues(string expr, bool expected) =>
        Assert.Equal(expected, Cond(expr));

    [Theory]
    // A percent sign in real content must not read as unresolved.
    [InlineData(@"IsSet(""50% complete"")", true)]
    [InlineData(@"IsSet(""%"")", true)]
    [InlineData(@"IsSet(""100%"")", true)]
    [InlineData(@"IsSet(""%A% and %B%"")", true)]
    public void IsSet_OnlyTreatsAWholeLoneTokenAsUnresolved(string expr, bool expected) =>
        Assert.Equal(expected, Cond(expr));

    [Fact]
    public void IsSet_GuardsAConditionThatWouldOtherwiseBeMeaningless()
    {
        // The shape a preflight check should have used: check the value arrived
        // before comparing it.
        Assert.False(Cond(@"IsSet(""%XHWMemory%"") AND CDbl(""%XHWMemory%"") >= 1024"));
        Assert.True(Cond(@"IsSet(""8192"") AND CDbl(""8192"") >= 1024"));
    }

    // -------------------------------------------------------------------------
    // InList
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(@"InList(""Fire,IST,HR"", ""IST"")", true)]
    [InlineData(@"InList(""Fire,IST,HR"", ""Legal"")", false)]
    // Case-insensitive on purpose: these lists are typed by hand.
    [InlineData(@"InList(""Fire,IST,HR"", ""ist"")", true)]
    // Surrounding spaces in either the list or the item are ignored.
    [InlineData(@"InList(""Fire, IST, HR"", ""IST"")", true)]
    [InlineData(@"InList(""Fire,IST,HR"", "" IST "")", true)]
    // A partial match is not a match.
    [InlineData(@"InList(""Fire,IST,HR"", ""IS"")", false)]
    [InlineData(@"InList("""", ""IST"")", false)]
    public void InList_TestsMembershipForgivingly(string expr, bool expected) =>
        Assert.Equal(expected, Cond(expr));

    [Theory]
    [InlineData(@"InList(""a;b;c"", ""b"", "";"")", true)]
    [InlineData(@"InList(""a|b|c"", ""b"", ""|"")", true)]
    [InlineData(@"InList(""a;b;c"", ""b"")", false)]
    public void InList_AcceptsAnotherDelimiter(string expr, bool expected) =>
        Assert.Equal(expected, Cond(expr));

    [Fact]
    public void InList_IsTheForgivingAlternativeToFilterOverSplit()
    {
        // Both express membership; Filter is exact, InList ignores case.
        Assert.True(Cond(@"UBound(Filter(Split(""Fire,IST"", "",""), ""IST"")) >= 0"));
        Assert.False(Cond(@"UBound(Filter(Split(""Fire,IST"", "",""), ""ist"")) >= 0"));
        Assert.True(Cond(@"InList(""Fire,IST"", ""ist"")"));
    }

    // -------------------------------------------------------------------------
    // VersionCompare
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(@"VersionCompare(""10.0.19041"", ""10.0.9600"")", "1")]
    [InlineData(@"VersionCompare(""10.0.9600"", ""10.0.19041"")", "-1")]
    [InlineData(@"VersionCompare(""10.0.19041"", ""10.0.19041"")", "0")]
    [InlineData(@"VersionCompare(""6.1"", ""10.0"")", "-1")]
    // A missing component is zero, so these are the same version.
    [InlineData(@"VersionCompare(""10.0"", ""10.0.0"")", "0")]
    [InlineData(@"VersionCompare(""10.0.1"", ""10.0"")", "1")]
    [InlineData(@"VersionCompare(""1.2.3.4"", ""1.2.3.5"")", "-1")]
    public void VersionCompare_ComparesNumerically(string expr, string expected) =>
        Assert.Equal(expected, Value(expr));

    [Fact]
    public void VersionCompare_FixesWhatAStringComparisonGetsBackwards()
    {
        // The trap: "1" sorts before "9", so 19041 looks older than 9600.
        Assert.False(Cond(@"""10.0.19041"" > ""10.0.9600"""));
        Assert.True(Cond(@"VersionCompare(""10.0.19041"", ""10.0.9600"") > 0"));
    }

    [Fact]
    public void VersionCompare_TreatsUnparseableComponentsAsZero()
    {
        // Never throws on a value that is not really a version.
        Assert.Equal("0", Value(@"VersionCompare(""abc"", ""def"")"));
        Assert.Equal("1", Value(@"VersionCompare(""1.0"", ""abc"")"));
    }

    // -------------------------------------------------------------------------
    // All of these are first-class, not compatibility constructs
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(@"EqualsIgnoreCase(""a"", ""A"")")]
    [InlineData(@"IsSet(""x"")")]
    [InlineData(@"InList(""a,b"", ""a"")")]
    [InlineData(@"VersionCompare(""1.0"", ""1.0"")")]
    public void ExtensionsAreReportedAsNeitherUnsupportedNorLegacy(string expr)
    {
        var result = Run(expr);

        Assert.True(result.IsReliable, result.DescribeProblems());
        // No migration advice: these already are the modern form.
        Assert.Empty(result.Advice);
    }

    [Fact]
    public void ExtensionsComposeWithTheRestOfTheGrammar()
    {
        Assert.True(Cond(
            @"EqualsIgnoreCase(""LENOVO"", ""Lenovo"") AND VersionCompare(""10.0"", ""6.1"") > 0"));

        Assert.True(Cond(@"NOT InList(""Fire,IST"", ""Legal"")"));

        Assert.Equal("LENOVO", Value(@"UCase(""lenovo"")"));
    }
}
