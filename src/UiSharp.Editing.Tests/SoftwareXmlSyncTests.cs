using System.Xml.Linq;
using UiSharp.Core.Configuration;
using UiSharp.Editing;

namespace UiSharp.Editing.Tests;

/// <summary>
/// The Software pane is the same shape as the Actions pane — a container
/// element holding children, each optionally preceded by a comment — and it had
/// the same defect: its outbound builder counted an item's comment as part of
/// that item's line range while its inbound parser did not, so clicking a
/// comment selected a different item depending on which pane was edited last.
///
/// Both panes now render and parse through EditorXml, so the agreement is one
/// guarantee rather than two that drift apart. These tests assert it holds for
/// the Software document shape specifically.
/// </summary>
public class SoftwareXmlSyncTests
{
    private static (string? Comment, XElement Element) Package(
        string id, string label, string? comment = null) =>
        (comment, XElement.Parse(
            $"""<Package Id="{id}" Label="{label}" PkgID="ONE00010" ProgramName="Install" />"""));

    private static (string? Comment, XElement Element) Application(
        string id, string label, string? comment = null) =>
        (comment, XElement.Parse($"""<Application Id="{id}" Label="{label}" Name="{label}" />"""));

    private static (string Xml, IReadOnlyList<(int Start, int End)> Ranges) Build(
        params (string? Comment, XElement Element)[] items) =>
        EditorXml.BuildDocument(XmlConstants.Elements.Software, items);

    // -------------------------------------------------------------------------
    // The agreement property, for Software
    // -------------------------------------------------------------------------

    public static IEnumerable<object[]> Catalogues()
    {
        yield return new object[] { new[] { Package("p1", ".NET 4.8"), Application("a1", "Reader") } };

        // A note on the second item — the case that used to select the first.
        yield return new object[]
        {
            new[] { Package("p1", ".NET 4.8"), Application("a1", "Reader", "licensed separately") }
        };

        // Notes on everything.
        yield return new object[]
        {
            new[]
            {
                Package("p1", ".NET 4.8", "required by the imaging step"),
                Application("a1", "Reader", "licensed separately"),
                Application("a2", "Office", "takes a while"),
            }
        };

        // A multi-line note, emitted as a comment block.
        yield return new object[]
        {
            new[] { Application("a1", "Reader", "line one\nline two"), Package("p1", "IE11") }
        };

        yield return new object[] { new[] { Package("only", "Solo", "just one") } };

        yield return new object[] { Array.Empty<(string? Comment, XElement Element)>() };
    }

    [Theory]
    [MemberData(nameof(Catalogues))]
    public void BuildAndParseAgree((string? Comment, XElement Element)[] items)
    {
        var (xml, built) = Build(items);
        var parsed = EditorXml.ComputeElementLineRanges(xml);

        Assert.Equal(built.Count, parsed.Count);

        for (var i = 0; i < built.Count; i++)
            Assert.Equal(built[i], parsed[i]);
    }

    [Theory]
    [MemberData(nameof(Catalogues))]
    public void EveryClaimedLineSelectsTheSameItemBothWays((string? Comment, XElement Element)[] items)
    {
        var (xml, built) = Build(items);
        var parsed = EditorXml.ComputeElementLineRanges(xml);

        var lineCount = xml.Split('\n').Length;

        for (var line = 1; line <= lineCount; line++)
        {
            Assert.Equal(
                EditorXml.IndexOfElementAtLine(built, line),
                EditorXml.IndexOfElementAtLine(parsed, line));
        }
    }

    [Fact]
    public void ClickingACommentSelectsTheItemBelowIt()
    {
        var (xml, built) = Build(
            Package("p1", ".NET 4.8"),
            Application("a1", "Reader", "licensed separately"));

        var parsed = EditorXml.ComputeElementLineRanges(xml);

        // <Software>    1
        //   <Package/>  2
        //   <!-- ... --> 3
        //   <Application/> 4
        // </Software>   5
        Assert.Equal((2, 2), built[0]);
        Assert.Equal((3, 4), built[1]);
        Assert.Equal(built, parsed);

        Assert.Equal(1, EditorXml.IndexOfElementAtLine(parsed, 3));
    }

    // -------------------------------------------------------------------------
    // The document the builder produces
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildDocument_UsesTheSoftwareRootElement()
    {
        var (xml, _) = Build(Package("p1", "X"));

        var root = XElement.Parse(xml);
        Assert.Equal("Software", root.Name.LocalName);
        Assert.Equal("Package", Assert.Single(root.Elements()).Name.LocalName);
    }

    [Fact]
    public void BuildDocument_WithNoItems_IsStillAValidDocument()
    {
        var (xml, ranges) = Build();

        Assert.Empty(ranges);
        Assert.Empty(XElement.Parse(xml).Elements());
    }

    [Fact]
    public void BuildDocument_RoundTripsCommentsThroughExtractNodePairs()
    {
        var (xml, _) = Build(
            Package("p1", ".NET 4.8", "required by the imaging step"),
            Application("a1", "Reader"),
            Application("a2", "Office", "takes a\nwhile"));

        var pairs = EditorXml.ExtractNodePairs(XElement.Parse(xml));

        Assert.Equal(3, pairs.Count);
        Assert.Equal("required by the imaging step", pairs[0].Comment);
        Assert.Null(pairs[1].Comment);
        Assert.Equal("takes a\nwhile", pairs[2].Comment);
    }

    [Fact]
    public void BuildDocument_PreservesItemAttributes()
    {
        var (xml, _) = Build(Package("9EBF5537", ".NET Framework 4.5.2"));

        var package = Assert.Single(XElement.Parse(xml).Elements());
        Assert.Equal("9EBF5537", (string?)package.Attribute("Id"));
        Assert.Equal(".NET Framework 4.5.2", (string?)package.Attribute("Label"));
        Assert.Equal("ONE00010", (string?)package.Attribute("PkgID"));
    }

    // -------------------------------------------------------------------------
    // The shared builder is genuinely shared
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildActionsXml_AndBuildDocument_ProduceTheSameThing()
    {
        // BuildActionsXml is a thin wrapper, so a change to one pane's rendering
        // cannot silently diverge from the other's.
        var model = new ActionNodeModel
        {
            Node = XElement.Parse("""<Action Type="TSVar" Variable="X" />"""),
            Comment = "a note",
        };

        var viaActions = EditorXml.BuildActionsXml([model]);
        var viaDocument = EditorXml.BuildDocument(
            XmlConstants.Elements.Actions, [(model.Comment, model.Node)]);

        Assert.Equal(viaActions.Xml, viaDocument.Xml);
        Assert.Equal(viaActions.Ranges, viaDocument.Ranges);
    }
}
