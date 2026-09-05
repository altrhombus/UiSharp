using System.Xml.Linq;
using UiSharp.Editing;

namespace UiSharp.Editing.Tests;

/// <summary>
/// Tests for the editor's XML sync logic.
///
/// This logic previously lived inside a WinUI view model, in a project with no
/// test project, and its bug history shows the cost: comments dropped on sync,
/// keystrokes lost while typing in the XML pane, and clicks on a comment line
/// selecting the wrong action. Each of those is now expressible as a test.
/// </summary>
public class ActionXmlTests
{
    // -------------------------------------------------------------------------
    // Comment / element pairing — how author notes stay attached to actions
    // -------------------------------------------------------------------------

    [Fact]
    public void ExtractNodePairs_AttachesAPrecedingCommentToItsElement()
    {
        var root = XElement.Parse("""
            <Actions>
              <!-- name the machine -->
              <Action Type="Input" />
            </Actions>
            """);

        var pair = Assert.Single(ActionXml.ExtractNodePairs(root));

        Assert.Equal("name the machine", pair.Comment);
        Assert.Equal("Input", (string?)pair.Element.Attribute("Type"));
    }

    [Fact]
    public void ExtractNodePairs_LeavesUncommentedElementsWithNoComment()
    {
        var root = XElement.Parse("""
            <Actions>
              <Action Type="A" />
              <!-- about B -->
              <Action Type="B" />
              <Action Type="C" />
            </Actions>
            """);

        var pairs = ActionXml.ExtractNodePairs(root);

        Assert.Equal(3, pairs.Count);
        Assert.Null(pairs[0].Comment);
        Assert.Equal("about B", pairs[1].Comment);
        Assert.Null(pairs[2].Comment);
    }

    [Fact]
    public void ExtractNodePairs_JoinsConsecutiveComments()
    {
        var root = XElement.Parse("""
            <Actions>
              <!-- first line -->
              <!-- second line -->
              <Action Type="Input" />
            </Actions>
            """);

        var pair = Assert.Single(ActionXml.ExtractNodePairs(root));

        Assert.Equal("first line\nsecond line", pair.Comment);
    }

    [Fact]
    public void ExtractNodePairs_IgnoresATrailingCommentWithNoElement()
    {
        // A note typed at the end of the document belongs to nothing yet. It
        // must not crash or attach itself to the last action.
        var root = XElement.Parse("""
            <Actions>
              <Action Type="A" />
              <!-- orphan -->
            </Actions>
            """);

        var pair = Assert.Single(ActionXml.ExtractNodePairs(root));
        Assert.Null(pair.Comment);
    }

    [Fact]
    public void ExtractNodePairs_OnAnEmptyElement_ReturnsNothing() =>
        Assert.Empty(ActionXml.ExtractNodePairs(XElement.Parse("<Actions />")));

    // -------------------------------------------------------------------------
    // Comment normalisation
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(" spaced ", "spaced")]
    [InlineData("\n  indented\n", "indented")]
    [InlineData("  line one\n  line two  ", "line one\nline two")]
    // Every line is trimmed, so interior indentation goes too; only blank lines
    // *between* content survive, which is what keeps paragraph breaks in a note.
    [InlineData("\n\n  a\n\n  b\n\n", "a\n\nb")]
    [InlineData("", "")]
    public void NormalizeComment_StripsOuterWhitespaceAndIndentation(string raw, string expected) =>
        Assert.Equal(expected, ActionXml.NormalizeComment(raw).Replace("\r", ""));

    // -------------------------------------------------------------------------
    // Model construction
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildModel_OnAPlainAction_HasNoChildren()
    {
        var model = ActionXml.BuildModel(XElement.Parse("""<Action Type="TSVar" />"""));

        Assert.Equal("TSVar", model.TypeName);
        Assert.False(model.IsGroup);
        Assert.Empty(model.Children);
    }

    [Fact]
    public void BuildModel_OnAGroup_RecursesAndCarriesChildComments()
    {
        var model = ActionXml.BuildModel(XElement.Parse("""
            <ActionGroup Name="Setup">
              <!-- ask first -->
              <Action Type="Input" />
              <Action Type="TSVar" />
            </ActionGroup>
            """));

        Assert.True(model.IsGroup);
        Assert.Equal(2, model.Children.Count);
        Assert.Equal("ask first", model.Children[0].Comment);
        Assert.Equal("Input", model.Children[0].TypeName);
        Assert.Null(model.Children[1].Comment);
    }

    [Fact]
    public void BuildModel_OnNestedGroups_RecursesAllTheWayDown()
    {
        var model = ActionXml.BuildModel(XElement.Parse("""
            <ActionGroup Name="Outer">
              <ActionGroup Name="Inner">
                <Action Type="Info" />
              </ActionGroup>
            </ActionGroup>
            """));

        var inner = Assert.Single(model.Children);
        Assert.True(inner.IsGroup);
        Assert.Equal("Info", Assert.Single(inner.Children).TypeName);
    }

    // -------------------------------------------------------------------------
    // In-place node replacement
    // -------------------------------------------------------------------------

    [Fact]
    public void ApplyParsedNode_UpdatesInPlaceSoReferencesStayValid()
    {
        var target = XElement.Parse("""<Action Type="TSVar" Variable="Old">before</Action>""");
        var sameInstance = target;

        ActionXml.ApplyParsedNode(target,
            XElement.Parse("""<Action Type="TSVar" Variable="New">after</Action>"""));

        // The model holds this reference; replacing the object would detach it.
        Assert.Same(sameInstance, target);
        Assert.Equal("New", (string?)target.Attribute("Variable"));
        Assert.Equal("after", target.Value);
    }

    [Fact]
    public void ApplyParsedNode_RemovesAttributesThatAreGone()
    {
        var target = XElement.Parse("""<Action Type="Input" Title="Old" Extra="x" />""");

        ActionXml.ApplyParsedNode(target, XElement.Parse("""<Action Type="Input" />"""));

        Assert.Null(target.Attribute("Title"));
        Assert.Null(target.Attribute("Extra"));
    }

    [Fact]
    public void ApplyParsedNode_ChangesTheElementName()
    {
        var target = XElement.Parse("""<Action Type="Input" />""");

        ActionXml.ApplyParsedNode(target, XElement.Parse("""<ActionGroup Name="G" />"""));

        Assert.Equal("ActionGroup", target.Name.LocalName);
    }

    [Fact]
    public void ApplyParsedNode_PreservesCData()
    {
        // Info action bodies are CDATA. Degrading it to text would escape the
        // markup on the next save.
        var target = XElement.Parse("""<Action Type="Info" />""");

        ActionXml.ApplyParsedNode(target,
            XElement.Parse("""<Action Type="Info"><![CDATA[<b>bold</b>]]></Action>"""));

        Assert.Single(target.Nodes().OfType<XCData>());
        Assert.Equal("<b>bold</b>", target.Value);
    }

    // -------------------------------------------------------------------------
    // Node cloning
    // -------------------------------------------------------------------------

    [Fact]
    public void CloneNode_PreservesEachNodeKind()
    {
        Assert.IsType<XElement>(ActionXml.CloneNode(new XElement("a")));
        Assert.IsType<XCData>(ActionXml.CloneNode(new XCData("c")));
        Assert.IsType<XText>(ActionXml.CloneNode(new XText("t")));
        Assert.IsType<XComment>(ActionXml.CloneNode(new XComment("k")));
        Assert.IsType<XProcessingInstruction>(
            ActionXml.CloneNode(new XProcessingInstruction("t", "d")));
    }

    [Fact]
    public void CloneNode_ProducesAnIndependentCopy()
    {
        var original = new XElement("a", new XAttribute("x", "1"));
        var clone = (XElement)ActionXml.CloneNode(original);

        clone.SetAttributeValue("x", "2");

        Assert.Equal("1", (string?)original.Attribute("x"));
    }

    // -------------------------------------------------------------------------
    // Line ranges — the editor <-> text mapping
    // -------------------------------------------------------------------------

    [Fact]
    public void ComputeElementLineRanges_MapsEachActionToItsOwnLines()
    {
        var xml = string.Join("\n",
        [
            "<Actions>",            // 1
            "  <Action Type=\"A\" />",  // 2
            "  <Action Type=\"B\" />",  // 3
            "</Actions>",           // 4
        ]);

        var ranges = ActionXml.ComputeElementLineRanges(xml);

        Assert.Equal(2, ranges.Count);
        Assert.Equal((2, 2), ranges[0]);
        Assert.Equal((3, 3), ranges[1]);
    }

    [Fact]
    public void ComputeElementLineRanges_SpansMultiLineActions()
    {
        var xml = string.Join("\n",
        [
            "<Actions>",                  // 1
            "  <Action Type=\"Input\">",  // 2
            "    <TextInput Variable=\"X\" />",  // 3
            "  </Action>",                // 4
            "  <Action Type=\"B\" />",    // 5
            "</Actions>",                 // 6
        ]);

        var ranges = ActionXml.ComputeElementLineRanges(xml);

        Assert.Equal((2, 4), ranges[0]);
        Assert.Equal((5, 5), ranges[1]);
    }

    // A comment between two actions belongs to the action BELOW it, matching
    // ExtractNodePairs. It must not fall into the preceding action's range —
    // that selected the wrong action and triggered a refresh that dropped the
    // comment — nor into no range at all, which left the note unclickable.
    // ActionXmlSyncTests pins that both sync directions agree on this.
    [Fact]
    public void ComputeElementLineRanges_GivesACommentToTheActionBelowIt()
    {
        var xml = string.Join("\n",
        [
            "<Actions>",                // 1
            "  <Action Type=\"A\" />",  // 2
            "  <!-- a note -->",        // 3
            "  <Action Type=\"B\" />",  // 4
            "</Actions>",               // 5
        ]);

        var ranges = ActionXml.ComputeElementLineRanges(xml);

        Assert.Equal((2, 2), ranges[0]);
        Assert.Equal((3, 4), ranges[1]);

        Assert.Equal(0, ActionXml.IndexOfElementAtLine(ranges, 2));
        Assert.Equal(1, ActionXml.IndexOfElementAtLine(ranges, 3));
        Assert.Equal(1, ActionXml.IndexOfElementAtLine(ranges, 4));
    }

    [Fact]
    public void ComputeElementLineRanges_ExcludesBlankLinesBetweenActions()
    {
        var xml = string.Join("\n",
        [
            "<Actions>",                // 1
            "  <Action Type=\"A\" />",  // 2
            "",                         // 3
            "  <Action Type=\"B\" />",  // 4
            "</Actions>",               // 5
        ]);

        var ranges = ActionXml.ComputeElementLineRanges(xml);

        Assert.Equal(-1, ActionXml.IndexOfElementAtLine(ranges, 3));
    }

    // Half-typed XML is the normal state while editing, not an error.
    [Theory]
    [InlineData("<Actions>")]
    [InlineData("<Actions><Action Type=\"A\" ")]
    [InlineData("not xml at all")]
    [InlineData("")]
    [InlineData("   ")]
    public void ComputeElementLineRanges_OnUnparseableText_ReturnsEmpty(string xml) =>
        Assert.Empty(ActionXml.ComputeElementLineRanges(xml));

    [Fact]
    public void ComputeElementLineRanges_OnNoActions_ReturnsEmpty() =>
        Assert.Empty(ActionXml.ComputeElementLineRanges("<Actions />"));

    [Fact]
    public void IndexOfElementAtLine_OutsideEveryRange_IsMinusOne()
    {
        var ranges = ActionXml.ComputeElementLineRanges(
            "<Actions>\n  <Action Type=\"A\" />\n</Actions>");

        Assert.Equal(-1, ActionXml.IndexOfElementAtLine(ranges, 1));
        Assert.Equal(-1, ActionXml.IndexOfElementAtLine(ranges, 99));
    }

    // -------------------------------------------------------------------------
    // Round trip: the property the editor actually depends on
    // -------------------------------------------------------------------------

    [Fact]
    public void CommentsSurviveAPairExtractAndModelRoundTrip()
    {
        var root = XElement.Parse("""
            <Actions>
              <!-- step one -->
              <Action Type="Input" Title="One" />
              <!-- step two -->
              <ActionGroup Name="G">
                <!-- inner -->
                <Action Type="TSVar" />
              </ActionGroup>
            </Actions>
            """);

        var models = ActionXml.ExtractNodePairs(root)
            .Select(p =>
            {
                var m = ActionXml.BuildModel(p.Element);
                m.Comment = p.Comment;
                return m;
            })
            .ToList();

        Assert.Equal("step one", models[0].Comment);
        Assert.Equal("step two", models[1].Comment);
        Assert.Equal("inner", Assert.Single(models[1].Children).Comment);
    }
}
