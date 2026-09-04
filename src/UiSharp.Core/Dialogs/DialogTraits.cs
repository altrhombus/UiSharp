using System.Drawing;

namespace UiSharp.Core.Dialogs;

[Flags]
public enum DialogTraitFlags : short
{
    None              = 0,
    ShowIcons         = 1,
    ShowSidebar       = 2,
    AllowVarEditor    = 4,
    AllowBack         = 8,
    AllowRefresh      = 16,
    Flat              = 32,
    AlwaysOnTop       = 64,
    Default           = ShowIcons | AllowVarEditor | AllowBack | AlwaysOnTop | ShowSidebar,
}

public sealed class DialogTraits
{
    public string Title           { get; set; } = "UI++";
    public string Subtitle        { get; set; } = string.Empty;
    public string FontFace        { get; set; } = "Tahoma";
    public string? IconPath       { get; set; }
    public Color AccentColor      { get; set; } = Color.FromArgb(0x00, 0x21, 0x47);   // #002147
    public Color TextColor        { get; set; } = Color.Black;
    public Color SidebarTextColor { get; set; } = Color.White;
    public float ScreenScaleX     { get; set; } = 1.0f;
    public float ScreenScaleY     { get; set; } = 1.0f;
    public DialogTraitFlags Flags { get; set; } = DialogTraitFlags.Default;

    public bool ShowIcons      => Flags.HasFlag(DialogTraitFlags.ShowIcons);
    public bool ShowSidebar    => Flags.HasFlag(DialogTraitFlags.ShowSidebar);
    public bool AllowVarEditor => Flags.HasFlag(DialogTraitFlags.AllowVarEditor);
    public bool Flat           => Flags.HasFlag(DialogTraitFlags.Flat);
    public bool AlwaysOnTop    => Flags.HasFlag(DialogTraitFlags.AlwaysOnTop);

    public bool AllowBack
    {
        get => Flags.HasFlag(DialogTraitFlags.AllowBack);
        set => Flags = value ? Flags | DialogTraitFlags.AllowBack
                             : (DialogTraitFlags)(Flags & ~DialogTraitFlags.AllowBack);
    }

    public bool AllowRefresh
    {
        get => Flags.HasFlag(DialogTraitFlags.AllowRefresh);
        set => Flags = value ? Flags | DialogTraitFlags.AllowRefresh
                             : (DialogTraitFlags)(Flags & ~DialogTraitFlags.AllowRefresh);
    }
}
