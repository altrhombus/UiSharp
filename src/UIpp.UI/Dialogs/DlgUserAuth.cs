using UIpp.Core.Dialogs;
using UIpp.Core.Ldap;
using UIpp.Core.Variables;

namespace UIpp.UI.Dialogs;

// Per-field display overrides parsed from <Field name="username|password|domain"> children.
public sealed record UserAuthFieldSpec(
    string? Question,
    string? Hint,
    bool    ReadOnly,
    IReadOnlyList<string>? DomainList,
    bool    AutoComplete);

// Domain credential prompt. Validates against AD if ILdap is provided.
public sealed class DlgUserAuth : DlgBase
{
    private readonly TextBox  _tbUsername;
    private readonly TextBox  _tbPassword;
    private readonly ILdap?   _ldap;
    private readonly string   _requiredDomain;
    private readonly string   _requiredGroup;
    private readonly string   _ldapAttribute;
    private readonly bool     _getGroups;
    private readonly string?  _domainController;
    private readonly bool     _doNotFallback;
    private readonly int      _maxRetry;
    private int               _attempts;

    // Domain control is either a TextBox or ComboBox depending on whether a List was provided.
    private readonly Control  _domainCtrl;

    public string AuthenticatedUser   { get; private set; } = string.Empty;
    public string AuthenticatedDomain { get; private set; } = string.Empty;
    public string AuthUserGroups      { get; private set; } = string.Empty;
    public string AuthUserAttr        { get; private set; } = string.Empty;

    public DlgUserAuth(
        DialogTraits        traits,
        ITSEnv              env,
        string?             dlgTitle,
        string              requiredDomain,
        string              requiredGroup,
        string              ldapAttribute,
        bool                getGroups,
        bool                showBack,
        bool                disableCancel,
        int                 maxRetry,
        string?             domainController,
        bool                doNotFallback,
        ILdap?              ldap,
        UserAuthFieldSpec?  usernameSpec  = null,
        UserAuthFieldSpec?  passwordSpec  = null,
        UserAuthFieldSpec?  domainSpec    = null)
        : base(traits, env, dlgTitle ?? "Authentication Required")
    {
        _ldap             = ldap;
        _requiredDomain   = requiredDomain;
        _requiredGroup    = requiredGroup;
        _ldapAttribute    = ldapAttribute;
        _getGroups        = getGroups;
        _domainController = domainController;
        _doNotFallback    = doNotFallback;
        _maxRetry         = maxRetry;
        BtnBack.Visible   = showBack;
        BtnCancel.Enabled = !disableCancel;

        int y = 20;

        // Username field
        AddPrompt(ContentPanel, usernameSpec?.Question ?? "Username:", 8, y);
        _tbUsername = new TextBox
        {
            Bounds   = new Rectangle(140, y, 250, 24),
            ReadOnly = usernameSpec?.ReadOnly ?? false,
        };
        if (!string.IsNullOrWhiteSpace(usernameSpec?.Hint))
            _tbUsername.PlaceholderText = usernameSpec.Hint;
        ContentPanel.Controls.Add(_tbUsername);
        y += 36;

        // Password field
        AddPrompt(ContentPanel, passwordSpec?.Question ?? "Password:", 8, y);
        _tbPassword = new TextBox
        {
            PasswordChar = '●',
            Bounds       = new Rectangle(140, y, 250, 24),
            ReadOnly     = passwordSpec?.ReadOnly ?? false,
        };
        if (!string.IsNullOrWhiteSpace(passwordSpec?.Hint))
            _tbPassword.PlaceholderText = passwordSpec.Hint;
        ContentPanel.Controls.Add(_tbPassword);
        y += 36;

        // Domain field — ComboBox when a List of choices is provided, TextBox otherwise.
        AddPrompt(ContentPanel, domainSpec?.Question ?? "Domain:", 8, y);
        if (domainSpec?.DomainList is { Count: > 0 } list)
        {
            var cb = new ComboBox
            {
                DropDownStyle    = domainSpec.AutoComplete
                    ? ComboBoxStyle.DropDown
                    : ComboBoxStyle.DropDownList,
                MaxDropDownItems = list.Count,
                Bounds           = new Rectangle(140, y, 250, 24),
                Enabled          = !(domainSpec.ReadOnly),
            };
            foreach (var item in list)
                cb.Items.Add(item);

            // Pre-select the required domain if it appears in the list.
            int idx = list.ToList().IndexOf(requiredDomain);
            if (idx >= 0)
                cb.SelectedIndex = idx;
            else if (cb.Items.Count > 0)
                cb.SelectedIndex = 0;

            ContentPanel.Controls.Add(cb);
            _domainCtrl = cb;
        }
        else
        {
            var tb = new TextBox
            {
                Text     = requiredDomain,
                Bounds   = new Rectangle(140, y, 250, 24),
                ReadOnly = domainSpec?.ReadOnly ?? false,
            };
            if (!string.IsNullOrWhiteSpace(domainSpec?.Hint))
                tb.PlaceholderText = domainSpec.Hint;
            ContentPanel.Controls.Add(tb);
            _domainCtrl = tb;
        }
    }

    private static void AddPrompt(Control parent, string text, int x, int y) =>
        parent.Controls.Add(new Label
        {
            Text      = text,
            AutoSize  = false,
            TextAlign = ContentAlignment.MiddleRight,
            Bounds    = new Rectangle(x, y, 128, 24),
        });

    protected override bool ValidateInput()
    {
        var rawUser  = _tbUsername.Text.Trim();
        var password = _tbPassword.Text;
        var domain   = _domainCtrl.Text.Trim();

        if (string.IsNullOrWhiteSpace(rawUser))
        {
            MessageBox.Show("Username is required.", "Authentication",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _tbUsername.Focus();
            return false;
        }

        // Parse domain\user or user@domain notation from the username field.
        string user = rawUser;
        if (rawUser.Contains('\\'))
        {
            var parts = rawUser.Split('\\', 2);
            domain = parts[0];
            user   = parts[1];
        }
        else if (rawUser.Contains('@'))
        {
            var parts = rawUser.Split('@', 2);
            user   = parts[0];
            domain = parts[1];
        }

        if (_ldap is not null)
        {
            if (!_ldap.Authenticate(user, password, domain, _domainController))
            {
                _attempts++;
                if (_maxRetry > 0 && _attempts >= _maxRetry)
                {
                    // Max retries exceeded — close with Cancel result.
                    DialogResult = System.Windows.Forms.DialogResult.Cancel;
                    Close();
                    return false;
                }

                MessageBox.Show("Authentication failed. Please check your credentials.",
                    "Authentication", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _tbPassword.Clear();
                _tbPassword.Focus();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(_requiredGroup))
            {
                var groups = _ldap.GetGroupMembership(user, domain);
                if (!groups.Any(g => g.Equals(_requiredGroup, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show($"You are not a member of the required group '{_requiredGroup}'.",
                        "Authentication", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                AuthUserGroups = string.Join(",", groups);
            }
            else if (_getGroups)
            {
                AuthUserGroups = string.Join(",", _ldap.GetGroupMembership(user, domain));
            }

            if (!string.IsNullOrWhiteSpace(_ldapAttribute))
                AuthUserAttr = _ldap.GetAttribute(user, domain, _ldapAttribute) ?? string.Empty;
        }

        AuthenticatedUser   = user;
        AuthenticatedDomain = domain;
        return true;
    }
}
