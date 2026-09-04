using System.Drawing;
using System.Xml.Linq;
using UiSharp.Core.Dialogs;
using UiSharp.Core.Configuration;
using UiSharp.Core.Software;

namespace UiSharp.Editing;

public static class ConfigWriter
{
    public static XDocument Write(EditorConfig config)
    {
        var root = new XElement(XmlConstants.Elements.Root);

        SetRootAttributes(root, config);

        if (config.SoftwareList.Count > 0)
            root.Add(BuildSoftwareElement(config.SoftwareList, config.SoftwareComments));

        if (config.MessagesElement is not null)
            root.Add(new XElement(config.MessagesElement));

        var actionsEl = new XElement(XmlConstants.Elements.Actions);
        foreach (var model in config.Actions)
        {
            if (!string.IsNullOrWhiteSpace(model.Comment))
                actionsEl.Add(BuildXComment(model.Comment));
            actionsEl.Add(BuildActionNode(model));
        }
        root.Add(actionsEl);

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null));
        if (!string.IsNullOrWhiteSpace(config.DocumentComment))
            doc.Add(BuildXComment(config.DocumentComment));
        doc.Add(root);
        return doc;
    }

    public static void Save(EditorConfig config, string path) =>
        Write(config).Save(path);

    // -------------------------------------------------------------------------

    private static void SetRootAttributes(XElement root, EditorConfig config)
    {
        var t = config.GlobalTraits;

        root.SetAttributeValue(XmlConstants.Attributes.Title, t.Title);

        if (!string.IsNullOrEmpty(t.Subtitle))
            root.SetAttributeValue(XmlConstants.Attributes.Subtitle, t.Subtitle);

        if (!string.IsNullOrEmpty(t.FontFace) && t.FontFace != XmlConstants.DefaultFontFace)
            root.SetAttributeValue(XmlConstants.Attributes.Font, t.FontFace);

        if (!string.IsNullOrEmpty(t.IconPath))
            root.SetAttributeValue(XmlConstants.Attributes.Icon, t.IconPath);

        root.SetAttributeValue(XmlConstants.Attributes.Color,         ToHex(t.AccentColor));
        root.SetAttributeValue(XmlConstants.Attributes.SidebarTextColor, ToHex(t.SidebarTextColor));
        root.SetAttributeValue(XmlConstants.Attributes.DialogIcons,   BoolStr(t.ShowIcons));
        root.SetAttributeValue(XmlConstants.Attributes.DialogSidebar, BoolStr(t.ShowSidebar));
        root.SetAttributeValue(XmlConstants.Attributes.AlwaysOnTop,   BoolStr(t.AlwaysOnTop));
        root.SetAttributeValue(XmlConstants.Attributes.Flat,          BoolStr(t.Flat));

        if (!config.ConditionEngine.Equals(XmlConstants.Values.ConditionEngineNative, StringComparison.OrdinalIgnoreCase))
            root.SetAttributeValue(XmlConstants.Attributes.ConditionEngine, config.ConditionEngine);

        if (config.SchemaVersion is > 0)
            root.SetAttributeValue(XmlConstants.Attributes.SchemaVersion, config.SchemaVersion.Value);
    }

    private static XElement BuildSoftwareElement(
        IEnumerable<ISoftware> list,
        IReadOnlyDictionary<string, string?>? comments = null)
    {
        var sw = new XElement(XmlConstants.Elements.Software);
        foreach (var item in list.OrderBy(s => s.OrderIndex))
        {
            if (comments is not null &&
                comments.TryGetValue(item.Id, out var comment) &&
                !string.IsNullOrEmpty(comment))
            {
                sw.Add(BuildXComment(comment));
            }
            sw.Add(BuildSoftwareItem(item));
        }
        return sw;
    }

    private static XComment BuildXComment(string comment) =>
        comment.Contains('\n')
            ? new XComment("\n  " + comment.Replace("\n", "\n  ") + "\n  ")
            : new XComment($" {comment.Trim()} ");

    private static XElement BuildSoftwareItem(ISoftware item)
    {
        XElement el;

        if (item is Application app)
        {
            el = new XElement(XmlConstants.Elements.Application);
            el.SetAttributeValue(XmlConstants.Attributes.Id,      app.Id);
            el.SetAttributeValue(XmlConstants.Attributes.Label,   app.Label);
            el.SetAttributeValue(XmlConstants.Attributes.AppName, app.AppName);
        }
        else if (item is Package pkg)
        {
            el = new XElement(XmlConstants.Elements.Package);
            el.SetAttributeValue(XmlConstants.Attributes.Id,          pkg.Id);
            el.SetAttributeValue(XmlConstants.Attributes.Label,       pkg.Label);
            el.SetAttributeValue(XmlConstants.Attributes.PkgId,       pkg.PkgId);
            el.SetAttributeValue(XmlConstants.Attributes.ProgramName, pkg.ProgramName);
        }
        else
        {
            el = new XElement(item.Type);
            el.SetAttributeValue(XmlConstants.Attributes.Id,    item.Id);
            el.SetAttributeValue(XmlConstants.Attributes.Label, item.Label);
        }

        if (!string.IsNullOrEmpty(item.Info))
            el.SetAttributeValue(XmlConstants.Attributes.SoftwareInfo, item.Info);
        if (!string.IsNullOrEmpty(item.IncludeIds))
            el.SetAttributeValue(XmlConstants.Attributes.IncludeId, item.IncludeIds);
        if (!string.IsNullOrEmpty(item.ExcludeIds))
            el.SetAttributeValue(XmlConstants.Attributes.ExcludeId, item.ExcludeIds);

        return el;
    }

    private static XElement BuildActionNode(ActionNodeModel model)
    {
        if (!model.IsGroup)
            return new XElement(model.Node);

        // Rebuild group element: clone attributes, then recurse into Children list.
        var group = new XElement(model.Node.Name);
        foreach (var attr in model.Node.Attributes())
            group.Add(new XAttribute(attr));

        foreach (var child in model.Children)
            group.Add(BuildActionNode(child));

        return group;
    }

    // -------------------------------------------------------------------------
    // Helpers

    private static string ToHex(Color c) =>
        $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private static string BoolStr(bool value) =>
        value ? XmlConstants.Values.True : XmlConstants.Values.False;
}
