using System.Text;
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
public static class EditorXml
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
    /// Renders actions to the text shown in the XML pane, and reports the
    /// 1-based line range each one occupies.
    ///
    /// This is the outbound half of the editor's sync;
    /// <see cref="ComputeElementLineRanges"/> is the inbound half. The two must
    /// agree on every range, or clicking a line selects a different action
    /// depending on which pane was edited last — which is exactly what happened
    /// while these lived apart. <c>BuildAndParseAgree</c> in the tests pins it.
    /// </summary>
    /// <returns>
    /// The XML text, and one range per action in document order. A range starts
    /// at the action's comment block, not at its element — see
    /// <see cref="ComputeElementLineRanges"/> for why.
    /// </returns>
    public static (string Xml, IReadOnlyList<(int Start, int End)> Ranges) BuildActionsXml(
        IReadOnlyList<ActionNodeModel> models, string indent = "  ") =>
        BuildDocument(
            XmlConstants.Elements.Actions,
            models.Select(m => (m.Comment, m.Node)),
            indent);

    /// <summary>
    /// Renders a container element holding commented children, and reports the
    /// 1-based line range each child occupies.
    ///
    /// Both editor panes are this same shape — <c>&lt;Actions&gt;</c> holding
    /// actions and <c>&lt;Software&gt;</c> holding packages and applications —
    /// so they share one implementation rather than two that drift apart.
    /// </summary>
    public static (string Xml, IReadOnlyList<(int Start, int End)> Ranges) BuildDocument(
        string rootElementName,
        IEnumerable<(string? Comment, XElement Element)> items,
        string indent = "  ")
    {
        var sb = new StringBuilder();
        var ranges = new List<(int Start, int End)>();

        sb.AppendLine($"<{rootElementName}>");
        var line = 2;

        foreach (var (comment, element) in items)
        {
            // The range opens at the comment, so clicking anywhere in an item's
            // note selects that item.
            var start = line;

            if (!string.IsNullOrWhiteSpace(comment))
                line = AppendComment(sb, indent, comment, line);

            foreach (var raw in element.ToString().Split('\n'))
            {
                sb.Append(indent);
                sb.AppendLine(raw.TrimEnd('\r'));
                line++;
            }

            ranges.Add((start, line - 1));
        }

        sb.Append($"</{rootElementName}>");
        return (sb.ToString(), ranges);
    }

    private static int AppendComment(StringBuilder sb, string indent, string comment, int line)
    {
        // A single-line note stays on one line; a multi-line note gets a block
        // so the text is not mangled by re-reading it.
        if (!comment.Contains('\n'))
        {
            sb.Append(indent);
            sb.AppendLine($"<!-- {comment.Trim()} -->");
            return line + 1;
        }

        sb.Append(indent);
        sb.AppendLine("<!--");
        line++;

        foreach (var commentLine in comment.Split('\n'))
        {
            sb.Append(indent);
            sb.Append("  ");
            sb.AppendLine(commentLine);
            line++;
        }

        sb.Append(indent);
        sb.AppendLine("-->");
        return line + 1;
    }

    /// <summary>
    /// Maps each top-level action in an <c>&lt;Actions&gt;</c> document to the
    /// 1-based line range it occupies, so selecting in the text editor can
    /// highlight the matching action and vice versa.
    ///
    /// A range starts at the action's preceding comment rather than at the
    /// element, because <see cref="ExtractNodePairs"/> attaches a comment to the
    /// element that follows it — the note belongs to that action, so clicking it
    /// must select that action. <see cref="BuildActionsXml"/> emits ranges the
    /// same way; the two directions previously disagreed, so which action a
    /// click selected depended on which pane had been edited last.
    ///
    /// A range never extends forward to the next element, which would swallow
    /// the following action's note.
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

        int? pendingCommentLine = null;

        foreach (var node in root.Nodes())
        {
            switch (node)
            {
                case XComment comment:
                    // Only the first of a run of comments opens the range, and
                    // whitespace between them does not break the run — matching
                    // how ExtractNodePairs joins them into one note.
                    pendingCommentLine ??= LineOf(comment);
                    break;

                case XElement el:
                {
                    var elementLine = LineOf(el);
                    var lineCount = el.ToString().Split('\n').Length;

                    ranges.Add((pendingCommentLine ?? elementLine, elementLine + lineCount - 1));
                    pendingCommentLine = null;
                    break;
                }
            }
        }

        return ranges;
    }

    private static int LineOf(XObject node)
    {
        var info = (IXmlLineInfo)node;
        return info.HasLineInfo() ? info.LineNumber : 1;
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
