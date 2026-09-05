using System.Xml.Linq;
using UiSharp.Editing;

namespace UiSharp.Editing.Tests;

/// <summary>
/// The two halves of the editor's sync must agree.
///
/// <c>BuildActionsXml</c> runs when the guided pane is edited;
/// <c>ComputeElementLineRanges</c> runs when the XML pane is typed in. They used
/// to disagree about whether an action's comment belongs to its line range —
/// each side carrying a code comment defending the opposite position — so
/// clicking a comment line selected the action below it, the action above it, or
/// nothing, depending on which pane had been touched last.
///
/// The model settles it: ExtractNodePairs attaches a comment to the element that
/// FOLLOWS it, so the note belongs to that action and clicking it must select
/// that action.
/// </summary>
public class EditorXmlSyncTests
{
    private static ActionNodeModel Action(string type, string? comment = null) =>
        new() { Node = XElement.Parse($"""<Action Type="{type}" />"""), Comment = comment };

    private static ActionNodeModel FromXml(string xml, string? comment = null) =>
        new() { Node = XElement.Parse(xml), Comment = comment };

    // -------------------------------------------------------------------------
    // The agreement property
    // -------------------------------------------------------------------------

    public static IEnumerable<object[]> Documents()
    {
        // No comments at all.
        yield return new object[] { new List<ActionNodeModel> { Action("A"), Action("B") } };

        // A comment on the first action only.
        yield return new object[] { new List<ActionNodeModel> { Action("A", "note"), Action("B") } };

        // A comment on the second action only — the case that used to select
        // the wrong action.
        yield return new object[] { new List<ActionNodeModel> { Action("A"), Action("B", "note") } };

        // Comments on every action.
        yield return new object[]
        {
            new List<ActionNodeModel> { Action("A", "first"), Action("B", "second"), Action("C", "third") }
        };

        // A multi-line note, which is emitted as a comment block.
        yield return new object[]
        {
            new List<ActionNodeModel> { Action("A", "line one\nline two"), Action("B") }
        };

        // Multi-line actions.
        yield return new object[]
        {
            new List<ActionNodeModel>
            {
                FromXml("""
                    <Action Type="Input">
                      <TextInput Variable="X" />
                    </Action>
                    """, "ask for X"),
                Action("TSVar"),
            }
        };

        // A group, whose children are rendered inside its own element text.
        yield return new object[]
        {
            new List<ActionNodeModel>
            {
                FromXml("""
                    <ActionGroup Name="G">
                      <Action Type="Info" />
                    </ActionGroup>
                    """, "the group"),
                Action("TSVar", "after it"),
            }
        };

        // A single action, commented.
        yield return new object[] { new List<ActionNodeModel> { Action("Only", "just one") } };

        // Nothing at all.
        yield return new object[] { new List<ActionNodeModel>() };
    }

    [Theory]
    [MemberData(nameof(Documents))]
    public void BuildAndParseAgree(List<ActionNodeModel> models)
    {
        var (xml, built) = EditorXml.BuildActionsXml(models);
        var parsed = EditorXml.ComputeElementLineRanges(xml);

        Assert.Equal(built.Count, parsed.Count);

        for (var i = 0; i < built.Count; i++)
        {
            Assert.Equal(built[i], parsed[i]);
        }
    }

    // Every line an action claims must map back to that same action, whichever
    // direction produced the ranges.
    [Theory]
    [MemberData(nameof(Documents))]
    public void EveryClaimedLineSelectsTheSameActionBothWays(List<ActionNodeModel> models)
    {
        var (xml, built) = EditorXml.BuildActionsXml(models);
        var parsed = EditorXml.ComputeElementLineRanges(xml);

        var lineCount = xml.Split('\n').Length;

        for (var line = 1; line <= lineCount; line++)
        {
            Assert.Equal(
                EditorXml.IndexOfElementAtLine(built, line),
                EditorXml.IndexOfElementAtLine(parsed, line));
        }
    }

    // -------------------------------------------------------------------------
    // The rule itself
    // -------------------------------------------------------------------------

    [Fact]
    public void ClickingACommentSelectsTheActionBelowIt()
    {
        var models = new List<ActionNodeModel> { Action("A"), Action("B", "about B") };

        var (xml, built) = EditorXml.BuildActionsXml(models);
        var parsed = EditorXml.ComputeElementLineRanges(xml);

        // <Actions>        1
        //   <Action A/>    2
        //   <!-- about B --> 3
        //   <Action B/>    4
        // </Actions>       5
        Assert.Equal((2, 2), built[0]);
        Assert.Equal((3, 4), built[1]);
        Assert.Equal(built, parsed);

        // The comment line belongs to B, not to A and not to nothing.
        Assert.Equal(1, EditorXml.IndexOfElementAtLine(parsed, 3));
    }

    [Fact]
    public void ACommentBlockIsFullyInsideItsActionsRange()
    {
        var models = new List<ActionNodeModel> { Action("A"), Action("B", "one\ntwo") };

        var (xml, built) = EditorXml.BuildActionsXml(models);
        var parsed = EditorXml.ComputeElementLineRanges(xml);

        Assert.Equal(built, parsed);

        // <!--, one, two, --> then the element: every one of those lines is B's.
        var range = parsed[1];
        for (var line = range.Start; line <= range.End; line++)
            Assert.Equal(1, EditorXml.IndexOfElementAtLine(parsed, line));
    }

    [Fact]
    public void ARangeNeverSwallowsTheNextActionsComment()
    {
        var models = new List<ActionNodeModel> { Action("A", "about A"), Action("B", "about B") };

        var (_, built) = EditorXml.BuildActionsXml(models);

        // A's range must end at A's element, not run on into B's note.
        Assert.True(built[0].End < built[1].Start,
            $"A's range {built[0]} overlaps B's range {built[1]}");
    }

    // Typed XML with blank lines has no equivalent from the builder, but the
    // rule still has to hold: a comment run opens the range even across blank
    // lines, matching how ExtractNodePairs ignores whitespace.
    [Fact]
    public void BlankLinesBetweenACommentAndItsActionDoNotBreakTheRange()
    {
        var xml = string.Join("\n",
        [
            "<Actions>",                // 1
            "  <Action Type=\"A\" />",  // 2
            "",                         // 3
            "  <!-- about B -->",       // 4
            "",                         // 5
            "  <Action Type=\"B\" />",  // 6
            "</Actions>",               // 7
        ]);

        var ranges = EditorXml.ComputeElementLineRanges(xml);

        Assert.Equal((2, 2), ranges[0]);
        Assert.Equal((4, 6), ranges[1]);
        Assert.Equal(1, EditorXml.IndexOfElementAtLine(ranges, 4));
    }

    [Fact]
    public void ACommentBeforeTheFirstActionBelongsToIt()
    {
        var xml = string.Join("\n",
        [
            "<Actions>",                // 1
            "  <!-- about A -->",       // 2
            "  <Action Type=\"A\" />",  // 3
            "</Actions>",               // 4
        ]);

        var ranges = EditorXml.ComputeElementLineRanges(xml);

        Assert.Equal((2, 3), Assert.Single(ranges));
    }

    [Fact]
    public void ATrailingCommentBelongsToNothing()
    {
        // There is no action after it, so it opens no range — consistent with
        // ExtractNodePairs discarding it.
        var xml = string.Join("\n",
        [
            "<Actions>",                // 1
            "  <Action Type=\"A\" />",  // 2
            "  <!-- orphan -->",        // 3
            "</Actions>",               // 4
        ]);

        var ranges = EditorXml.ComputeElementLineRanges(xml);

        Assert.Equal((2, 2), Assert.Single(ranges));
        Assert.Equal(-1, EditorXml.IndexOfElementAtLine(ranges, 3));
    }

    // -------------------------------------------------------------------------
    // The text the builder produces
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildActionsXml_WrapsActionsAndIndentsThem()
    {
        var (xml, _) = EditorXml.BuildActionsXml([Action("A")]);

        Assert.Equal(
            string.Join(Environment.NewLine, ["<Actions>", "  <Action Type=\"A\" />", "</Actions>"]),
            xml);
    }

    [Fact]
    public void BuildActionsXml_WithNoActions_IsStillAValidDocument()
    {
        var (xml, ranges) = EditorXml.BuildActionsXml([]);

        Assert.Empty(ranges);
        var parsed = XElement.Parse(xml);
        Assert.Equal("Actions", parsed.Name.LocalName);
        Assert.Empty(parsed.Elements());
    }

    [Fact]
    public void BuildActionsXml_RoundTripsCommentsThroughExtractNodePairs()
    {
        // The strongest round trip: models -> text -> models, notes intact.
        var models = new List<ActionNodeModel>
        {
            Action("A", "first note"),
            Action("B", "second\nnote"),
            Action("C"),
        };

        var (xml, _) = EditorXml.BuildActionsXml(models);
        var pairs = EditorXml.ExtractNodePairs(XElement.Parse(xml));

        Assert.Equal(3, pairs.Count);
        Assert.Equal("first note", pairs[0].Comment);
        Assert.Equal("second\nnote", pairs[1].Comment);
        Assert.Null(pairs[2].Comment);
    }

    [Fact]
    public void BuildActionsXml_PreservesCDataBodies()
    {
        var models = new List<ActionNodeModel>
        {
            FromXml("""<Action Type="Info"><![CDATA[<b>hi</b>]]></Action>"""),
        };

        var (xml, _) = EditorXml.BuildActionsXml(models);

        Assert.Contains("<![CDATA[<b>hi</b>]]>", xml);
    }
}
