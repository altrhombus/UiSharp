using System.Drawing;
using System.Globalization;
using System.Xml.Linq;
using UIpp.Core.Dialogs;
using UIpp.Core.Software;

namespace UIpp.Core.Configuration;

public static class ConfigLoader
{
    public static LoadedConfig Load(string path)
    {
        var rawXml = File.ReadAllText(path);
        var doc = XDocument.Parse(EscapeAttributeLt(rawXml));
        var root = doc.Root
            ?? throw new InvalidOperationException($"XML file '{path}' has no root element.");

        if (!root.Name.LocalName.Equals(XmlConstants.Elements.Root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Root element must be '{XmlConstants.Elements.Root}', found '{root.Name.LocalName}'.");

        var traits         = ReadDialogTraits(root);
        var software       = ReadSoftware(root);
        var conditionEngine = Attr(root, XmlConstants.Attributes.ConditionEngine)
                              ?? XmlConstants.Values.ConditionEngineNative;
        var schemaVersion  = int.TryParse(
                                 Attr(root, XmlConstants.Attributes.SchemaVersion),
                                 out var sv) ? sv : (int?)null;

        return new LoadedConfig(doc, traits, software, conditionEngine, schemaVersion);
    }

    // -------------------------------------------------------------------------

    private static DialogTraits ReadDialogTraits(XElement root)
    {
        // Build flags from XML attrs; Back/Refresh are set dynamically by ActionProcessor.
        var flags = DialogTraitFlags.None;
        if (ParseBool(Attr(root, XmlConstants.Attributes.DialogIcons),  defaultValue: true))  flags |= DialogTraitFlags.ShowIcons;
        if (ParseBool(Attr(root, XmlConstants.Attributes.DialogSidebar), defaultValue: true)) flags |= DialogTraitFlags.ShowSidebar;
        if (ParseBool(Attr(root, XmlConstants.Attributes.AlwaysOnTop),  defaultValue: true))  flags |= DialogTraitFlags.AlwaysOnTop;
        if (ParseBool(Attr(root, XmlConstants.Attributes.Flat),         defaultValue: false)) flags |= DialogTraitFlags.Flat;
        flags |= DialogTraitFlags.AllowVarEditor;

        return new DialogTraits
        {
            Title           = Attr(root, XmlConstants.Attributes.Title)           ?? "UI++",
            Subtitle        = Attr(root, XmlConstants.Attributes.Subtitle)        ?? string.Empty,
            FontFace        = Attr(root, XmlConstants.Attributes.Font)            ?? XmlConstants.DefaultFontFace,
            IconPath        = Attr(root, XmlConstants.Attributes.Icon),
            AccentColor     = ParseHexColor(Attr(root, XmlConstants.Attributes.Color),            Color.FromArgb(0x00, 0x21, 0x47)),
            SidebarTextColor= ParseHexColor(Attr(root, XmlConstants.Attributes.SidebarTextColor), Color.White),
            Flags           = flags,
        };
    }

    private static IReadOnlyDictionary<string, ISoftware> ReadSoftware(XElement root)
    {
        var softwareNode = root.Element(XmlConstants.Elements.Software);
        if (softwareNode is null)
            return new Dictionary<string, ISoftware>();

        var result = new Dictionary<string, ISoftware>(StringComparer.OrdinalIgnoreCase);
        int order = 0;

        foreach (var node in softwareNode.Elements())
        {
            var localName = node.Name.LocalName;
            var id        = Attr(node, XmlConstants.Attributes.Id);
            if (string.IsNullOrWhiteSpace(id)) continue;

            var label      = Attr(node, XmlConstants.Attributes.Label)      ?? id;
            var info       = Attr(node, XmlConstants.Attributes.SoftwareInfo) ?? string.Empty;
            var includeIds = Attr(node, XmlConstants.Attributes.IncludeId)  ?? string.Empty;
            var excludeIds = Attr(node, XmlConstants.Attributes.ExcludeId)  ?? string.Empty;

            ISoftware? sw = null;

            if (localName.Equals(XmlConstants.Elements.Application, StringComparison.OrdinalIgnoreCase))
            {
                var appName = Attr(node, XmlConstants.Attributes.AppName) ?? string.Empty;
                sw = new Application(id, label, info, appName, includeIds, excludeIds, order);
            }
            else if (localName.Equals(XmlConstants.Elements.Package, StringComparison.OrdinalIgnoreCase))
            {
                var pkgId    = Attr(node, XmlConstants.Attributes.PkgId)       ?? string.Empty;
                var progName = Attr(node, XmlConstants.Attributes.ProgramName) ?? string.Empty;
                sw = new Package(id, label, info, pkgId, progName, includeIds, excludeIds, order);
            }

            if (sw is not null)
                result[id] = sw;

            order++;
        }

        return result;
    }

    // -------------------------------------------------------------------------
    // Helpers

    private static string? Attr(XElement el, string name) =>
        (string?)el.Attribute(name);

    private static Color ParseHexColor(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        hex = hex.TrimStart('#');
        if (hex.Length == 6 &&
            uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            return Color.FromArgb(0xFF, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        }
        return fallback;
    }

    private static bool ParseBool(string? value, bool defaultValue = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        return value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes",  StringComparison.OrdinalIgnoreCase)
            || value.Equals("1",    StringComparison.Ordinal);
    }

    // UI++ config files commonly contain unescaped < in Condition attribute values
    // (e.g. VBScript operators: <>, <=). This walks the raw text character by character,
    // escaping < only when inside a quoted attribute value, leaving tag structure intact.
    private static string EscapeAttributeLt(string xml)
    {
        var sb = new System.Text.StringBuilder(xml.Length);
        int i = 0;

        while (i < xml.Length)
        {
            char c = xml[i];
            sb.Append(c);
            i++;

            if (c != '<') continue;

            // Scan inside the tag, escaping < only within quoted attribute values.
            char quoteChar = '\0';
            while (i < xml.Length)
            {
                char t = xml[i];
                i++;

                if (quoteChar == '\0')
                {
                    sb.Append(t);
                    if (t == '>') break;
                    if (t is '\'' or '"') quoteChar = t;
                }
                else if (t == quoteChar)
                {
                    sb.Append(t);
                    quoteChar = '\0';
                }
                else if (t == '<')
                {
                    sb.Append("&lt;");
                }
                else
                {
                    sb.Append(t);
                }
            }
        }

        return sb.ToString();
    }

}
