using UIpp.Core.Dialogs;
using UIpp.Core.Variables;

namespace UIpp.UI.Dialogs;

// Preflight dialog: displays check results with pass/warn/fail icons.
// Next is disabled when any check failed.
public sealed class DlgPreflight : DlgBase
{
    private static readonly Color ColorPass = Color.FromArgb(0x22, 0x8B, 0x22);
    private static readonly Color ColorWarn = Color.FromArgb(0xFF, 0x8C, 0x00);
    private static readonly Color ColorFail = Color.FromArgb(0xCC, 0x00, 0x00);

    private readonly IReadOnlyList<PreflightResult> _results;
    private readonly Panel _detailPanel;

    public DlgPreflight(
        DialogTraits traits,
        ITSEnv env,
        string? dlgTitle,
        string? dlgSubtitle,
        IReadOnlyList<PreflightResult> results,
        bool showBack,
        bool showCancel,
        bool anyFailed)
        : base(traits, env, dlgTitle ?? "Preflight", dlgSubtitle)
    {
        _results = results;
        BtnBack.Visible   = showBack;
        BtnCancel.Visible = showCancel;
        BtnNext.Enabled   = !anyFailed;

        // Detail panel below the list
        _detailPanel = new Panel
        {
            BorderStyle = BorderStyle.FixedSingle,
            BackColor   = Color.WhiteSmoke,
            Bounds      = new Rectangle(8, ContentPanel.Height - 80, ContentPanel.Width - 16, 70),
        };

        var detailLabel = new Label
        {
            Dock      = DockStyle.Fill,
            AutoSize  = false,
            TextAlign = ContentAlignment.TopLeft,
            Padding   = new Padding(4),
        };

        _detailPanel.Controls.Add(detailLabel);
        ContentPanel.Controls.Add(_detailPanel);

        BuildList(detailLabel);
    }

    private void BuildList(Label detailLabel)
    {
        var lv = new ListView
        {
            View          = View.Details,
            FullRowSelect = true,
            GridLines     = true,
            Bounds        = new Rectangle(8, 8, ContentPanel.Width - 16, ContentPanel.Height - 96),
            HeaderStyle   = ColumnHeaderStyle.Nonclickable,
        };

        lv.Columns.Add("Status",      70);
        lv.Columns.Add("Check",       ContentPanel.Width - 100);

        foreach (var r in _results)
        {
            var item = new ListViewItem(StatusText(r.Status))
            {
                ForeColor = StatusColor(r.Status),
            };
            item.SubItems.Add(r.Check.Text);
            item.Tag = r;
            lv.Items.Add(item);
        }

        lv.SelectedIndexChanged += (_, _) =>
        {
            if (lv.SelectedItems.Count == 0) return;
            var r = (PreflightResult)lv.SelectedItems[0].Tag!;
            detailLabel.Text = r.ActiveDescription;
        };

        ContentPanel.Controls.Add(lv);
    }

    private static string StatusText(PreflightStatus s) => s switch
    {
        PreflightStatus.Pass => "Pass",
        PreflightStatus.Warn => "Warn",
        _                    => "Fail",
    };

    private static Color StatusColor(PreflightStatus s) => s switch
    {
        PreflightStatus.Pass => ColorPass,
        PreflightStatus.Warn => ColorWarn,
        _                    => ColorFail,
    };
}
