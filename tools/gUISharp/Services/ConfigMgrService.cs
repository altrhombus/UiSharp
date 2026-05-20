using System.Management;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GUISharp.Services;

public sealed class ConfigMgrService : IConfigMgrService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public Task<IReadOnlyList<CmApplicationEntry>> GetApplicationsAsync(string server, string siteCode, NetworkCredential? credential = null) =>
        credential is null
            ? Task.Run(() => QueryApplicationsDcom(server, siteCode))
            : QueryApplicationsAdminServiceAsync(server, credential);

    public Task<IReadOnlyList<CmPackageEntry>> GetPackagesAsync(string server, string siteCode, NetworkCredential? credential = null) =>
        credential is null
            ? Task.Run(() => QueryPackagesDcom(server, siteCode))
            : QueryPackagesAdminServiceAsync(server, credential);

    // ── DCOM (current user, no alternate credentials) ──────────────────────────

    private static IReadOnlyList<CmApplicationEntry> QueryApplicationsDcom(string server, string siteCode)
    {
        var scope = new ManagementScope($@"\\{server}\root\SMS\site_{siteCode}");
        scope.Connect();
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

    private static IReadOnlyList<CmPackageEntry> QueryPackagesDcom(string server, string siteCode)
    {
        var scope = new ManagementScope($@"\\{server}\root\SMS\site_{siteCode}");
        scope.Connect();
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

    // ── AdminService (alternate credentials, Kerberos over HTTPS) ──────────────
    //
    // DCOM Negotiate tries NTLM first; NTLM is blocked on the server for accounts
    // in the Protected Users security group. The ConfigMgr Administration Service
    // (https://{server}/AdminService/) uses HTTPS + Kerberos and is not subject to
    // that restriction. Requires ConfigMgr 2002+ and the AdminService to be enabled.

    private static async Task<IReadOnlyList<CmApplicationEntry>> QueryApplicationsAdminServiceAsync(
        string server, NetworkCredential credential)
    {
        using var http = CreateHttpClient(credential);
        var url  = $"https://{server}/AdminService/wmi/SMS_Application?$filter=IsLatest%20eq%20true&$select=LocalizedDisplayName,LocalizedDescription";
        var resp = await http.GetAsync(url).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        var data = JsonSerializer.Deserialize<ODataEnvelope<AppRecord>>(json, JsonOpts);

        var result = (data?.Value ?? [])
            .Where(a => !string.IsNullOrEmpty(a.LocalizedDisplayName))
            .Select(a => new CmApplicationEntry(a.LocalizedDisplayName, a.LocalizedDescription))
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return result;
    }

    private static async Task<IReadOnlyList<CmPackageEntry>> QueryPackagesAdminServiceAsync(
        string server, NetworkCredential credential)
    {
        using var http = CreateHttpClient(credential);
        var url  = $"https://{server}/AdminService/wmi/SMS_Package?$select=PackageID,Name";
        var resp = await http.GetAsync(url).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        var data = JsonSerializer.Deserialize<ODataEnvelope<PkgRecord>>(json, JsonOpts);

        var result = (data?.Value ?? [])
            .Where(p => !string.IsNullOrEmpty(p.PackageID))
            .Select(p => new CmPackageEntry(p.PackageID, p.Name))
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return result;
    }

    private static HttpClient CreateHttpClient(NetworkCredential credential)
    {
        var handler = new HttpClientHandler { Credentials = credential };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
    }

    private sealed class ODataEnvelope<T>
    {
        [JsonPropertyName("value")] public T[]? Value { get; set; }
    }

    private sealed class AppRecord
    {
        [JsonPropertyName("LocalizedDisplayName")] public string LocalizedDisplayName { get; set; } = string.Empty;
        [JsonPropertyName("LocalizedDescription")] public string LocalizedDescription { get; set; } = string.Empty;
    }

    private sealed class PkgRecord
    {
        [JsonPropertyName("PackageID")] public string PackageID { get; set; } = string.Empty;
        [JsonPropertyName("Name")]      public string Name      { get; set; } = string.Empty;
    }
}
