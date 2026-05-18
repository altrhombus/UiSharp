using System.Management;

namespace GUISharp.Services;

public sealed class ConfigMgrService : IConfigMgrService
{
    public Task<IReadOnlyList<CmApplicationEntry>> GetApplicationsAsync(string server, string siteCode) =>
        Task.Run(() => QueryApplications(server, siteCode));

    public Task<IReadOnlyList<CmPackageEntry>> GetPackagesAsync(string server, string siteCode) =>
        Task.Run(() => QueryPackages(server, siteCode));

    private static IReadOnlyList<CmApplicationEntry> QueryApplications(string server, string siteCode)
    {
        var scope = Connect(server, siteCode);
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

    private static IReadOnlyList<CmPackageEntry> QueryPackages(string server, string siteCode)
    {
        var scope = Connect(server, siteCode);
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

    private static ManagementScope Connect(string server, string siteCode)
    {
        var scope = new ManagementScope($@"\\{server}\root\SMS\site_{siteCode}");
        scope.Connect();
        return scope;
    }
}
