using System.Xml.Linq;
using UIpp.Core.Dialogs;
using UIpp.Core.Software;

namespace UIpp.Core.Configuration;

public sealed class EditorConfig
{
    public DialogTraits   GlobalTraits    { get; init; } = new();
    public string         ConditionEngine { get; init; } = XmlConstants.Values.ConditionEngineNative;
    public int?           SchemaVersion   { get; init; }
    public List<ISoftware> SoftwareList   { get; init; } = [];
    public XElement?      MessagesElement { get; init; }
    public List<ActionNodeModel> Actions  { get; init; } = [];

    public static EditorConfig FromLoaded(LoadedConfig loaded)
    {
        var actionsRoot = loaded.Document.Root?.Element(XmlConstants.Elements.Actions);
        var actions = actionsRoot is null
            ? []
            : BuildActionModels(actionsRoot.Elements());

        return new EditorConfig
        {
            GlobalTraits    = loaded.GlobalTraits,
            ConditionEngine = loaded.ConditionEngine,
            SchemaVersion   = loaded.SchemaVersion,
            SoftwareList    = [.. loaded.Software.Values.OrderBy(s => s.OrderIndex)],
            MessagesElement = loaded.Document.Root?.Element(XmlConstants.Elements.Messages),
            Actions         = actions,
        };
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
}
