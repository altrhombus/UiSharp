using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using System.Management;
using System.Net;
using System.Security;

namespace GUISharp.Services;

public sealed class ConfigMgrService : IConfigMgrService
{
    public Task<IReadOnlyList<CmApplicationEntry>> GetApplicationsAsync(string server, string siteCode, NetworkCredential? credential = null) =>
        Task.Run(() => QueryApplications(server, siteCode, credential));

    public Task<IReadOnlyList<CmPackageEntry>> GetPackagesAsync(string server, string siteCode, NetworkCredential? credential = null) =>
        Task.Run(() => QueryPackages(server, siteCode, credential));

    private static IReadOnlyList<CmApplicationEntry> QueryApplications(string server, string siteCode, NetworkCredential? credential)
    {
        var ns  = $@"root\SMS\site_{siteCode}";
        const string wql = "SELECT LocalizedDisplayName, LocalizedDescription FROM SMS_Application WHERE IsLatest = 1";
        var result = new List<CmApplicationEntry>();

        if (credential is null)
        {
            using var searcher = new ManagementObjectSearcher(
                ConnectDcom(server, ns), new ObjectQuery(wql));
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = (string?)obj["LocalizedDisplayName"] ?? string.Empty;
                var desc = (string?)obj["LocalizedDescription"] ?? string.Empty;
                if (!string.IsNullOrEmpty(name))
                    result.Add(new CmApplicationEntry(name, desc));
            }
        }
        else
        {
            using var session = ConnectWsman(server, credential);
            foreach (var inst in session.QueryInstances(ns, "WQL", wql))
            {
                var name = (string?)inst.CimInstanceProperties["LocalizedDisplayName"]?.Value ?? string.Empty;
                var desc = (string?)inst.CimInstanceProperties["LocalizedDescription"]?.Value ?? string.Empty;
                if (!string.IsNullOrEmpty(name))
                    result.Add(new CmApplicationEntry(name, desc));
            }
        }

        result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return result;
    }

    private static IReadOnlyList<CmPackageEntry> QueryPackages(string server, string siteCode, NetworkCredential? credential)
    {
        var ns  = $@"root\SMS\site_{siteCode}";
        const string wql = "SELECT PackageID, Name FROM SMS_Package";
        var result = new List<CmPackageEntry>();

        if (credential is null)
        {
            using var searcher = new ManagementObjectSearcher(
                ConnectDcom(server, ns), new ObjectQuery(wql));
            foreach (ManagementObject obj in searcher.Get())
            {
                var pkgId = (string?)obj["PackageID"] ?? string.Empty;
                var name  = (string?)obj["Name"]       ?? string.Empty;
                if (!string.IsNullOrEmpty(pkgId))
                    result.Add(new CmPackageEntry(pkgId, name));
            }
        }
        else
        {
            using var session = ConnectWsman(server, credential);
            foreach (var inst in session.QueryInstances(ns, "WQL", wql))
            {
                var pkgId = (string?)inst.CimInstanceProperties["PackageID"]?.Value ?? string.Empty;
                var name  = (string?)inst.CimInstanceProperties["Name"]?.Value       ?? string.Empty;
                if (!string.IsNullOrEmpty(pkgId))
                    result.Add(new CmPackageEntry(pkgId, name));
            }
        }

        result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return result;
    }

    private static ManagementScope ConnectDcom(string server, string ns)
    {
        var scope = new ManagementScope($@"\\{server}\{ns}");
        scope.Connect();
        return scope;
    }

    // Alternate credentials use WS-MAN (WinRM) instead of DCOM. DCOM Negotiate auth
    // tries NTLM first; NTLM is blocked on the remote server for accounts in the
    // Protected Users security group. WS-MAN uses Kerberos over HTTP and is not
    // affected by that restriction. WinRM must be enabled on the SMS Provider.
    private static CimSession ConnectWsman(string server, NetworkCredential credential)
    {
        var secure = new SecureString();
        foreach (char c in credential.Password) secure.AppendChar(c);
        secure.MakeReadOnly();

        var options = new WSManSessionOptions();
        options.AddDestinationCredentials(new CimCredential(
            PasswordAuthenticationMechanism.Kerberos,
            credential.Domain,
            credential.UserName,
            secure));

        return CimSession.Create(server, options);
    }
}
