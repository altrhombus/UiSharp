using UIpp.Core.Actions;
using UIpp.Core.Dialogs;
using UIpp.Core.Variables;

namespace UIpp.UI.Dialogs;

public class DlgBase : Form
{
    private const int SidebarWidth    = 200;
    private const int ButtonBarHeight = 48;
    private const int FormWidth       = 760;
    private const int FormHeight      = 520;
    private const int BtnW            = 88;
    private const int BtnH            = 30;
    private const int BtnY            = 9;

    protected readonly DialogTraits Traits;
    protected readonly Panel ContentPanel;
    protected readonly Button BtnBack;
    protected readonly Button BtnNext;
    protected readonly Button BtnCancel;
    protected readonly Button BtnRefresh;

    private readonly ITSEnv? _env;
    private System.Windows.Forms.Timer? _timer;
    private int _countdown;
    private ActionResult _countdownResult = ActionResult.Next;

    public ActionResult Result { get; private set; } = ActionResult.Next;

    public DlgBase(DialogTraits traits, ITSEnv? env = null, string? dlgTitle = null, string? dlgSubtitle = null)
    {
        _env = env;
        Traits = traits;
        SuspendLayout();

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
        var buttonBar = new Panel
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
        var sidebar = new Panel
        {
            Bounds = new Rectangle(0, 0, SidebarWidth, FormHeight - ButtonBarHeight),
            BackColor = traits.AccentColor,
        };

        sidebar.Controls.Add(new Label
        {
            Text      = dlgTitle ?? traits.Title,
            ForeColor = traits.SidebarTextColor,
            Font      = new Font(traits.FontFace, 14f, FontStyle.Bold),
            AutoSize  = false,
            Bounds    = new Rectangle(16, 20, SidebarWidth - 32, 80),
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
