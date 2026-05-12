using CommunityToolkit.Mvvm.ComponentModel;
using UIpp.Core.Configuration;
using UIpp.Core.Dialogs;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels;

public sealed partial class GlobalSettingsViewModel : ObservableObject
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
    }

    public void LoadFrom(DialogTraits traits, string conditionEngine, int? schemaVersion)
    {
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
