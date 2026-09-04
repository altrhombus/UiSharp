using System.Xml;
using System.Xml.Linq;
using UiSharp.Core.Configuration;

namespace UiSharp.Editing;

/// <summary>
/// The XML side of the editor's bidirectional sync, separated from the WinUI
/// view models so it can be tested.
///
/// This is the most intricate logic in the editor and historically its most
/// bug-prone: comments being dropped, keystrokes lost, and clicks selecting the
/// wrong action all traced back here. None of it was reachable by a test while
/// it lived inside a ViewModel in an assembly with no test project.
/// </summary>
public static class ActionXml
{
    /// <summary>
    /// Pairs each child element with the XML comment(s) immediately preceding
    /// it, which is how the editor keeps an author's notes attached to the
    /// action they describe across a load/save round trip.
    ///
    /// Whitespace text nodes between elements are skipped. Consecutive comments
    /// are joined with newlines and treated as one note.
    /// </summary>
    public static List<(string? Comment, XElement Element)> ExtractNodePairs(XElement parent)
    {
        var result = new List<(string? Comment, XElement Element)>();
        string? pending = null;

        foreach (var node in parent.Nodes())
        {
            if (node is XComment comment)
            {
                var normalized = NormalizeComment(comment.Value);
                pending = pending is null ? normalized : pending + "\n" + normalized;
            }
            else if (node is XElement el)
            {
                result.Add((pending, el));
                pending = null;
            }
        }

        return result;
    }

    /// <summary>
    /// Strips outer whitespace and per-line indentation from a comment's raw
    /// value, so an editor field shows clean text rather than the leading
    /// spaces and newlines a block-style comment carries.
    /// </summary>
    public static string NormalizeComment(string rawValue)
    {
        var lines = rawValue
            .Split('\n')
            .Select(l => l.Trim())
            .SkipWhile(string.IsNullOrEmpty)
            .ToList();

        while (lines.Count > 0 && string.IsNullOrEmpty(lines[^1]))
            lines.RemoveAt(lines.Count - 1);

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Builds the editor's action model from an element, recursing into
    /// ActionGroup children and carrying each child's preceding comment.
    /// </summary>
    public static ActionNodeModel BuildModel(XElement element)
    {
        var model = new ActionNodeModel { Node = element };

        if (model.IsGroup)
        {
            foreach (var (comment, child) in ExtractNodePairs(element))
            {
                var childModel = BuildModel(child);
                childModel.Comment = comment;
                model.Children.Add(childModel);
            }
        }

        return model;
    }

    /// <summary>
    /// Replaces <paramref name="target"/>'s name, attributes and content with
    /// those of <paramref name="parsed"/>, in place.
    ///
    /// Editing in place matters: the model holds a reference to the target
    /// element, so replacing the object would silently detach every view model
    /// still pointing at it.
    /// </summary>
    public static void ApplyParsedNode(XElement target, XElement parsed)
    {
        target.Name = parsed.Name;
        target.RemoveAll();

        foreach (var attr in parsed.Attributes())
            target.Add(new XAttribute(attr));

        foreach (var child in parsed.Nodes())
            target.Add(CloneNode(child));
    }

    /// <summary>
    /// Copies a node, preserving its kind. CDATA in particular must stay CDATA:
    /// Info action bodies rely on it, and a copy that degraded to text would
    /// escape the markup on the next save.
    /// </summary>
    public static XNode CloneNode(XNode node) => node switch
    {
        XElement el               => new XElement(el),
        XCData cd                 => new XCData(cd.Value),
        XText txt                 => new XText(txt.Value),
        XComment c                => new XComment(c.Value),
        XProcessingInstruction pi => new XProcessingInstruction(pi.Target, pi.Data),
        _                         => new XText(node.ToString()),
    };

    /// <summary>
    /// Maps each top-level action in an <c>&lt;Actions&gt;</c> document to the
    /// 1-based line range it occupies, so selecting in the text editor can
    /// highlight the matching action and vice versa.
    ///
    /// A range covers only the element's own lines. Extending it to the next
    /// element's start would pull the comment lines between two actions into the
    /// preceding action's range, so clicking such a comment selected the wrong
    /// action and triggered a refresh that dropped comments not yet stored on a
    /// model.
    /// </summary>
    /// <returns>
    /// One range per top-level element, in document order. Empty when the text
    /// is not parseable — a half-typed document is normal while editing, not an
    /// error worth surfacing.
    /// </returns>
    public static IReadOnlyList<(int Start, int End)> ComputeElementLineRanges(string xml)
    {
        var ranges = new List<(int Start, int End)>();

        if (string.IsNullOrWhiteSpace(xml)) return ranges;

        XElement root;
        try
        {
            root = XElement.Parse(xml, LoadOptions.SetLineInfo);
        }
        catch (XmlException)
        {
            return ranges;
        }

        foreach (var el in root.Elements())
        {
            var info = (IXmlLineInfo)el;
            var start = info.HasLineInfo() ? info.LineNumber : 1;
            var lineCount = el.ToString().Split('\n').Length;

            ranges.Add((start, start + lineCount - 1));
        }

        return ranges;
    }

    /// <summary>
    /// The index of the element whose line range contains
    /// <paramref name="line"/>, or -1 when no element does — which is the case
    /// for the comment and blank lines that sit between actions.
    /// </summary>
    public static int IndexOfElementAtLine(IReadOnlyList<(int Start, int End)> ranges, int line)
    {
        for (var i = 0; i < ranges.Count; i++)
            if (line >= ranges[i].Start && line <= ranges[i].End)
                return i;

        return -1;
    }
}
