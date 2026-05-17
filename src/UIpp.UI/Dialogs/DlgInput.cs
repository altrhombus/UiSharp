using UIpp.Core.Dialogs;
using UIpp.Core.Variables;

namespace UIpp.UI.Dialogs;

// Dynamic input dialog: renders InputFieldSpec items as WinForms controls.
public sealed class DlgInput : DlgBase
{
    private readonly IReadOnlyList<InputFieldSpec> _fields;
    // Maps field index → the control that holds the value (TextBox, ComboBox, CheckBox).
    private readonly Dictionary<int, Control> _controls = [];

    public DlgInput(
        DialogTraits traits,
        ITSEnv env,
        string? dlgTitle,
        string? dlgSubtitle,
        IReadOnlyList<InputFieldSpec> fields,
        bool showBack,
        bool showCancel)
        : base(traits, env, dlgTitle, dlgSubtitle)
    {
        _fields = fields;
        BtnBack.Visible   = showBack;
        BtnCancel.Visible = showCancel;

        BuildControls();
    }

    private void BuildControls()
    {
        var scroll = new Panel
        {
            AutoScroll = true,
            Bounds     = new Rectangle(0, 0, ContentPanel.Width, ContentPanel.Height),
        };

        int y = 8;
        const int Lw   = 200;  // label width
        const int Cw   = 300;  // control width
        const int Lh   = 20;
        const int Pad  = 4;

        for (int i = 0; i < _fields.Count; i++)
        {
            var spec = _fields[i];

            var lbl = new Label
            {
                Text      = spec.Question,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Bounds    = new Rectangle(8, y, Lw, Lh),
            };
            scroll.Controls.Add(lbl);

            switch (spec)
            {
                case InputTextSpec ts:
                {
                    var tb = new TextBox
                    {
                        Text          = ts.DefaultValue,
                        PasswordChar  = ts.Password ? '●' : '\0',
                        ScrollBars    = ts.HorizontalScroll ? ScrollBars.Horizontal : ScrollBars.None,
                        ReadOnly      = ts.ReadOnly,
                        Bounds        = new Rectangle(8 + Lw + Pad, y, Cw, Lh + 4),
                    };
                    if (!string.IsNullOrWhiteSpace(ts.Hint))
                        tb.PlaceholderText = ts.Hint;

                    scroll.Controls.Add(tb);
                    _controls[i] = tb;
                    y += Lh + Pad * 3;
                    break;
                }

                case InputChoiceSpec cs:
                {
                    var sorted = cs.Sort
                        ? cs.Choices.OrderBy(c => c.Option).ToList()
                        : cs.Choices.ToList();

                    var cb = new ComboBox
                    {
                        DropDownStyle = cs.AutoComplete
                            ? ComboBoxStyle.DropDown
                            : ComboBoxStyle.DropDownList,
                        MaxDropDownItems = cs.DropDownSize,
                        Bounds           = new Rectangle(8 + Lw + Pad, y, Cw, Lh + 4),
                    };

                    foreach (var opt in sorted)
                        cb.Items.Add(opt.Option);

                    if (!string.IsNullOrEmpty(cs.DefaultValue))
                    {
                        int idx = sorted.FindIndex(o =>
                            string.Equals(o.Value, cs.DefaultValue, StringComparison.OrdinalIgnoreCase));
                        if (idx >= 0) cb.SelectedIndex = idx;
                    }

                    scroll.Controls.Add(cb);
                    _controls[i] = cb;
                    y += Lh + Pad * 3;
                    break;
                }

                case InputCheckboxSpec cks:
                {
                    // Override label — checkbox has its own text.
                    lbl.Text = string.Empty;
                    var chk = new CheckBox
                    {
                        Text    = spec.Question,
                        Checked = cks.DefaultValue.Equals(cks.CheckedValue, StringComparison.Ordinal),
                        Bounds  = new Rectangle(8, y, Lw + Pad + Cw, Lh),
                        AutoSize = false,
                    };
                    scroll.Controls.Add(chk);
                    _controls[i] = chk;
                    y += Lh + Pad * 3;
                    break;
                }

                case InputInfoSpec _:
                {
                    // Info field — just the label, no input control.
                    lbl.Bounds = new Rectangle(8, y, Lw + Pad + Cw, Lh);
                    y += Lh + Pad * 2;
                    break;
                }
            }
        }

        ContentPanel.Controls.Add(scroll);
    }

    protected override bool ValidateInput()
    {
        for (int i = 0; i < _fields.Count; i++)
        {
            if (!_controls.TryGetValue(i, out var ctrl)) continue;

            string value = ctrl switch
            {
                TextBox tb  => tb.Text,
                ComboBox cb => cb.Text,
                _           => string.Empty,
            };

            ValidationResult v = _fields[i] switch
            {
                InputTextSpec ts   => ts.Validate(value),
                InputChoiceSpec cs => cs.Validate(value),
                _                  => ValidationResult.Ok,
            };

            if (!v.IsValid)
            {
                MessageBox.Show(v.ErrorMessage, "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ctrl.Focus();
                return false;
            }
        }
        return true;
    }

    // Writes validated values back to the TS environment.
    public void CommitValues(UIpp.Core.Variables.ITSEnv env)
    {
        for (int i = 0; i < _fields.Count; i++)
        {
            if (!_controls.TryGetValue(i, out var ctrl)) continue;

            switch (_fields[i])
            {
                case InputTextSpec ts:
                {
                    var tb  = (TextBox)ctrl;
                    var raw = ts.ApplyForceCase(tb.Text);
                    env.Set(ts.Variable, raw);
                    break;
                }

                case InputChoiceSpec cs:
                {
                    var cb  = (ComboBox)ctrl;
                    var opt = cs.Choices.FirstOrDefault(c =>
                        string.Equals(c.Option, cb.Text, StringComparison.OrdinalIgnoreCase));

                    // C++: returns empty string when selection not found in choices list (e.g. free-text AutoComplete).
                    env.Set(cs.Variable, opt?.Value ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(cs.AltVariable))
                        env.Set(cs.AltVariable, opt?.AltValue ?? string.Empty);
                    break;
                }

                case InputCheckboxSpec cks:
                {
                    var chk = (CheckBox)ctrl;
                    env.Set(cks.Variable, chk.Checked ? cks.CheckedValue : cks.UncheckedValue);
                    break;
                }
            }
        }
    }
}
