using System.DirectoryServices.Protocols;
using System.Net;
using UiSharp.Core.Ldap;

namespace UiSharp.Windows.Ldap;

// ILdap implementation using System.DirectoryServices.Protocols (raw LDAP, no ADSI).
// System.DirectoryServices.Protocols does not depend on adsldp.dll, making it compatible
// with WinPE environments that lack the ADSI COM infrastructure.
public sealed class WindowsLdap : ILdap
{
    public bool Authenticate(string username, string password, string domain,
                             string? domainController = null)
    {
        var host = domainController ?? domain;
        try
        {
            var id   = new LdapDirectoryIdentifier(host, 389);
            var cred = new NetworkCredential($"{domain}\\{username}", password);
            using var conn = new LdapConnection(id, cred, AuthType.Ntlm)
            {
                Timeout = TimeSpan.FromSeconds(30),
            };
            conn.Bind();
            return true;
        }
        catch { return false; }
    }

    public IReadOnlyList<string> GetGroupMembership(string username, string domain)
    {
        var groups = new List<string>();
        try
        {
            using var conn = OpenAnonymous(domain);
            var request = new SearchRequest(
                DomainToDn(domain),
                $"(&(objectClass=user)(sAMAccountName={EscapeLdap(username)}))",
                SearchScope.Subtree,
                "memberOf");

            if (conn.SendRequest(request) is not SearchResponse response) return groups;

            foreach (SearchResultEntry entry in response.Entries)
            {
                if (!entry.Attributes.Contains("memberOf")) continue;
                foreach (object val in entry.Attributes["memberOf"].GetValues(typeof(string)))
                {
                    var cn = ExtractCn(val?.ToString() ?? string.Empty);
                    if (!string.IsNullOrEmpty(cn))
                        groups.Add(cn);
                }
            }
        }
        catch { /* domain unreachable or user not found */ }
        return groups;
    }

    public string? GetAttribute(string username, string domain, string attribute)
    {
        try
        {
            using var conn = OpenAnonymous(domain);
            var request = new SearchRequest(
                DomainToDn(domain),
                $"(&(objectClass=user)(sAMAccountName={EscapeLdap(username)}))",
                SearchScope.Subtree,
                attribute);

            if (conn.SendRequest(request) is not SearchResponse response) return null;
            if (response.Entries.Count == 0) return null;

            var entry = response.Entries[0];
            if (!entry.Attributes.Contains(attribute)) return null;

            var vals = entry.Attributes[attribute].GetValues(typeof(string));
            return vals.Length > 0 ? vals[0]?.ToString() : null;
        }
        catch { return null; }
    }

    // Opens an unauthenticated connection for directory searches.
    // Many AD configurations allow anonymous reads of memberOf and user attributes.
    private static LdapConnection OpenAnonymous(string domain)
    {
        var id   = new LdapDirectoryIdentifier(domain, 389);
        var conn = new LdapConnection(id) { Timeout = TimeSpan.FromSeconds(30) };
        conn.SessionOptions.ProtocolVersion = 3;
        return conn;
    }

    // Converts a DNS domain name to an LDAP base DN: "corp.example.com" → "DC=corp,DC=example,DC=com"
    private static string DomainToDn(string domain) =>
        string.Join(",", domain.Split('.').Select(part => $"DC={part}"));

    // Escapes special characters per RFC 4515 for use in LDAP filter values.
    private static string EscapeLdap(string input) =>
        input
            .Replace("\\", "\\5c")
            .Replace("*",  "\\2a")
            .Replace("(",  "\\28")
            .Replace(")",  "\\29")
            .Replace("\0", "\\00");

    // Extracts the CN value from an LDAP DN: "CN=GroupName,OU=..." → "GroupName"
    private static string ExtractCn(string dn)
    {
        if (!dn.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
            return dn;
        var comma = dn.IndexOf(',');
        return comma < 0 ? dn[3..] : dn[3..comma];
    }
}
