using System.Text;
using System.Xml;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using UIpp.Core.Configuration;
using UIpp.Core.Dialogs;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels;

public sealed partial class GlobalSettingsViewModel : ObservableObject, IXmlEditorSource
{
    [ObservableProperty] public partial string Title           { get; set; }
    [ObservableProperty] public partial string Subtitle        { get; set; }
    [ObservableProperty] public partial string FontFace        { get; set; }
    [ObservableProperty] public partial string IconPath        { get; set; }
    [ObservableProperty] public partial string AccentColor     { get; set; }
    [ObservableProperty] public partial string SidebarTextColor { get; set; }
    [ObservableProperty] public partial bool   ShowIcons       { get; set; }
    [ObservableProperty] public partial bool   ShowSidebar     { get; set; }
    [ObservableProperty] public partial bool   AlwaysOnTop     { get; set; }
    [ObservableProperty] public partial bool   Flat            { get; set; }
    [ObservableProperty] public partial string ConditionEngine { get; set; }
    [ObservableProperty] public partial string SchemaVersion   { get; set; }

    public IReadOnlyList<string> ConditionEngineOptions { get; } =
        [C.Values.ConditionEngineNative, C.Values.ConditionEngineVbscript];

    // ── Comment (document-level XML comment before the root element) ─────────

    private string? _comment;
    public string? Comment
    {
        get => _comment;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? null
                           : value.Replace("\r\n", "\n").Replace("\r", "\n");
            if (_comment == normalized) return;
            _comment = normalized;
            OnPropertyChanged();
        }
    }

    // ── IXmlEditorSource ─────────────────────────────────────────────────────

    private bool _updatingFromXml;

    [ObservableProperty]
    public partial string CurrentXmlText { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string? XmlValidationError { get; private set; }

    public (int Start, int End) SelectedLineRange { get; private set; } = (-1, -1);

#pragma warning disable CS0067 // Event required by IXmlEditorSource; never raised because there is no per-item selection on this page.
    public event EventHandler? SelectionDecorationChanged;
#pragma warning restore CS0067

    public void OnXmlEdited(string xml)
    {
        _updatingFromXml = true;
        CurrentXmlText = xml;
        TryApplyXmlToSettings(xml);
        _updatingFromXml = false;
    }

    public void SelectAtLine(int line) { }

    public void SyncXmlToGuided() { }

    public void SyncGuidedToXml() => RefreshXml();

    // ── Construction ─────────────────────────────────────────────────────────

    public GlobalSettingsViewModel()
    {
        Title            = "UI++";
        Subtitle         = string.Empty;
        FontFace         = XmlConstants.DefaultFontFace;
        IconPath         = string.Empty;
        AccentColor      = C.Defaults.AccentColor;
        SidebarTextColor = C.Defaults.SidebarTextColor;
        ShowIcons        = true;
        ShowSidebar      = true;
        AlwaysOnTop      = true;
        ConditionEngine  = C.Values.ConditionEngineNative;
        SchemaVersion    = string.Empty;

        // Rebuild the XML panel whenever any guided field changes.
        PropertyChanged += (_, e) =>
        {
            if (_updatingFromXml) return;
            if (e.PropertyName is nameof(CurrentXmlText) or nameof(XmlValidationError)) return;
            RefreshXml();
        };
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void LoadFrom(DialogTraits traits, string conditionEngine, int? schemaVersion,
                         string? documentComment = null)
    {
        _updatingFromXml = true;
        Title           = traits.Title;
        Subtitle        = traits.Subtitle;
        FontFace        = traits.FontFace;
        IconPath        = traits.IconPath ?? string.Empty;
        AccentColor     = $"#{traits.AccentColor.R:X2}{traits.AccentColor.G:X2}{traits.AccentColor.B:X2}";
        SidebarTextColor = $"#{traits.SidebarTextColor.R:X2}{traits.SidebarTextColor.G:X2}{traits.SidebarTextColor.B:X2}";
        ShowIcons       = traits.ShowIcons;
        ShowSidebar     = traits.ShowSidebar;
        AlwaysOnTop     = traits.AlwaysOnTop;
        Flat            = traits.Flat;
        ConditionEngine = conditionEngine;
        SchemaVersion   = schemaVersion?.ToString() ?? string.Empty;
        Comment         = documentComment;
        _updatingFromXml = false;
        RefreshXml();
    }

    public DialogTraits ToTraits()
    {
        var flags = DialogTraitFlags.None;
        if (ShowIcons)   flags |= DialogTraitFlags.ShowIcons;
        if (ShowSidebar) flags |= DialogTraitFlags.ShowSidebar;
        if (AlwaysOnTop) flags |= DialogTraitFlags.AlwaysOnTop;
        if (Flat)        flags |= DialogTraitFlags.Flat;
        flags |= DialogTraitFlags.AllowVarEditor;

        return new DialogTraits
        {
            Title           = Title,
            Subtitle        = Subtitle,
            FontFace        = FontFace,
            IconPath        = string.IsNullOrEmpty(IconPath) ? null : IconPath,
            AccentColor     = ParseHex(AccentColor, System.Drawing.Color.FromArgb(0x00, 0x21, 0x47)),
            SidebarTextColor = ParseHex(SidebarTextColor, System.Drawing.Color.White),
            Flags           = flags,
        };
    }

    public int? GetSchemaVersion() =>
        int.TryParse(SchemaVersion, out var v) && v > 0 ? v : null;

    // ── XML sync helpers ──────────────────────────────────────────────────────

    private void RefreshXml()
    {
        if (_updatingFromXml) return;
        var xml = BuildSettingsXml();
        if (xml != CurrentXmlText)
            CurrentXmlText = xml;
    }

    private string BuildSettingsXml()
    {
        // Build the element matching ConfigWriter.SetRootAttributes exactly.
        var el = new XElement(C.Elements.Root);
        el.SetAttributeValue(C.Attributes.Title, Title);
        if (!string.IsNullOrEmpty(Subtitle))
            el.SetAttributeValue(C.Attributes.Subtitle, Subtitle);
        if (!string.IsNullOrEmpty(FontFace) && FontFace != XmlConstants.DefaultFontFace)
            el.SetAttributeValue(C.Attributes.Font, FontFace);
        if (!string.IsNullOrEmpty(IconPath))
            el.SetAttributeValue(C.Attributes.Icon, IconPath);
        el.SetAttributeValue(C.Attributes.Color,            AccentColor);
        el.SetAttributeValue(C.Attributes.SidebarTextColor, SidebarTextColor);
        el.SetAttributeValue(C.Attributes.DialogIcons,      ShowIcons   ? "true" : "false");
        el.SetAttributeValue(C.Attributes.DialogSidebar,    ShowSidebar ? "true" : "false");
        el.SetAttributeValue(C.Attributes.AlwaysOnTop,      AlwaysOnTop ? "true" : "false");
        el.SetAttributeValue(C.Attributes.Flat,             Flat        ? "true" : "false");
        if (!ConditionEngine.Equals(C.Values.ConditionEngineNative, StringComparison.OrdinalIgnoreCase))
            el.SetAttributeValue(C.Attributes.ConditionEngine, ConditionEngine);
        var sv = GetSchemaVersion();
        if (sv is > 0)
            el.SetAttributeValue(C.Attributes.SchemaVersion, sv.Value);

        // Serialize with one attribute per line for readability.
        var xmlSettings = new XmlWriterSettings
        {
            Indent              = true,
            IndentChars         = "  ",
            NewLineOnAttributes = true,
            OmitXmlDeclaration  = true,
        };
        var sb = new StringBuilder();
        using (var w = XmlWriter.Create(sb, xmlSettings))
            el.WriteTo(w);
        var elementXml = sb.ToString();

        if (!string.IsNullOrWhiteSpace(Comment))
            return FormatComment(Comment) + Environment.NewLine + elementXml;
        return elementXml;
    }

    private bool TryApplyXmlToSettings(string xml)
    {
        try
        {
            // Wrap in a container so a leading comment + element parse as a fragment.
            var doc = XDocument.Parse("<wrap>" + xml + "</wrap>");
            var wrapper = doc.Root!;

            string? newComment = null;
            foreach (var node in wrapper.Nodes())
            {
                if (node is XComment c)
                {
                    var normalized = NormalizeComment(c.Value);
                    newComment = newComment is null ? normalized : newComment + "\n" + normalized;
                }
            }

            var el = wrapper.Elements().FirstOrDefault()
                ?? throw new InvalidOperationException("Expected a <UIpp> element.");

            Comment         = newComment;
            Title           = (string?)el.Attribute(C.Attributes.Title)           ?? string.Empty;
            Subtitle        = (string?)el.Attribute(C.Attributes.Subtitle)        ?? string.Empty;
            FontFace        = (string?)el.Attribute(C.Attributes.Font)            ?? XmlConstants.DefaultFontFace;
            IconPath        = (string?)el.Attribute(C.Attributes.Icon)            ?? string.Empty;
            AccentColor     = (string?)el.Attribute(C.Attributes.Color)           ?? C.Defaults.AccentColor;
            SidebarTextColor = (string?)el.Attribute(C.Attributes.SidebarTextColor) ?? C.Defaults.SidebarTextColor;
            ShowIcons       = ParseBool(el.Attribute(C.Attributes.DialogIcons),   defaultValue: true);
            ShowSidebar     = ParseBool(el.Attribute(C.Attributes.DialogSidebar), defaultValue: true);
            AlwaysOnTop     = ParseBool(el.Attribute(C.Attributes.AlwaysOnTop),   defaultValue: true);
            Flat            = ParseBool(el.Attribute(C.Attributes.Flat),          defaultValue: false);
            ConditionEngine = (string?)el.Attribute(C.Attributes.ConditionEngine) ?? C.Values.ConditionEngineNative;
            SchemaVersion   = (string?)el.Attribute(C.Attributes.SchemaVersion)   ?? string.Empty;

            XmlValidationError = null;
            return true;
        }
        catch (Exception ex)
        {
            XmlValidationError = ex.Message;
            return false;
        }
    }

    private static string FormatComment(string comment) =>
        comment.Contains('\n')
            ? "<!--\n" + string.Join("\n", comment.Split('\n').Select(l => "  " + l)) + "\n-->"
            : $"<!-- {comment.Trim()} -->";

    private static string NormalizeComment(string rawValue)
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

    private static bool ParseBool(XAttribute? attr, bool defaultValue) =>
        attr is null ? defaultValue
        : attr.Value.Equals("true", StringComparison.OrdinalIgnoreCase)
          || attr.Value.Equals("yes",  StringComparison.OrdinalIgnoreCase)
          || attr.Value.Equals("1",    StringComparison.Ordinal);

    private static System.Drawing.Color ParseHex(string? hex, System.Drawing.Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        hex = hex.TrimStart('#');
        if (hex.Length == 6 &&
            uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber,
                          System.Globalization.CultureInfo.InvariantCulture, out var rgb))
        {
            return System.Drawing.Color.FromArgb(0xFF, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        }
        return fallback;
    }
}
