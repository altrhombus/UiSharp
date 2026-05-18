using System.Drawing;
using System.Globalization;
using System.Xml.Linq;
using UIpp.Core.Dialogs;
using UIpp.Core.Software;

namespace UIpp.Core.Configuration;

public static class ConfigLoader
{
    // Synchronous load from a local file path.
    public static LoadedConfig Load(string path) =>
        ParseXml(File.ReadAllText(path), path);

    // Parse from an in-memory XML string (e.g. wizard-generated templates).
    public static LoadedConfig LoadFromXml(string xml) =>
        ParseXml(xml, "template");

    // Async load — handles both local paths and http(s):// URLs.
    // On download failure, falls back to fallbackPath if provided.
    // Retries up to maxRetries times with a 5-second delay between attempts.
    public static async Task<LoadedConfig> LoadAsync(
        string path,
        string? fallbackPath  = null,
        int    maxRetries     = 3,
        CancellationToken ct  = default)
    {
        string xml;
        if (IsHttpUrl(path))
            xml = await DownloadWithRetryAsync(path, fallbackPath, maxRetries, ct);
        else
            xml = File.ReadAllText(path);

        return ParseXml(xml, path);
    }

    // -------------------------------------------------------------------------

    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    private static bool IsHttpUrl(string path) =>
        path.StartsWith("http://",  StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static async Task<string> DownloadWithRetryAsync(
        string url,
        string? fallbackPath,
        int maxRetries,
        CancellationToken ct)
    {

        Exception? lastEx = null;
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await _httpClient.GetStringAsync(url, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                lastEx = ex;
                if (attempt < maxRetries)
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackPath) && File.Exists(fallbackPath))
            return File.ReadAllText(fallbackPath);

        throw new InvalidOperationException(
            $"Failed to download config from '{url}' after {maxRetries + 1} attempt(s).", lastEx);
    }

    // -------------------------------------------------------------------------

    private static LoadedConfig ParseXml(string rawXml, string sourceName)
    {
        var doc  = XDocument.Parse(EscapeAttributeLt(rawXml));
        var root = doc.Root
            ?? throw new InvalidOperationException($"Config '{sourceName}' has no root element.");

        if (!root.Name.LocalName.Equals(XmlConstants.Elements.Root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Root element must be '{XmlConstants.Elements.Root}', found '{root.Name.LocalName}'.");

        var traits          = ReadDialogTraits(root);
        var software        = ReadSoftware(root);
        var messages        = root.Element(XmlConstants.Elements.Messages);
        var conditionEngine = Attr(root, XmlConstants.Attributes.ConditionEngine)
                              ?? XmlConstants.Values.ConditionEngineNative;
        var schemaVersion   = int.TryParse(
                                  Attr(root, XmlConstants.Attributes.SchemaVersion),
                                  out var sv) ? sv : (int?)null;

        return new LoadedConfig(doc, traits, software, conditionEngine, schemaVersion, messages);
    }

    // -------------------------------------------------------------------------

    private static DialogTraits ReadDialogTraits(XElement root)
    {
        var flags = DialogTraitFlags.None;
        if (ParseBool(Attr(root, XmlConstants.Attributes.DialogIcons),  defaultValue: true))  flags |= DialogTraitFlags.ShowIcons;
        if (ParseBool(Attr(root, XmlConstants.Attributes.DialogSidebar), defaultValue: true)) flags |= DialogTraitFlags.ShowSidebar;
        if (ParseBool(Attr(root, XmlConstants.Attributes.AlwaysOnTop),  defaultValue: true))  flags |= DialogTraitFlags.AlwaysOnTop;
        if (ParseBool(Attr(root, XmlConstants.Attributes.Flat),         defaultValue: false)) flags |= DialogTraitFlags.Flat;
        flags |= DialogTraitFlags.AllowVarEditor;

        return new DialogTraits
        {
            Title            = Attr(root, XmlConstants.Attributes.Title)           ?? "UI++",
            Subtitle         = Attr(root, XmlConstants.Attributes.Subtitle)        ?? string.Empty,
            FontFace         = Attr(root, XmlConstants.Attributes.Font)            ?? XmlConstants.DefaultFontFace,
            IconPath         = Attr(root, XmlConstants.Attributes.Icon),
            AccentColor      = ParseHexColor(Attr(root, XmlConstants.Attributes.Color),            Color.FromArgb(0x00, 0x21, 0x47)),
            SidebarTextColor = ParseHexColor(Attr(root, XmlConstants.Attributes.SidebarTextColor), Color.White),
            TextColor        = ParseHexColor(Attr(root, XmlConstants.Attributes.TextColor),        Color.Black),
            Flags            = flags,
        };
    }

    private static IReadOnlyDictionary<string, ISoftware> ReadSoftware(XElement root)
    {
        var softwareNode = root.Element(XmlConstants.Elements.Software);
        if (softwareNode is null)
            return new Dictionary<string, ISoftware>();

        var result = new Dictionary<string, ISoftware>(StringComparer.OrdinalIgnoreCase);
        int order  = 0;

        foreach (var node in softwareNode.Elements())
        {
            var localName = node.Name.LocalName;
            var id        = Attr(node, XmlConstants.Attributes.Id);
            if (string.IsNullOrWhiteSpace(id)) continue;

            var label      = Attr(node, XmlConstants.Attributes.Label)       ?? id;
            var info       = Attr(node, XmlConstants.Attributes.SoftwareInfo) ?? string.Empty;
            var includeIds = Attr(node, XmlConstants.Attributes.IncludeId)   ?? string.Empty;
            var excludeIds = Attr(node, XmlConstants.Attributes.ExcludeId)   ?? string.Empty;

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
    //
    // The inner loop has two branches guarded by quoteChar == '\0':
    //   Outside quotes: '>' ends the tag (break); quote chars open a quoted section.
    //   Inside quotes:  '<' is escaped to &lt;; the closing quote char exits the section.
    // A '>' that appears inside a quoted value (e.g. Condition="A &gt; B") does NOT hit
    // the break — it falls through to the final else{Append} branch instead.
    private static string EscapeAttributeLt(string xml)
    {
        var sb = new System.Text.StringBuilder(xml.Length);
        int i  = 0;

        while (i < xml.Length)
        {
            char c = xml[i];
            sb.Append(c);
            i++;

            if (c != '<') continue;

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
