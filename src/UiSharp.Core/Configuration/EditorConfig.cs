using System.Xml.Linq;
using UiSharp.Core.Dialogs;
using UiSharp.Core.Software;

namespace UiSharp.Core.Configuration;

public sealed class EditorConfig
{
    public DialogTraits   GlobalTraits    { get; init; } = new();
    public string         ConditionEngine { get; init; } = XmlConstants.Values.ConditionEngineNative;
    public int?           SchemaVersion   { get; init; }
    public List<ISoftware> SoftwareList   { get; init; } = [];
    /// <summary>The original &lt;Software&gt; XElement from the loaded document, used to attach leading XML comments to items on load.</summary>
    public XElement?      SoftwareElement { get; init; }
    /// <summary>XML comment text keyed by software item Id, emitted before each item when saving.</summary>
    public IReadOnlyDictionary<string, string?> SoftwareComments { get; init; } = new Dictionary<string, string?>();
    /// <summary>Text of any XML comment node(s) that appear before the root element in the document.</summary>
    public string?        DocumentComment { get; init; }
    public XElement?      MessagesElement { get; init; }
    public List<ActionNodeModel> Actions  { get; init; } = [];

    public static EditorConfig FromLoaded(LoadedConfig loaded)
    {
        var actionsRoot     = loaded.Document.Root?.Element(XmlConstants.Elements.Actions);
        var softwareElement = loaded.Document.Root?.Element(XmlConstants.Elements.Software);
        var actions = actionsRoot is null
            ? []
            : BuildActionModels(actionsRoot.Elements());

        // Collect any XComment nodes that appear before the root element.
        string? documentComment = null;
        foreach (var node in loaded.Document.Nodes())
        {
            if (node is XComment c)
            {
                var normalized = NormalizeComment(c.Value);
                documentComment = documentComment is null ? normalized : documentComment + "\n" + normalized;
            }
            else if (node is XElement)
                break;
        }

        return new EditorConfig
        {
            GlobalTraits    = loaded.GlobalTraits,
            ConditionEngine = loaded.ConditionEngine,
            SchemaVersion   = loaded.SchemaVersion,
            SoftwareList    = [.. loaded.Software.Values.OrderBy(s => s.OrderIndex)],
            SoftwareElement = softwareElement,
            DocumentComment = documentComment,
            MessagesElement = loaded.Document.Root?.Element(XmlConstants.Elements.Messages),
            Actions         = actions,
        };
    }

    private static string NormalizeComment(string rawValue)
    {
        var lines = rawValue
            .Split('\n')
            .Select(static l => l.Trim())
            .SkipWhile(string.IsNullOrEmpty)
            .ToList();
        while (lines.Count > 0 && string.IsNullOrEmpty(lines[^1]))
            lines.RemoveAt(lines.Count - 1);
        return string.Join("\n", lines);
    }

    private static List<ActionNodeModel> BuildActionModels(IEnumerable<XElement> elements)
    {
        var result = new List<ActionNodeModel>();
        foreach (var el in elements)
        {
            var model = new ActionNodeModel { Node = el };
            if (model.IsGroup)
                model.Children.AddRange(BuildActionModels(el.Elements()));
            result.Add(model);
        }
        return result;
    }
}

public sealed class ActionNodeModel
{
    public required XElement Node { get; set; }

    public string TypeName =>
        (string?)Node.Attribute(XmlConstants.Attributes.Type) ?? string.Empty;

    public bool IsGroup =>
        Node.Name.LocalName.Equals(XmlConstants.Elements.ActionGroup, StringComparison.OrdinalIgnoreCase);

    public List<ActionNodeModel> Children { get; init; } = [];

    /// <summary>Text of any XML comment(s) that immediately precede this action in the document.</summary>
    public string? Comment { get; set; }
}
