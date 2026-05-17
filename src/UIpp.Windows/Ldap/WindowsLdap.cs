using System.DirectoryServices;
using UIpp.Core.Ldap;

namespace UIpp.Windows.Ldap;

// ILdap implementation backed by System.DirectoryServices (ADSI).
// Works against on-premises Active Directory from both WinPE and full OS.
public sealed class WindowsLdap : ILdap
{
    public bool Authenticate(string username, string password, string domain,
                             string? domainController = null)
    {
        var path = domainController is not null
            ? $"LDAP://{domainController}"
            : $"LDAP://{domain}";

        try
        {
            using var entry = new DirectoryEntry(path,
                $"{domain}\\{username}", password,
                AuthenticationTypes.Secure);

            // Accessing NativeObject forces the bind — throws on bad credentials.
            _ = entry.NativeObject;
            return true;
        }
        catch { return false; }
    }

    public IReadOnlyList<string> GetGroupMembership(string username, string domain)
    {
        var groups = new List<string>();
        try
        {
            using var root     = new DirectoryEntry($"LDAP://{domain}");
            using var searcher = new DirectorySearcher(root)
            {
                Filter = $"(&(objectClass=user)(sAMAccountName={EscapeLdap(username)}))",
            };
            searcher.PropertiesToLoad.Add("memberOf");

            var result = searcher.FindOne();
            if (result is null) return groups;

            foreach (var obj in result.Properties["memberOf"])
            {
                var cn = ExtractCn(obj?.ToString() ?? string.Empty);
                if (!string.IsNullOrEmpty(cn))
                    groups.Add(cn);
            }
        }
        catch { /* domain unreachable or user not found */ }
        return groups;
    }

    public string? GetAttribute(string username, string domain, string attribute)
    {
        try
        {
            using var root     = new DirectoryEntry($"LDAP://{domain}");
            using var searcher = new DirectorySearcher(root)
            {
                Filter = $"(&(objectClass=user)(sAMAccountName={EscapeLdap(username)}))",
            };
            searcher.PropertiesToLoad.Add(attribute);

            var result = searcher.FindOne();
            return result?.Properties[attribute]?[0]?.ToString();
        }
        catch { return null; }
    }

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
