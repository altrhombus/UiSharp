using System.Drawing;
using System.Globalization;
using System.Xml.Linq;
using UiSharp.Core.Dialogs;
using UiSharp.Core.Software;
using UiSharp.Core.Variables;

namespace UiSharp.Core.Configuration;

public static class ConfigLoader
{
    // Synchronous load from a local file path.
    public static LoadedConfig Load(string path, ITSEnv? env = null) =>
        ParseXml(File.ReadAllText(path), path, env);

    // Parse from an in-memory XML string (e.g. wizard-generated templates).
    public static LoadedConfig LoadFromXml(string xml, ITSEnv? env = null) =>
        ParseXml(xml, "template", env);

    // Async load — handles both local paths and http(s):// URLs.
    // On download failure, falls back to fallbackPath if provided.
    // Retries up to maxRetries times with a 5-second delay between attempts.
    public static async Task<LoadedConfig> LoadAsync(
        string path,
        string? fallbackPath  = null,
        int    maxRetries     = 3,
        CancellationToken ct  = default,
        ITSEnv? env           = null)
    {
        string xml;
        if (IsHttpUrl(path))
            xml = await DownloadWithRetryAsync(path, fallbackPath, maxRetries, ct);
        else
            xml = File.ReadAllText(path);

        return ParseXml(xml, path, env);
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

    private static LoadedConfig ParseXml(string rawXml, string sourceName, ITSEnv? env = null)
    {
        var doc  = XDocument.Parse(EscapeAttributeLt(rawXml));
        var root = doc.Root
            ?? throw new InvalidOperationException($"Config '{sourceName}' has no root element.");

        if (!root.Name.LocalName.Equals(XmlConstants.Elements.Root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Root element must be '{XmlConstants.Elements.Root}', found '{root.Name.LocalName}'.");

        var traits          = ReadDialogTraits(root, env);
        var software        = ReadSoftware(root, env);
        var messages        = root.Element(XmlConstants.Elements.Messages);
        var conditionEngine = Attr(root, env, XmlConstants.Attributes.ConditionEngine)
                              ?? XmlConstants.Values.ConditionEngineNative;
        var schemaVersion   = int.TryParse(
                                  Attr(root, env, XmlConstants.Attributes.SchemaVersion),
                                  out var sv) ? sv : (int?)null;

        return new LoadedConfig(doc, traits, software, conditionEngine, schemaVersion, messages);
    }

    // -------------------------------------------------------------------------

    private static DialogTraits ReadDialogTraits(XElement root, ITSEnv? env)
    {
        var flags = DialogTraitFlags.None;
        if (ParseBool(Attr(root, env, XmlConstants.Attributes.DialogIcons),   defaultValue: true))  flags |= DialogTraitFlags.ShowIcons;
        if (ParseBool(Attr(root, env, XmlConstants.Attributes.DialogSidebar), defaultValue: true)) flags |= DialogTraitFlags.ShowSidebar;
        if (ParseBool(Attr(root, env, XmlConstants.Attributes.AlwaysOnTop),   defaultValue: true))  flags |= DialogTraitFlags.AlwaysOnTop;
        if (ParseBool(Attr(root, env, XmlConstants.Attributes.Flat),          defaultValue: false)) flags |= DialogTraitFlags.Flat;
        flags |= DialogTraitFlags.AllowVarEditor;

        return new DialogTraits
        {
            Title            = Attr(root, env, XmlConstants.Attributes.Title)           ?? "UI++",
            Subtitle         = Attr(root, env, XmlConstants.Attributes.Subtitle)        ?? string.Empty,
            FontFace         = Attr(root, env, XmlConstants.Attributes.Font)            ?? XmlConstants.DefaultFontFace,
            IconPath         = Attr(root, env, XmlConstants.Attributes.Icon),
            AccentColor      = ParseHexColor(Attr(root, env, XmlConstants.Attributes.Color),            Color.FromArgb(0x00, 0x21, 0x47)),
            SidebarTextColor = ParseHexColor(Attr(root, env, XmlConstants.Attributes.SidebarTextColor), Color.White),
            TextColor        = ParseHexColor(Attr(root, env, XmlConstants.Attributes.TextColor),        Color.Black),
            Flags            = flags,
        };
    }

    private static IReadOnlyDictionary<string, ISoftware> ReadSoftware(XElement root, ITSEnv? env)
    {
        var softwareNode = root.Element(XmlConstants.Elements.Software);
        if (softwareNode is null)
            return new Dictionary<string, ISoftware>();

        var result = new Dictionary<string, ISoftware>(StringComparer.OrdinalIgnoreCase);
        int order  = 0;

        foreach (var node in softwareNode.Elements())
        {
            var localName = node.Name.LocalName;
            var id        = Attr(node, env, XmlConstants.Attributes.Id);
            if (string.IsNullOrWhiteSpace(id)) continue;

            var label      = Attr(node, env, XmlConstants.Attributes.Label)        ?? id;
            var info       = Attr(node, env, XmlConstants.Attributes.SoftwareInfo) ?? string.Empty;
            var includeIds = Attr(node, env, XmlConstants.Attributes.IncludeId)    ?? string.Empty;
            var excludeIds = Attr(node, env, XmlConstants.Attributes.ExcludeId)    ?? string.Empty;

            ISoftware? sw = null;

            if (localName.Equals(XmlConstants.Elements.Application, StringComparison.OrdinalIgnoreCase))
            {
                var appName = Attr(node, env, XmlConstants.Attributes.AppName) ?? string.Empty;
                sw = new Application(id, label, info, appName, includeIds, excludeIds, order);
            }
            else if (localName.Equals(XmlConstants.Elements.Package, StringComparison.OrdinalIgnoreCase))
            {
                var pkgId    = Attr(node, env, XmlConstants.Attributes.PkgId)       ?? string.Empty;
                var progName = Attr(node, env, XmlConstants.Attributes.ProgramName) ?? string.Empty;
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

    // Mirrors C++ GetXMLAttribute (UI++/Actions/IAction.cpp:21), which the
    // original also uses for the root element's attributes (UI++.cpp:237), so
    // variables in them are substituted. Returns null when the attribute is
    // absent so callers can apply their own defaults.
    private static string? Attr(XElement el, ITSEnv? env, string name)
    {
        var raw = (string?)el.Attribute(name);
        if (raw is null || raw.Length == 0) return raw;
        return env is null ? raw : env.Substitute(raw);
    }

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
    //
    // Comments, CDATA sections and processing instructions are copied through
    // untouched: nothing inside them is an attribute. Without that, an ordinary
    // apostrophe in a comment ("the developer's machine") opens a quoted section
    // that never closes and swallows the rest of the file, and the config then
    // fails to parse pointing at a line nowhere near the comment.
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

            if (CopyVerbatim(xml, ref i, sb, "!--",      "-->")) continue;
            if (CopyVerbatim(xml, ref i, sb, "![CDATA[", "]]>")) continue;
            if (CopyVerbatim(xml, ref i, sb, "!DOCTYPE", ">"))   continue;
            if (CopyVerbatim(xml, ref i, sb, "?",        "?>"))  continue;

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

    // Copies a run that starts with <prefix and ends with terminator straight
    // through, advancing the cursor past it. Returns false when the text at the
    // cursor does not begin such a run, leaving the cursor untouched.
    private static bool CopyVerbatim(
        string xml, ref int i, System.Text.StringBuilder sb, string prefix, string terminator)
    {
        if (string.CompareOrdinal(xml, i, prefix, 0, prefix.Length) != 0) return false;

        var end = xml.IndexOf(terminator, i + prefix.Length, StringComparison.Ordinal);

        // An unterminated comment is a malformed document: hand the remainder to
        // the XML parser so it reports that, rather than guessing here.
        var stop = end < 0 ? xml.Length : end + terminator.Length;

        sb.Append(xml, i, stop - i);
        i = stop;
        return true;
    }
}
