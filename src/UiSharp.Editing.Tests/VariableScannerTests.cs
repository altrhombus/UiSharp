using System.Xml.Linq;
using UiSharp.Editing;

namespace UiSharp.Editing.Tests;

/// <summary>
/// Tests for the editor's variable analysis and its cascading-rename offer.
///
/// This drives the Variables page, the autocomplete lists, the "view usages"
/// links and the rename prompt. It previously lived in a WinUI view model, so
/// none of it was reachable by a test.
/// </summary>
public class VariableScannerTests
{
    private static ActionNodeModel Model(string xml, params ActionNodeModel[] children)
    {
        var model = new ActionNodeModel { Node = XElement.Parse(xml) };
        model.Children.AddRange(children);
        return model;
    }

    // -------------------------------------------------------------------------
    // Declared variables
    // -------------------------------------------------------------------------

    [Fact]
    public void CollectDeclared_FindsTheVariableAttribute()
    {
        var declared = VariableScanner.CollectDeclared(
            [Model("""<Action Type="TSVar" Variable="ComputerName" />""")]);

        var entry = Assert.Single(declared);
        Assert.Equal("ComputerName", entry.Name);
        Assert.Equal("TSVar", entry.SourceType);
        Assert.Equal(1, entry.ActionIndex);
    }

    [Fact]
    public void CollectDeclared_FindsExitCodeVariable()
    {
        var declared = VariableScanner.CollectDeclared(
            [Model("""<Action Type="ExternalCall" ExitCodeVariable="Code" />""")]);

        Assert.Equal("Code", Assert.Single(declared).Name);
    }

    [Fact]
    public void CollectDeclared_FindsOnePerInputField()
    {
        // Input actions declare on their fields, not on themselves, and each
        // field is labelled by its element name.
        var declared = VariableScanner.CollectDeclared(
        [
            Model("""
                <Action Type="Input">
                  <TextInput Variable="Name" />
                  <ChoiceInput Variable="Dept" />
                  <CheckboxInput Variable="IsExec" />
                </Action>
                """)
        ]);

        Assert.Equal(3, declared.Count);
        Assert.Equal(["Name", "Dept", "IsExec"], declared.Select(d => d.Name));
        Assert.Equal(["TextInput", "ChoiceInput", "CheckboxInput"], declared.Select(d => d.SourceType));
        Assert.All(declared, d => Assert.Equal(1, d.ActionIndex));
    }

    [Fact]
    public void CollectDeclared_IgnoresGroupsThemselvesButVisitsTheirChildren()
    {
        var declared = VariableScanner.CollectDeclared(
        [
            Model("""<ActionGroup Name="G" Variable="ShouldBeIgnored" />""",
                Model("""<Action Type="TSVar" Variable="Inner" />"""))
        ]);

        var entry = Assert.Single(declared);
        Assert.Equal("Inner", entry.Name);
        Assert.Equal(2, entry.ActionIndex);   // the group is #1
    }

    [Fact]
    public void CollectDeclared_NumbersActionsDepthFirstStartingAtOne()
    {
        var declared = VariableScanner.CollectDeclared(
        [
            Model("""<Action Type="TSVar" Variable="A" />"""),                       // 1
            Model("""<ActionGroup Name="G" />""",                                     // 2
                Model("""<Action Type="TSVar" Variable="B" />"""),                    // 3
                Model("""<Action Type="TSVar" Variable="C" />""")),                   // 4
            Model("""<Action Type="TSVar" Variable="D" />"""),                        // 5
        ]);

        Assert.Equal([1, 3, 4, 5], declared.Select(d => d.ActionIndex));
        Assert.Equal(["A", "B", "C", "D"], declared.Select(d => d.Name));
    }

    [Fact]
    public void CollectDeclared_KeepsDuplicatesSoBothDeclarationsAreVisible()
    {
        var declared = VariableScanner.CollectDeclared(
        [
            Model("""<Action Type="TSVar" Variable="Dup" />"""),
            Model("""<Action Type="RegRead" Variable="Dup" />"""),
        ]);

        Assert.Equal(2, declared.Count);
        Assert.Equal([1, 2], declared.Select(d => d.ActionIndex));
    }

    [Theory]
    [InlineData("""<Action Type="TSVar" Variable="" />""")]
    [InlineData("""<Action Type="TSVar" Variable="   " />""")]
    [InlineData("""<Action Type="TSVar" />""")]
    public void CollectDeclared_SkipsBlankNames(string xml) =>
        Assert.Empty(VariableScanner.CollectDeclared([Model(xml)]));

    // -------------------------------------------------------------------------
    // The single variable an action declares, used as the rename anchor
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("TSVar")]
    [InlineData("RegRead")]
    [InlineData("WMIRead")]
    [InlineData("FileRead")]
    [InlineData("REST")]
    [InlineData("FromJSON")]
    [InlineData("ToJSON")]
    [InlineData("RandomString")]
    public void DeclaredVariableOf_ReadsVariableForTypesThatDeclareOne(string type) =>
        Assert.Equal("V", VariableScanner.DeclaredVariableOf(
            Model($"""<Action Type="{type}" Variable="V" />""")));

    [Fact]
    public void DeclaredVariableOf_ReadsExitCodeVariableForExternalCall() =>
        Assert.Equal("Code", VariableScanner.DeclaredVariableOf(
            Model("""<Action Type="ExternalCall" ExitCodeVariable="Code" />""")));

    [Fact]
    public void DeclaredVariableOf_IsNullForAGroup() =>
        Assert.Null(VariableScanner.DeclaredVariableOf(
            Model("""<ActionGroup Name="G" Variable="X" />""")));

    [Fact]
    public void DeclaredVariableOf_IsNullForATypeThatDeclaresNothing() =>
        Assert.Null(VariableScanner.DeclaredVariableOf(
            Model("""<Action Type="Info" Variable="X" />""")));

    // Input declares per-field, so the action itself anchors nothing.
    [Fact]
    public void DeclaredVariableOf_IsNullForInput() =>
        Assert.Null(VariableScanner.DeclaredVariableOf(
            Model("""<Action Type="Input"><TextInput Variable="X" /></Action>""")));

    // -------------------------------------------------------------------------
    // Usages
    // -------------------------------------------------------------------------

    [Fact]
    public void FindUsages_FindsAReferenceInAnActionAttribute()
    {
        var usages = VariableScanner.FindUsages(
            [Model("""<Action Type="Info" Title="Hello %Name%" />""")], "Name");

        var usage = Assert.Single(usages);
        Assert.Equal(1, usage.ActionIndex);
        Assert.Equal("Title", usage.Field);
    }

    [Fact]
    public void FindUsages_LooksInsideALeafActionsDescendants()
    {
        var usages = VariableScanner.FindUsages(
        [
            Model("""
                <Action Type="Input">
                  <TextInput Variable="X" Hint="suffix %Suffix%" />
                </Action>
                """)
        ], "Suffix");

        var usage = Assert.Single(usages);
        Assert.Equal("TextInput · Hint", usage.Field);
    }

    // A group's children are visited in their own right, so searching the
    // group's descendants as well would report every hit twice.
    [Fact]
    public void FindUsages_DoesNotReportAGroupsChildrenTwice()
    {
        var usages = VariableScanner.FindUsages(
        [
            Model("""<ActionGroup Name="G"><Action Type="Info" Title="%V%" /></ActionGroup>""",
                Model("""<Action Type="Info" Title="%V%" />"""))
        ], "V");

        var usage = Assert.Single(usages);
        Assert.Equal(2, usage.ActionIndex);   // the child, not the group
    }

    [Fact]
    public void FindUsages_IsCaseInsensitive()
    {
        var usages = VariableScanner.FindUsages(
            [Model("""<Action Type="Info" Title="%NAME%" />""")], "name");

        Assert.Single(usages);
    }

    [Fact]
    public void FindUsages_UsesTheSameIndicesAsCollectDeclared()
    {
        // The two must agree, or "view usages" navigates to the wrong action.
        var actions = new List<ActionNodeModel>
        {
            Model("""<Action Type="TSVar" Variable="Suffix" />"""),                  // 1
            Model("""<ActionGroup Name="G" />""",                                     // 2
                Model("""<Action Type="Info" Title="%Suffix%" />""")),                // 3
        };

        var declared = Assert.Single(VariableScanner.CollectDeclared(actions));
        var usage = Assert.Single(VariableScanner.FindUsages(actions, "Suffix"));

        Assert.Equal(1, declared.ActionIndex);
        Assert.Equal(3, usage.ActionIndex);
    }

    [Fact]
    public void FindUsages_ForAnUnusedVariable_IsEmpty() =>
        Assert.Empty(VariableScanner.FindUsages(
            [Model("""<Action Type="Info" Title="plain" />""")], "Name"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FindUsages_WithNoName_IsEmpty(string name) =>
        Assert.Empty(VariableScanner.FindUsages(
            [Model("""<Action Type="Info" Title="%X%" />""")], name));

    // -------------------------------------------------------------------------
    // Reference counting
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("<Actions><Action Title=\"%A%\" /></Actions>", "A", 1)]
    [InlineData("<Actions><Action Title=\"%A% %A%\" /></Actions>", "A", 2)]
    [InlineData("<Actions><Action Title=\"none\" /></Actions>", "A", 0)]
    [InlineData("<Actions><Action Title=\"%a%\" /></Actions>", "A", 1)]   // case-insensitive
    public void CountReferences_CountsTaggedOccurrences(string xml, string name, int expected) =>
        Assert.Equal(expected, VariableScanner.CountReferences(name, xml));

    [Fact]
    public void CountReferences_CountsReferencesInElementText()
    {
        // A TSVar's value lives in element content, not an attribute, which is
        // why counting runs over the raw XML rather than the model.
        var xml = """<Actions><Action Type="TSVar" Variable="B">%A%</Action></Actions>""";

        Assert.Equal(1, VariableScanner.CountReferences("A", xml));
    }

    [Fact]
    public void CountReferences_DoesNotMatchAnUntaggedName() =>
        Assert.Equal(0, VariableScanner.CountReferences("A", "<Actions><Action Title=\"A\" /></Actions>"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CountReferences_WithNoName_IsZero(string? name) =>
        Assert.Equal(0, VariableScanner.CountReferences(name!, "<Actions />"));

    // -------------------------------------------------------------------------
    // Renaming references
    // -------------------------------------------------------------------------

    [Fact]
    public void RenameReferences_RewritesEveryOccurrence()
    {
        var xml = """<Actions><Action Title="%Old% and %Old%" /></Actions>""";

        Assert.Equal(
            """<Actions><Action Title="%New% and %New%" /></Actions>""",
            VariableScanner.RenameReferences(xml, "Old", "New"));
    }

    // The tags are what get replaced, so a name that is a prefix of another
    // is not caught by accident.
    [Fact]
    public void RenameReferences_LeavesAPrefixNameAlone()
    {
        var xml = """<Actions><Action Title="%Name% %NameSuffix%" /></Actions>""";

        Assert.Equal(
            """<Actions><Action Title="%Renamed% %NameSuffix%" /></Actions>""",
            VariableScanner.RenameReferences(xml, "Name", "Renamed"));
    }

    [Fact]
    public void RenameReferences_DoesNotTouchAnUntaggedOccurrence()
    {
        var xml = """<Actions><Action Type="TSVar" Variable="Old">%Old%</Action></Actions>""";

        // The declaring attribute is a bare name; only the reference is a tag.
        Assert.Equal(
            """<Actions><Action Type="TSVar" Variable="Old">%New%</Action></Actions>""",
            VariableScanner.RenameReferences(xml, "Old", "New"));
    }

    // -------------------------------------------------------------------------
    // The rename offer
    // -------------------------------------------------------------------------

    private static RenameDecision Decide(string? anchor, string? current, int refCount = 0) =>
        VariableScanner.DecideRename(anchor, current, _ => refCount);

    [Fact]
    public void DecideRename_WithNoAnchor_AdoptsTheCurrentName()
    {
        // A legacy TSVar using the Name attribute has no anchor until the first
        // flush migrates Name to Variable. That stabilising edit must not look
        // like the user renaming something.
        var decision = Decide(anchor: null, current: "Migrated");

        Assert.Equal(RenameAction.AdoptAnchor, decision.Action);
        Assert.Equal("Migrated", decision.To);
    }

    [Fact]
    public void DecideRename_WhenTheNameChangedAndIsReferenced_Offers()
    {
        var decision = Decide(anchor: "Old", current: "New", refCount: 3);

        Assert.Equal(RenameAction.Offer, decision.Action);
        Assert.Equal("Old", decision.From);
        Assert.Equal("New", decision.To);
        Assert.Equal(3, decision.ReferenceCount);
    }

    [Fact]
    public void DecideRename_WhenNothingReferencesTheOldName_DoesNotOffer() =>
        Assert.Equal(RenameAction.Dismiss, Decide("Old", "New", refCount: 0).Action);

    [Fact]
    public void DecideRename_WhenTheNameIsUnchanged_DoesNotOffer() =>
        Assert.Equal(RenameAction.Dismiss, Decide("Same", "Same", refCount: 5).Action);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DecideRename_WhenTheNameWasCleared_DoesNotOffer(string? current) =>
        Assert.Equal(RenameAction.Dismiss, Decide("Old", current, refCount: 5).Action);

    // The anchor is the name held since selection, so typing "A" -> "AB" -> "ABC"
    // keeps offering to rename from the original, not from the previous keystroke.
    [Fact]
    public void DecideRename_AlwaysComparesAgainstTheAnchorNotTheLastEdit()
    {
        foreach (var typed in new[] { "Ab", "Abc", "Abcd" })
        {
            var decision = Decide(anchor: "A", current: typed, refCount: 1);

            Assert.Equal(RenameAction.Offer, decision.Action);
            Assert.Equal("A", decision.From);
            Assert.Equal(typed, decision.To);
        }
    }

    [Fact]
    public void DecideRename_DoesNotCountReferencesUnlessARenameIsInProspect()
    {
        // Counting scans the whole document on every keystroke otherwise.
        var counted = 0;

        VariableScanner.DecideRename("Same", "Same", _ => { counted++; return 1; });
        VariableScanner.DecideRename(null, "Anything", _ => { counted++; return 1; });
        VariableScanner.DecideRename("Old", "", _ => { counted++; return 1; });

        Assert.Equal(0, counted);
    }

    // -------------------------------------------------------------------------
    // Attribute labels
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("OnValue", "On Value")]
    [InlineData("ExitCodeVariable", "Exit Code Var")]
    [InlineData("WarnDescription", "Warn Description")]
    [InlineData("Condition", "Condition")]
    [InlineData("SomethingUnmapped", "SomethingUnmapped")]
    public void FriendlyAttributeName_ReadsWellOrFallsBackToTheXmlName(string xml, string expected) =>
        Assert.Equal(expected, VariableScanner.FriendlyAttributeName(xml));
}
