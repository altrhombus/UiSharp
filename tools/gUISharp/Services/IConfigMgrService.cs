namespace GUISharp.Services;

public interface IConfigMgrService
{
    Task<IReadOnlyList<CmApplicationEntry>> GetApplicationsAsync(string server, string siteCode);
    Task<IReadOnlyList<CmPackageEntry>>     GetPackagesAsync(string server, string siteCode);
}

public sealed record CmApplicationEntry(string Name, string Description);
public sealed record CmPackageEntry(string PackageId, string Name);
