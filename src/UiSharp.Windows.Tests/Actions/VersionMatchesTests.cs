using UiSharp.Windows.Actions;

namespace UiSharp.Windows.Tests.Actions;

// Tests for ActionSoftwareDiscovery.VersionMatches — the version comparison helper
// used to evaluate the VersionOperator attribute on <Match> elements.
public class VersionMatchesTests
{
    // Helper — shorter call site
    private static bool Vm(string installed, string op, string target) =>
        ActionSoftwareDiscovery.VersionMatches(installed, op, target);

    // ── Parsed Version comparisons ─────────────────────────────────────────────

    [Theory]
    [InlineData("1.2.3.4", "eq",  "1.2.3.4", true)]
    [InlineData("1.2.3.4", "=",   "1.2.3.4", true)]
    [InlineData("1.2.3.4", "eq",  "1.2.3.5", false)]
    public void Eq_ParsedVersion(string inst, string op, string target, bool expected) =>
        Assert.Equal(expected, Vm(inst, op, target));

    [Theory]
    [InlineData("1.2.3.4", "ne",  "1.2.3.5", true)]
    [InlineData("1.2.3.4", "!=",  "1.2.3.5", true)]
    [InlineData("1.2.3.4", "ne",  "1.2.3.4", false)]
    public void Ne_ParsedVersion(string inst, string op, string target, bool expected) =>
        Assert.Equal(expected, Vm(inst, op, target));

    [Theory]
    [InlineData("1.0.0.0", "lt",  "2.0.0.0", true)]
    [InlineData("1.0.0.0", "<",   "2.0.0.0", true)]
    [InlineData("2.0.0.0", "lt",  "1.0.0.0", false)]
    [InlineData("1.0.0.0", "lt",  "1.0.0.0", false)]
    public void Lt_ParsedVersion(string inst, string op, string target, bool expected) =>
        Assert.Equal(expected, Vm(inst, op, target));

    [Theory]
    [InlineData("1.0.0.0", "lte", "1.0.0.0", true)]
    [InlineData("1.0.0.0", "<=",  "2.0.0.0", true)]
    [InlineData("2.0.0.0", "lte", "1.0.0.0", false)]
    public void Lte_ParsedVersion(string inst, string op, string target, bool expected) =>
        Assert.Equal(expected, Vm(inst, op, target));

    [Theory]
    [InlineData("2.0.0.0", "gt",  "1.0.0.0", true)]
    [InlineData("2.0.0.0", ">",   "1.0.0.0", true)]
    [InlineData("1.0.0.0", "gt",  "2.0.0.0", false)]
    [InlineData("1.0.0.0", "gt",  "1.0.0.0", false)]
    public void Gt_ParsedVersion(string inst, string op, string target, bool expected) =>
        Assert.Equal(expected, Vm(inst, op, target));

    [Theory]
    [InlineData("2.0.0.0", "gte", "2.0.0.0", true)]
    [InlineData("2.0.0.0", ">=",  "1.0.0.0", true)]
    [InlineData("1.0.0.0", "gte", "2.0.0.0", false)]
    public void Gte_ParsedVersion(string inst, string op, string target, bool expected) =>
        Assert.Equal(expected, Vm(inst, op, target));

    // ── String-fallback comparisons (non-parseable versions) ──────────────────

    [Fact]
    public void StringFallback_Eq_EqualStrings() =>
        Assert.True(Vm("notaversion", "eq", "notaversion"));

    [Fact]
    public void StringFallback_Eq_DifferentStrings() =>
        Assert.False(Vm("abc", "eq", "xyz"));

    [Fact]
    public void StringFallback_CaseInsensitive() =>
        Assert.True(Vm("ABC", "eq", "abc"));

    [Fact]
    public void StringFallback_Ne() =>
        Assert.True(Vm("abc", "ne", "xyz"));

    [Fact]
    public void StringFallback_Lt() =>
        Assert.True(Vm("abc", "lt", "xyz"));

    [Fact]
    public void StringFallback_Gt() =>
        Assert.True(Vm("xyz", "gt", "abc"));

    // ── Unknown operator ───────────────────────────────────────────────────────

    [Fact]
    public void UnknownOperator_ReturnsFalse() =>
        Assert.False(Vm("1.0", "unknown", "1.0"));

    // ── Operator alias symmetry ────────────────────────────────────────────────

    [Theory]
    [InlineData("eq",  "=")]
    [InlineData("ne",  "!=")]
    [InlineData("lt",  "<")]
    [InlineData("lte", "<=")]
    [InlineData("gt",  ">")]
    [InlineData("gte", ">=")]
    public void SymbolAliasMatchesWordAlias(string word, string sym)
    {
        Assert.Equal(Vm("1.0.0.0", word, "1.0.0.0"), Vm("1.0.0.0", sym, "1.0.0.0"));
        Assert.Equal(Vm("2.0.0.0", word, "1.0.0.0"), Vm("2.0.0.0", sym, "1.0.0.0"));
        Assert.Equal(Vm("1.0.0.0", word, "2.0.0.0"), Vm("1.0.0.0", sym, "2.0.0.0"));
    }
}
