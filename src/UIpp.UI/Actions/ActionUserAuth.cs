using System.Xml.Linq;
using UIpp.Core.Actions;
using UIpp.Core.Configuration;
using UIpp.UI.Dialogs;

namespace UIpp.UI.Actions;

[ActionType(XmlConstants.ActionTypes.UserAuth)]
public sealed class ActionUserAuth(ActionData data) : ActionBase(data)
{
    public override bool IsGuiAction => true;

    public override ActionResult Go()
    {
        var title    = Attr(XmlConstants.Attributes.Title) is { Length: > 0 } t ? t : null;
        var showBack = BoolAttr(XmlConstants.Attributes.ShowBack);

        // Domain: prefer previously authenticated domain TSVar, fall back to attribute.
        var domain = Data.TsEnv.Get(XmlConstants.Variables.AuthUserDomain);
        if (string.IsNullOrWhiteSpace(domain))
            domain = Attr(XmlConstants.Attributes.Domain);

        var requiredGroup     = Attr(XmlConstants.Attributes.Group);
        var getGroups         = BoolAttr(XmlConstants.Attributes.GetGroups);
        var ldapAttribute     = Attr(XmlConstants.Attributes.RestAttributes); // "Attributes" in C++
        var disableCancel     = BoolAttr(XmlConstants.Attributes.DisableCancel);
        var domainController  = Attr(XmlConstants.Attributes.DomainController);
        var maxRetryStr       = Attr(XmlConstants.Attributes.MaxRetry, XmlConstants.Defaults.MaxRetry);
        int maxRetry          = int.TryParse(maxRetryStr, out var mr) ? mr : 5;

        var (usernameSpec, passwordSpec, domainSpec) = ParseFieldSpecs();

        using var dlg = new DlgUserAuth(
            Data.GlobalDialogTraits,
            Data.TsEnv,
            title,
            domain,
            requiredGroup,
            ldapAttribute,
            getGroups,
            showBack,
            disableCancel,
            maxRetry,
            domainController,
            Data.Ldap,
            usernameSpec,
            passwordSpec,
            domainSpec);

        dlg.ShowDialog();
        var result = dlg.Result;

        if (result == ActionResult.Next)
        {
            Data.TsEnv.Set(XmlConstants.Variables.AuthUser,       dlg.AuthenticatedUser);
            Data.TsEnv.Set(XmlConstants.Variables.AuthUserDomain, dlg.AuthenticatedDomain);
            Data.TsEnv.Set(XmlConstants.Variables.AuthUserGroups, dlg.AuthUserGroups);

            if (!string.IsNullOrWhiteSpace(ldapAttribute))
                Data.TsEnv.Set(XmlConstants.Variables.AuthUserAttr, dlg.AuthUserAttr);
        }

        return result;
    }

    // Parses <Field name="username|password|domain"> children and returns per-field specs.
    // C++: DlgUserAuthData reads Question, Hint, ReadOnly, List, AutoComplete per field.
    private (UserAuthFieldSpec? Username, UserAuthFieldSpec? Password, UserAuthFieldSpec? Domain)
        ParseFieldSpecs()
    {
        var map = new Dictionary<string, UserAuthFieldSpec>(StringComparer.OrdinalIgnoreCase);

        foreach (var el in Data.ActionNode.Elements(XmlConstants.Elements.Field))
        {
            // C++ uses lowercase "name" attribute on <Field> elements.
            var fieldName = Attr(el, "name") is { Length: > 0 } n
                ? n
                : Attr(el, XmlConstants.Attributes.Name);
            if (string.IsNullOrWhiteSpace(fieldName)) continue;

            // Read through Attr so variables are substituted, as the original does
            // for these very attributes (InteractiveActions.cpp:297-320). A null
            // means "absent", which the dialog replaces with its built-in default.
            var question     = Attr(el, XmlConstants.Attributes.Question) is { Length: > 0 } q ? q : null;
            var hint         = Attr(el, XmlConstants.Attributes.Hint)     is { Length: > 0 } h ? h : null;
            var readOnly     = BoolAttr(el, XmlConstants.Attributes.ReadOnly);
            var autoComplete = BoolAttr(el, XmlConstants.Attributes.AutoComplete);
            var listStr      = Attr(el, XmlConstants.Attributes.List);

            IReadOnlyList<string>? domainList = null;
            if (!string.IsNullOrWhiteSpace(listStr))
            {
                // C++ tokenizes this list on ",;" (InteractiveActions.cpp:332); the
                // port previously split on '|', so the comma-separated lists in the
                // sample configs collapsed into a single entry.
                domainList = listStr
                    .Split([',', ';'], StringSplitOptions.TrimEntries)
                    .Where(v => v.Length > 0)
                    .ToList();
            }

            map[fieldName] = new UserAuthFieldSpec(question, hint, readOnly, domainList, autoComplete);
        }

        return (
            map.GetValueOrDefault("username"),
            map.GetValueOrDefault("password"),
            map.GetValueOrDefault("domain")
        );
    }
}
