using UiSharp.Core.Variables;

namespace UiSharp.UI.Dialogs;

// TS variable editor: Ctrl+F2 from any dialog.
// Two tabs — read-only (_-prefixed vars) and editable (everything else).
// OK writes modified editable values back to ITSEnv.
public sealed class DlgTSVar : Form
{
    private readonly ITSEnv _env;
    private readonly DataGridView _readonlyGrid;
    private readonly DataGridView _editableGrid;

    public DlgTSVar(ITSEnv env)
    {
        _env = env;
        SuspendLayout();

        Text            = "Task Sequence Variables";
        ClientSize      = new Size(640, 480);
        MinimumSize     = new Size(400, 300);
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition   = FormStartPosition.CenterParent;
        ShowInTaskbar   = false;

        var tabs = new TabControl { Dock = DockStyle.Fill };

        _readonlyGrid = MakeGrid(readOnly: true);
        _editableGrid = MakeGrid(readOnly: false);

        var tabReadonly = new TabPage("Read-only");
        tabReadonly.Controls.Add(_readonlyGrid);

        var tabEditable = new TabPage("Editable");
        tabEditable.Controls.Add(_editableGrid);

        tabs.TabPages.AddRange([tabReadonly, tabEditable]);

        var btnBar = new Panel
        {
            Dock   = DockStyle.Bottom,
            Height = 44,
        };

        var btnOk = new Button
        {
            Text     = "OK",
            Size     = new Size(88, 28),
            Location = new Point(ClientSize.Width - 200, 8),
            Anchor   = AnchorStyles.Right | AnchorStyles.Top,
        };
        btnOk.Click += (_, _) => { CommitEditable(); DialogResult = DialogResult.OK; };

        var btnCancel = new Button
        {
            Text     = "Cancel",
            Size     = new Size(88, 28),
            Location = new Point(ClientSize.Width - 104, 8),
            Anchor   = AnchorStyles.Right | AnchorStyles.Top,
        };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; };

        btnBar.Controls.AddRange([btnOk, btnCancel]);

        Controls.AddRange([tabs, btnBar]);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        ResumeLayout(false);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        PopulateGrids();
    }

    private static DataGridView MakeGrid(bool readOnly)
    {
        var gv = new DataGridView
        {
            Dock                  = DockStyle.Fill,
            AllowUserToAddRows    = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible     = false,
            AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
            ReadOnly              = readOnly,
        };

        gv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Variable Name",
            FillWeight = 40,
            ReadOnly   = true,
        });

        gv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Value",
            FillWeight = 60,
            ReadOnly   = readOnly,
        });

        return gv;
    }

    private void PopulateGrids()
    {
        _readonlyGrid.Rows.Clear();
        _editableGrid.Rows.Clear();

        // LocalTSEnv exposes GetAll; ConfigMgrTSEnv enumerates via GetVariables.
        // Use the common ITSEnv.GetAll() if available, otherwise skip enumeration.
        if (_env is not UiSharp.Core.Variables.LocalTSEnv local)
        {
            _editableGrid.Rows.Add("(info)",
                "Variable enumeration is not available inside a running task sequence.");
            return;
        }

        foreach (var kv in local.GetAll().OrderBy(k => k.Key))
        {
            var isReadOnly = kv.Key.StartsWith('_');
            var target = isReadOnly ? _readonlyGrid : _editableGrid;
            target.Rows.Add(kv.Key, kv.Value);
        }
    }

    private void CommitEditable()
    {
        foreach (DataGridViewRow row in _editableGrid.Rows)
        {
            var name  = row.Cells[0].Value?.ToString() ?? string.Empty;
            var value = row.Cells[1].Value?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(name))
                _env.Set(name, value);
        }
    }
}
