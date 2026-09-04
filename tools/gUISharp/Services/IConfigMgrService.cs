using System.Net;

namespace UiSharp.Editor.Services;

public interface IConfigMgrService
{
    Task<IReadOnlyList<CmApplicationEntry>> GetApplicationsAsync(string server, string siteCode, NetworkCredential? credential = null);
    Task<IReadOnlyList<CmPackageEntry>>     GetPackagesAsync(string server, string siteCode, NetworkCredential? credential = null);
}

public sealed record CmApplicationEntry(string Name, string Description);
public sealed record CmPackageEntry(string PackageId, string Name);
