namespace UiSharp.Core.Ldap;

public interface ILdap
{
    bool Authenticate(string username, string password, string domain, string? domainController = null);
    IReadOnlyList<string> GetGroupMembership(string username, string domain);
    string? GetAttribute(string username, string domain, string attribute);
}
