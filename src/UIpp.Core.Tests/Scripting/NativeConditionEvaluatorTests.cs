using UIpp.Core.Scripting;

namespace UIpp.Core.Tests.Scripting;

public class NativeConditionEvaluatorTests
{
    private readonly NativeConditionEvaluator _eval = new();
    private readonly IReadOnlyDictionary<string, string> _empty =
        new Dictionary<string, string>();

    private bool Eval(string expr) => _eval.Evaluate(expr, _empty);

    // ----------------------------------------------------------
    // String comparisons
    // ----------------------------------------------------------

    [Theory]
    [InlineData("'LAPTOP' = 'LAPTOP'",  true)]
    [InlineData("'LAPTOP' = 'DESKTOP'", false)]
    [InlineData("'LAPTOP' <> 'DESKTOP'", true)]
    [InlineData("'A' < 'B'",  true)]
    [InlineData("'B' > 'A'",  true)]
    [InlineData("'A' <= 'A'", true)]
    [InlineData("'A' >= 'B'", false)]
    public void StringComparisons(string expr, bool expected) =>
        Assert.Equal(expected, Eval(expr));

    [Fact]
    public void StringCompare_CaseInsensitive() =>
        Assert.True(Eval("'laptop' = 'LAPTOP'"));

    // ----------------------------------------------------------
    // Numeric comparisons
    // ----------------------------------------------------------

    [Theory]
    [InlineData("1 = 1",    true)]
    [InlineData("1 = 2",    false)]
    [InlineData("1 < 2",    true)]
    [InlineData("2 > 1",    true)]
    [InlineData("1 <= 1",   true)]
    [InlineData("2 >= 3",   false)]
    [InlineData("1 <> 2",   true)]
    public void NumericComparisons(string expr, bool expected) =>
        Assert.Equal(expected, Eval(expr));

    // ----------------------------------------------------------
    // Boolean operators
    // ----------------------------------------------------------

    [Theory]
    [InlineData("'A' = 'A' AND '1' = '1'",  true)]
    [InlineData("'A' = 'A' AND '1' = '2'",  false)]
    [InlineData("'A' = 'B' OR '1' = '1'",   true)]
    [InlineData("'A' = 'B' OR '1' = '2'",   false)]
    [InlineData("NOT '1' = '2'",             true)]
    [InlineData("NOT '1' = '1'",             false)]
    public void BooleanOperators(string expr, bool expected) =>
        Assert.Equal(expected, Eval(expr));

    [Fact]
    public void Grouping_OverridesPrecedence() =>
        Assert.True(Eval("('A' = 'B' OR 'A' = 'A') AND '1' = '1'"));

    [Fact]
    public void AndBeforeOr() =>
        // 'A'='A' OR ('A'='B' AND '1'='2') → true OR false → true
        Assert.True(Eval("'A' = 'A' OR 'A' = 'B' AND '1' = '2'"));

    // ----------------------------------------------------------
    // Empty / trivial
    // ----------------------------------------------------------

    [Fact]
    public void EmptyExpression_ReturnsTrue() =>
        Assert.True(_eval.Evaluate("", _empty));

    [Fact]
    public void WhitespaceExpression_ReturnsTrue() =>
        Assert.True(_eval.Evaluate("   ", _empty));

    // ----------------------------------------------------------
    // InStr
    // ----------------------------------------------------------

    [Theory]
    [InlineData("InStr('SRV-001', 'SRV') > 0",  true)]
    [InlineData("InStr('WKS-001', 'SRV') > 0",  false)]
    [InlineData("InStr('Hello', 'ell') = 2",     true)]
    [InlineData("InStr('Hello', 'xyz') = 0",     true)]
    public void InStr_Basic(string expr, bool expected) =>
        Assert.Equal(expected, Eval(expr));

    [Fact]
    public void InStr_WithStartParam() =>
        // 'bb' is at position 3; searching from position 4 skips it → returns 0
        Assert.True(Eval("InStr(4, 'aabbcc', 'bb') = 0"));

    [Fact]
    public void InStr_CaseInsensitive() =>
        Assert.True(Eval("InStr('Hello', 'HELLO') > 0"));

    // ----------------------------------------------------------
    // UCase / LCase
    // ----------------------------------------------------------

    [Fact]
    public void UCase() => Assert.True(Eval("UCase('hello') = 'HELLO'"));
    [Fact]
    public void LCase() => Assert.True(Eval("LCase('HELLO') = 'hello'"));

    // ----------------------------------------------------------
    // Len
    // ----------------------------------------------------------

    [Fact]
    public void Len_Basic() => Assert.True(Eval("Len('hello') = 5"));
    [Fact]
    public void Len_Empty() => Assert.True(Eval("Len('') = 0"));

    // ----------------------------------------------------------
    // Mid / Left / Right
    // ----------------------------------------------------------

    [Fact]
    public void Mid_TwoArg() => Assert.True(Eval("Mid('Hello', 2) = 'ello'"));
    [Fact]
    public void Mid_ThreeArg() => Assert.True(Eval("Mid('Hello', 2, 3) = 'ell'"));
    [Fact]
    public void Left_Basic() => Assert.True(Eval("Left('Hello', 3) = 'Hel'"));
    [Fact]
    public void Right_Basic() => Assert.True(Eval("Right('Hello', 3) = 'llo'"));

    // ----------------------------------------------------------
    // Trim
    // ----------------------------------------------------------

    [Fact]
    public void Trim_Basic() => Assert.True(Eval("Trim('  hi  ') = 'hi'"));
    [Fact]
    public void LTrim_Basic() => Assert.True(Eval("LTrim('  hi') = 'hi'"));
    [Fact]
    public void RTrim_Basic() => Assert.True(Eval("RTrim('hi  ') = 'hi'"));

    // ----------------------------------------------------------
    // IsNumeric
    // ----------------------------------------------------------

    [Theory]
    [InlineData("IsNumeric('42')",   true)]
    [InlineData("IsNumeric('3.14')", true)]
    [InlineData("IsNumeric('abc')",  false)]
    public void IsNumeric(string expr, bool expected) =>
        Assert.Equal(expected, Eval(expr));

    // ----------------------------------------------------------
    // Replace
    // ----------------------------------------------------------

    [Fact]
    public void Replace_Basic() =>
        Assert.True(Eval("Replace('hello world', 'world', 'earth') = 'hello earth'"));

    // ----------------------------------------------------------
    // Real-world style expressions (post-substitution)
    // ----------------------------------------------------------

    [Fact]
    public void RealWorld_LaptopCheck() =>
        // After substitution: "LAPTOP" = "LAPTOP"
        Assert.True(Eval("'LAPTOP' = 'LAPTOP'"));

    [Fact]
    public void RealWorld_InStrCondition() =>
        // InStr('%OSDComputerName%', 'SRV') > 0  after substitution becomes InStr('SRV-001', 'SRV') > 0
        Assert.True(Eval("InStr('SRV-001', 'SRV') > 0"));

    [Fact]
    public void RealWorld_CompoundAndNot() =>
        Assert.True(Eval("NOT 'LAPTOP' = 'DESKTOP' AND Len('SRV-001') > 0"));

    [Fact]
    public void RealWorld_NumericMemoryCheck() =>
        // %XHWMemory% substituted to 16384 (MB)
        Assert.True(Eval("16384 >= 8192"));

    // ----------------------------------------------------------
    // Mod operator
    // ----------------------------------------------------------

    [Theory]
    [InlineData("10 Mod 3 = 1",   true)]
    [InlineData("10 Mod 5 = 0",   true)]
    [InlineData("7 Mod 4 = 3",    true)]
    [InlineData("10 Mod 3 = 0",   false)]
    public void Mod_Basic(string expr, bool expected) =>
        Assert.Equal(expected, Eval(expr));

    [Fact]
    public void Mod_DivideByZero_ReturnsZero() =>
        Assert.True(Eval("5 Mod 0 = 0"));

    [Fact]
    public void Mod_InCompoundExpression() =>
        // Typical use: check if memory is even multiple of 1024
        Assert.True(Eval("16384 Mod 1024 = 0 AND 16384 >= 8192"));

    // ----------------------------------------------------------
    // Date / Time built-ins (smoke tests — exact values vary)
    // ----------------------------------------------------------

    [Fact]
    public void Now_ReturnsNonEmpty() =>
        Assert.True(Eval("Len(Now()) > 0"));

    [Fact]
    public void Date_ReturnsNonEmpty() =>
        Assert.True(Eval("Len(Date()) > 0"));

    [Fact]
    public void Year_ReturnsCurrentYear() =>
        Assert.True(Eval($"Year(Now()) = {DateTime.Now.Year}"));

    [Fact]
    public void Month_InRange() =>
        Assert.True(Eval("Month(Now()) >= 1 AND Month(Now()) <= 12"));
}
