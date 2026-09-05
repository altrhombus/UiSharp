using UiSharp.Core.Actions;
using UiSharp.Core.Dialogs;
using UiSharp.Core.Logging;
using UiSharp.Core.Variables;

namespace UiSharp.UI.Dialogs;

public class DlgBase : Form
{
    private const int SidebarWidth    = 200;
    private const int ButtonBarHeight = 48;
    private const int FormWidth       = 760;
    private const int FormHeight      = 520;
    private const int BtnW            = 88;
    private const int BtnH            = 30;
    private const int BtnY            = 9;
    private const int IconSize        = 32;   // the size the original loads (UI++.cpp:254)

    protected readonly DialogTraits Traits;
    protected readonly Panel ContentPanel;

    // Exposed so a dialog that is not the standard fixed size can re-dock them.
    // Everything here is laid out at fixed coordinates, which is right for a
    // FixedDialog and wrong for anything that resizes.
    protected readonly Panel Sidebar;
    protected readonly Panel ButtonBar;
    protected readonly Button BtnBack;
    protected readonly Button BtnNext;
    protected readonly Button BtnCancel;
    protected readonly Button BtnRefresh;

    private readonly ITSEnv? _env;
    private System.Windows.Forms.Timer? _timer;
    private int _countdown;
    private ActionResult _countdownResult = ActionResult.Next;

    public ActionResult Result { get; private set; } = ActionResult.Next;

    public DlgBase(
        DialogTraits traits,
        ITSEnv? env = null,
        string? dlgTitle = null,
        string? dlgSubtitle = null,
        ICMLog? log = null)
    {
        _env = env;
        Traits = traits;
        SuspendLayout();

        // The root <UIpp Icon="..."> attribute. It was parsed and then used by
        // nothing at all. The original puts it in the dialog's banner, left and
        // vertically centred (DlgBase.cpp:116); it is also used as the window
        // icon here, which the original does not do but which costs nothing and
        // is what a title bar is for.
        var dlgIcon = WindowIcon(traits.IconPath, log);
        if (dlgIcon is not null) Icon = dlgIcon;

        Text = dlgTitle ?? traits.Title;
        ClientSize = new Size(FormWidth, FormHeight);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = traits.AlwaysOnTop;
        ShowInTaskbar = false;
        BackColor = SystemColors.Control;
        Font = new Font(traits.FontFace, 9f);

        // Button bar across the full bottom
        var buttonBar = ButtonBar = new Panel
        {
            Bounds = new Rectangle(0, FormHeight - ButtonBarHeight, FormWidth, ButtonBarHeight),
            BackColor = SystemColors.Control,
        };

        BtnBack    = Btn("< Back",   new Point(8, BtnY));
        BtnNext    = Btn("Next >",   new Point(FormWidth - 8 - BtnW, BtnY));
        BtnCancel  = Btn("Cancel",   new Point(FormWidth - 8 - BtnW * 2 - 8, BtnY));
        BtnRefresh = Btn("Refresh",  new Point(FormWidth - 8 - BtnW * 3 - 16, BtnY));

        BtnBack.Click    += (_, _) => Finish(ActionResult.Back);
        BtnNext.Click    += (_, _) => { if (ValidateInput()) Finish(ActionResult.Next); };
        BtnCancel.Click  += (_, _) => Finish(ActionResult.Cancel);
        BtnRefresh.Click += (_, _) => Finish(ActionResult.Refresh);

        buttonBar.Controls.AddRange([BtnBack, BtnCancel, BtnRefresh, BtnNext]);

        // Sidebar
        var sidebar = Sidebar = new Panel
        {
            Bounds = new Rectangle(0, 0, SidebarWidth, FormHeight - ButtonBarHeight),
            BackColor = traits.AccentColor,
        };

        // The icon sits to the left of the title, and the title gives way to it.
        var titleLeft = 16;

        if (dlgIcon is not null)
        {
            sidebar.Controls.Add(new PictureBox
            {
                Image    = dlgIcon.ToBitmap(),
                SizeMode = PictureBoxSizeMode.Zoom,
                Bounds   = new Rectangle(16, 20, IconSize, IconSize),
            });

            titleLeft = 16 + IconSize + 8;
        }

        sidebar.Controls.Add(new Label
        {
            Text      = dlgTitle ?? traits.Title,
            ForeColor = traits.SidebarTextColor,
            Font      = new Font(traits.FontFace, 14f, FontStyle.Bold),
            AutoSize  = false,
            Bounds    = new Rectangle(titleLeft, 20, SidebarWidth - titleLeft - 16, 80),
        });

        if (!string.IsNullOrWhiteSpace(dlgSubtitle ?? traits.Subtitle))
        {
            sidebar.Controls.Add(new Label
            {
                Text      = dlgSubtitle ?? traits.Subtitle,
                ForeColor = traits.SidebarTextColor,
                Font      = new Font(traits.FontFace, 9f),
                AutoSize  = false,
                Bounds    = new Rectangle(16, 110, SidebarWidth - 32, 200),
            });
        }

        // Content panel (right of sidebar, above button bar)
        ContentPanel = new Panel
        {
            Bounds    = new Rectangle(SidebarWidth, 0, FormWidth - SidebarWidth, FormHeight - ButtonBarHeight),
            BackColor = Color.White,
            ForeColor = traits.TextColor,
            Padding   = new Padding(16, 12, 12, 12),
        };

        Controls.AddRange([ContentPanel, sidebar, buttonBar]);
        AcceptButton = BtnNext;

        // Buttons default: Back and Refresh hidden; Cancel shown
        BtnBack.Visible    = false;
        BtnRefresh.Visible = false;

        ResumeLayout(true);
    }

    private static Button Btn(string text, Point loc) =>
        new() { Text = text, Bounds = new Rectangle(loc, new Size(BtnW, BtnH)) };

    // One icon serves every dialog of a run, so it is loaded once. Caching the
    // failures too keeps a missing file, or an unreachable URL, from being
    // retried — and re-reported — on every screen.
    private static readonly Dictionary<string, Icon?> IconCache = new(StringComparer.OrdinalIgnoreCase);

    private static Icon? WindowIcon(string? iconPath, ICMLog? log)
    {
        if (string.IsNullOrWhiteSpace(iconPath)) return null;

        if (IconCache.TryGetValue(iconPath, out var cached)) return cached;

        var icon = UiImage.LoadIcon(iconPath, log);
        IconCache[iconPath] = icon;
        return icon;
    }

    protected virtual bool ValidateInput() => true;

    protected void Finish(ActionResult result)
    {
        Result = result;
        _timer?.Stop();
        DialogResult = result == ActionResult.Cancel ? DialogResult.Cancel : DialogResult.OK;
    }

    public void EnableTimeout(int seconds, ActionResult action)
    {
        _countdown       = seconds;
        _countdownResult = action;
        UpdateNextText();
        _timer          = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick    += OnTick;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _countdown--;
        UpdateNextText();
        if (_countdown <= 0)
        {
            _timer!.Stop();
            if (ValidateInput()) Finish(_countdownResult);
        }
    }

    private void UpdateNextText() =>
        BtnNext.Text = _countdown > 0 ? $"Next ({_countdown}s)" : "Next >";

    // Ctrl+F2 → TSVar editor  |  Ctrl+F3 → dump vars to file (matches C++ accelerators)
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_env is not null && Traits.AllowVarEditor)
        {
            if (keyData == (Keys.F2 | Keys.Control))
            {
                using var dlg = new DlgTSVar(_env);
                dlg.ShowDialog(this);
                return true;
            }
            if (keyData == (Keys.F3 | Keys.Control))
            {
                _env.DumpToFile();
                return true;
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer?.Stop();
        _timer?.Dispose();
        base.OnFormClosed(e);
    }
}
