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
        var title    = SubstAttr(XmlConstants.Attributes.Title) is { Length: > 0 } t ? t : null;
        var showBack = BoolAttr(XmlConstants.Attributes.ShowBack);

        // Domain: prefer previously authenticated domain TSVar, fall back to attribute.
        var domain = Data.TsEnv.Get(XmlConstants.Variables.AuthUserDomain);
        if (string.IsNullOrWhiteSpace(domain))
            domain = SubstAttr(XmlConstants.Attributes.Domain);

        var requiredGroup     = Attr(XmlConstants.Attributes.Group);
        var getGroups         = BoolAttr(XmlConstants.Attributes.GetGroups);
        var ldapAttribute     = Attr(XmlConstants.Attributes.RestAttributes); // "Attributes" in C++
        var disableCancel     = BoolAttr(XmlConstants.Attributes.DisableCancel);
        var domainController  = SubstAttr(XmlConstants.Attributes.DomainController);
        var maxRetryStr       = Attr(XmlConstants.Attributes.MaxRetry) ?? XmlConstants.Defaults.MaxRetry;
        int maxRetry          = int.TryParse(maxRetryStr, out var mr) ? mr : 5;

        var (usernameSpec, passwordSpec, domainSpec) = ParseFieldSpecs(Data.ActionNode);

        ActionResult result = ActionResult.Next;

        var thread = new Thread(() =>
        {
            Application.EnableVisualStyles();
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
            result = dlg.Result;

            if (result == ActionResult.Next)
            {
                Data.TsEnv.Set(XmlConstants.Variables.AuthUser,       dlg.AuthenticatedUser);
                Data.TsEnv.Set(XmlConstants.Variables.AuthUserDomain, dlg.AuthenticatedDomain);
                Data.TsEnv.Set(XmlConstants.Variables.AuthUserGroups, dlg.AuthUserGroups);

                if (!string.IsNullOrWhiteSpace(ldapAttribute))
                    Data.TsEnv.Set(XmlConstants.Variables.AuthUserAttr, dlg.AuthUserAttr);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return result;
    }

    // Parses <Field name="username|password|domain"> children and returns per-field specs.
    // C++: DlgUserAuthData reads Question, Hint, ReadOnly, List, AutoComplete per field.
    private static (UserAuthFieldSpec? Username, UserAuthFieldSpec? Password, UserAuthFieldSpec? Domain)
        ParseFieldSpecs(System.Xml.Linq.XElement actionNode)
    {
        var map = new Dictionary<string, UserAuthFieldSpec>(StringComparer.OrdinalIgnoreCase);

        foreach (var el in actionNode.Elements(XmlConstants.Elements.Field))
        {
            // C++ uses lowercase "name" attribute on <Field> elements.
            var fieldName = (string?)el.Attribute("name") ?? (string?)el.Attribute(XmlConstants.Attributes.Name);
            if (string.IsNullOrWhiteSpace(fieldName)) continue;

            var question     = (string?)el.Attribute(XmlConstants.Attributes.Question);
            var hint         = (string?)el.Attribute(XmlConstants.Attributes.Hint);
            var readOnly     = ((string?)el.Attribute(XmlConstants.Attributes.ReadOnly))
                                   ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            var autoComplete = ((string?)el.Attribute(XmlConstants.Attributes.AutoComplete))
                                   ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            var listStr      = (string?)el.Attribute(XmlConstants.Attributes.List);

            IReadOnlyList<string>? domainList = null;
            if (!string.IsNullOrWhiteSpace(listStr))
                domainList = listStr.Split('|').Where(s => s.Length > 0).ToList();

            map[fieldName] = new UserAuthFieldSpec(question, hint, readOnly, domainList, autoComplete);
        }

        return (
            map.GetValueOrDefault("username"),
            map.GetValueOrDefault("password"),
            map.GetValueOrDefault("domain")
        );
    }
}
