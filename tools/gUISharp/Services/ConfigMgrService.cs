using System.Management;
using System.Net;

namespace GUISharp.Services;

public sealed class ConfigMgrService : IConfigMgrService
{
    public Task<IReadOnlyList<CmApplicationEntry>> GetApplicationsAsync(string server, string siteCode, NetworkCredential? credential = null) =>
        Task.Run(() => QueryApplications(server, siteCode, credential));

    public Task<IReadOnlyList<CmPackageEntry>> GetPackagesAsync(string server, string siteCode, NetworkCredential? credential = null) =>
        Task.Run(() => QueryPackages(server, siteCode, credential));

    private static IReadOnlyList<CmApplicationEntry> QueryApplications(string server, string siteCode, NetworkCredential? credential)
    {
        var scope = Connect(server, siteCode, credential);
        var result = new List<CmApplicationEntry>();
        using var searcher = new ManagementObjectSearcher(scope,
            new ObjectQuery("SELECT LocalizedDisplayName, LocalizedDescription FROM SMS_Application WHERE IsLatest = 1"));
        foreach (ManagementObject obj in searcher.Get())
        {
            var name = (string?)obj["LocalizedDisplayName"] ?? string.Empty;
            var desc = (string?)obj["LocalizedDescription"] ?? string.Empty;
            if (!string.IsNullOrEmpty(name))
                result.Add(new CmApplicationEntry(name, desc));
        }
        result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return result;
    }

    private static IReadOnlyList<CmPackageEntry> QueryPackages(string server, string siteCode, NetworkCredential? credential)
    {
        var scope = Connect(server, siteCode, credential);
        var result = new List<CmPackageEntry>();
        using var searcher = new ManagementObjectSearcher(scope,
            new ObjectQuery("SELECT PackageID, Name FROM SMS_Package"));
        foreach (ManagementObject obj in searcher.Get())
        {
            var pkgId = (string?)obj["PackageID"] ?? string.Empty;
            var name  = (string?)obj["Name"]       ?? string.Empty;
            if (!string.IsNullOrEmpty(pkgId))
                result.Add(new CmPackageEntry(pkgId, name));
        }
        result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return result;
    }

    private static ManagementScope Connect(string server, string siteCode, NetworkCredential? credential)
    {
        var path = $@"\\{server}\root\SMS\site_{siteCode}";
        if (credential is null)
        {
            var scope = new ManagementScope(path);
            scope.Connect();
            return scope;
        }
        // Kerberos:{domain}\{server} matches what the ConfigMgr admin console uses and
        // is required for Protected Users group members (who have NTLM disabled).
        // UPN-format credentials (username@domain.com) carry no explicit domain, so
        // we let DCOM negotiate the auth package for those.
        var options = new ConnectionOptions
        {
            Username       = credential.UserName,
            Password       = credential.Password,
            Authority      = string.IsNullOrEmpty(credential.Domain)
                ? null
                : $@"Kerberos:{credential.Domain}\{server}",
            Impersonation  = ImpersonationLevel.Impersonate,
            Authentication = AuthenticationLevel.PacketPrivacy,
        };
        var scopeWithCreds = new ManagementScope(path, options);
        scopeWithCreds.Connect();
        return scopeWithCreds;
    }
}
