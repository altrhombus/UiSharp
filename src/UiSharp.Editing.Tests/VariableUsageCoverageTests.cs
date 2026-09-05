using System.Xml.Linq;
using UiSharp.Editing;

namespace UiSharp.Editing.Tests;

/// <summary>
/// The reference badge and the usage list must describe the same thing.
///
/// They did not: CountReferences scans the raw XML, so it sees element content,
/// while FindUsages only walked attributes. A config using a variable in a TSVar
/// value or an Info body showed "3 refs" above a list of two, with no way to
/// navigate to the third. Both functions were individually correct, so only
/// comparing them catches it — which is what these tests do.
/// </summary>
public class VariableUsageCoverageTests
{
    // Built the way the editor builds them, so a group's Node really contains
    // its children. Hand-assembling a group with an empty Node but populated
    // Children produces a model the app can never hold, and the rendered XML
    // would not contain the children at all.
    private static ActionNodeModel Model(string xml) =>
        ActionXml.BuildModel(XElement.Parse(xml));

    private static string Document(params ActionNodeModel[] actions) =>
        ActionXml.BuildActionsXml(actions).Xml;

    // -------------------------------------------------------------------------
    // Content is searched, not just attributes
    // -------------------------------------------------------------------------

    [Fact]
    public void FindUsages_FindsAReferenceInATSVarValue()
    {
        var usages = VariableScanner.FindUsages(
            [Model("""<Action Type="TSVar" Variable="Full">%First% %Last%</Action>""")], "First");

        var usage = Assert.Single(usages);
        Assert.Equal("Content", usage.Field);
    }

    [Fact]
    public void FindUsages_FindsAReferenceInACDataBody()
    {
        // The case seen in the running editor: an Info body.
        var usages = VariableScanner.FindUsages(
        [
            Model("""<Action Type="Info"><![CDATA[Name: %SystemName%]]></Action>""")
        ], "SystemName");

        Assert.Equal("Content", Assert.Single(usages).Field);
    }

    [Fact]
    public void FindUsages_FindsAReferenceInADescendantsContent()
    {
        var usages = VariableScanner.FindUsages(
        [
            Model("""
                <Action Type="Switch" OnValue="x">
                  <Case RegEx="a"><Variable Name="Out">%Suffix%</Variable></Case>
                </Action>
                """)
        ], "Suffix");

        Assert.Equal("Variable · Content", Assert.Single(usages).Field);
    }

    // A parent must not be credited with a reference that belongs to its child,
    // or the same reference is reported twice at different depths.
    [Fact]
    public void FindUsages_DoesNotCreditAParentWithItsChildsContent()
    {
        var usages = VariableScanner.FindUsages(
        [
            Model("""
                <Action Type="Switch" OnValue="x">
                  <Case RegEx="a"><Variable Name="Out">%V%</Variable></Case>
                </Action>
                """)
        ], "V");

        // Exactly one row: the Variable element, not also the Case or the Action.
        Assert.Equal("Variable · Content", Assert.Single(usages).Field);
    }

    [Fact]
    public void FindUsages_DoesNotReportAGroupsChildContentTwice()
    {
        var usages = VariableScanner.FindUsages(
        [
            Model("""<ActionGroup Name="G"><Action Type="TSVar">%V%</Action></ActionGroup>""")
        ], "V");

        var usage = Assert.Single(usages);
        Assert.Equal(2, usage.ActionIndex);   // the child action, not the group
    }

    [Fact]
    public void FindUsages_ReportsAttributeAndContentSeparately()
    {
        var usages = VariableScanner.FindUsages(
            [Model("""<Action Type="TSVar" Variable="%V%">%V%</Action>""")], "V");

        Assert.Equal(2, usages.Count);
        Assert.Equal(["Variable", "Content"], usages.Select(u => u.Field));
    }

    // -------------------------------------------------------------------------
    // Badge and list agree
    // -------------------------------------------------------------------------

    public static IEnumerable<object[]> Configurations()
    {
        // The exact shape checked by hand in the running editor.
        yield return new object[]
        {
            new[]
            {
                Model("""<Action Type="TSVar" Variable="SystemNameSuffix">CTG</Action>"""),
                Model("""
                    <Action Type="Input" Title="Information">
                      <TextInput Variable="SystemName" Hint="use the suffix %SystemNameSuffix%"
                                 RegEx=".{3,5}%SystemNameSuffix%" />
                    </Action>
                    """),
                Model("""
                    <Action Type="Info" Title="Summary for %SystemName%"><![CDATA[Name: %SystemName%%SystemNameSuffix%]]></Action>
                    """),
            },
            new[] { "SystemNameSuffix", "SystemName" },
        };

        yield return new object[]
        {
            new[]
            {
                Model("""<Action Type="TSVar" Variable="A">seed</Action>"""),
                Model("""<Action Type="TSVar" Variable="B">%A%</Action>"""),
                Model("""<Action Type="Info" Title="%A%"><![CDATA[%B%]]></Action>"""),
            },
            new[] { "A", "B" },
        };

        yield return new object[]
        {
            new[]
            {
                Model("""
                    <ActionGroup Name="G">
                      <Action Type="TSVar" Variable="X">%Y%</Action>
                      <Action Type="Info" Title="%Y%" />
                    </ActionGroup>
                    """),
                Model("""<Action Type="TSVar" Variable="Y">v</Action>"""),
            },
            new[] { "Y" },
        };
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public void EveryCountedReferenceHasSomewhereToNavigateTo(
        ActionNodeModel[] actions, string[] variables)
    {
        var xml = Document(actions);

        foreach (var name in variables)
        {
            var count = VariableScanner.CountReferences(name, xml);
            var usages = VariableScanner.FindUsages(actions, name);

            Assert.True(count > 0, $"expected %{name}% to be referenced at all");

            // The badge said N; the list must be able to show N places. It is
            // allowed to show fewer only when one site holds several references,
            // which the next test covers.
            Assert.Equal(count, usages.Count);
        }
    }

    // -------------------------------------------------------------------------
    // The one case where the two legitimately differ
    // -------------------------------------------------------------------------

    [Fact]
    public void ASingleSiteHoldingTwoReferencesCountsTwiceButListsOnce()
    {
        // "%V% and %V%" is two references in one attribute. Listing it twice
        // would give two rows that navigate to the same place, so the list keeps
        // one row per site and the badge stays a count of references.
        var actions = new[] { Model("""<Action Type="Info" Title="%V% and %V%" />""") };

        Assert.Equal(2, VariableScanner.CountReferences("V", Document(actions)));
        Assert.Single(VariableScanner.FindUsages(actions, "V"));
    }

    // -------------------------------------------------------------------------
    // Unreferenced variables stay unreferenced
    // -------------------------------------------------------------------------

    [Fact]
    public void AnUnusedVariableHasNoCountAndNoUsages()
    {
        var actions = new[]
        {
            Model("""<Action Type="TSVar" Variable="Unused">value</Action>"""),
        };

        // The declaring attribute is a bare name, not a %tag%, so it is not a
        // reference to itself.
        Assert.Equal(0, VariableScanner.CountReferences("Unused", Document(actions)));
        Assert.Empty(VariableScanner.FindUsages(actions, "Unused"));
    }

    [Fact]
    public void WhitespaceOnlyContentIsNotAReference()
    {
        var actions = new[] { Model("""<Action Type="Info">   </Action>""") };

        Assert.Empty(VariableScanner.FindUsages(actions, "V"));
    }
}
