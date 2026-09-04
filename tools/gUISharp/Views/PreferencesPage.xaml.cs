using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UiSharp.Editor.Views;

public sealed partial class PreferencesPage : Page
{
    // ── Update check (cached for the session) ─────────────────────────────────
    private static readonly HttpClient _http;
    private static bool    _updateChecked;
    private static string? _updateStatusText;
    private static string? _updateDownloadUrl;

    static PreferencesPage()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "gUISharp UpdateCheck");
    }

    public PreferencesPage()
    {
        this.InitializeComponent();
    }

    private void PreferencesPage_Loaded(object sender, RoutedEventArgs e)
    {
        var s = App.UserSettings.Settings;

        RecentFilesLimitBox.Value = s.RecentFilesLimit;
        ConfigMgrServerBox.Text   = s.ConfigMgrServer;
        ConfigMgrSiteCodeBox.Text = s.ConfigMgrSiteCode;

        switch (s.DefaultPanelLayout)
        {
            case "GuidedOnly": LayoutGuidedOnly.IsChecked = true; break;
            case "XmlOnly":    LayoutXmlOnly.IsChecked    = true; break;
            default:           LayoutBoth.IsChecked       = true; break;
        }

        var infoVer = typeof(App).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? string.Empty;
        var plus          = infoVer.IndexOf('+');
        var version       = plus >= 0 ? infoVer[..plus] : infoVer;
        var commit        = plus >= 0 ? infoVer[(plus + 1)..] : null;
        var isPreRelease  = version.Contains('-');

        var verParts = new List<string> { $"Version {version}" };
        if (commit is { Length: > 0 }) verParts.Add(commit);
        if (isPreRelease)              verParts.Add("pre-release");
        VersionText.Text = string.Join("  ·  ", verParts);

        if (_updateChecked)
            ApplyUpdateResult(_updateStatusText, _updateDownloadUrl);
        else
            _ = CheckForUpdatesAsync(version);
    }

    // ── Settings handlers ─────────────────────────────────────────────────────

    private void RecentFilesLimit_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!IsLoaded || double.IsNaN(sender.Value)) return;
        App.UserSettings.Settings.RecentFilesLimit = (int)sender.Value;
        App.UserSettings.Save();
    }

    private void ConfigMgrServer_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        App.UserSettings.Settings.ConfigMgrServer = ConfigMgrServerBox.Text.Trim();
        App.UserSettings.Save();
    }

    private void ConfigMgrSiteCode_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        App.UserSettings.Settings.ConfigMgrSiteCode = ConfigMgrSiteCodeBox.Text.Trim();
        App.UserSettings.Save();
    }

    private void Layout_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        App.UserSettings.Settings.DefaultPanelLayout =
            LayoutGuidedOnly.IsChecked == true ? "GuidedOnly" :
            LayoutXmlOnly.IsChecked    == true ? "XmlOnly"    : "Both";
        App.UserSettings.Save();
    }

    // ── Update check ──────────────────────────────────────────────────────────

    private async Task CheckForUpdatesAsync(string currentVersion)
    {
        UpdateProgress.Visibility = Visibility.Visible;
        UpdateProgress.IsActive   = true;
        UpdateStatusText.Text     = "Checking for updates…";

        string? statusText  = null;
        string? downloadUrl = null;

        try
        {
            var json     = await _http.GetStringAsync("https://api.github.com/repos/altrhombus/UiSharp/releases");
            var releases = JsonSerializer.Deserialize<GitHubRelease[]>(json) ?? [];

            var isPreRelease = currentVersion.Contains('-');
            var newer = releases
                .Where(r => !r.Draft && (isPreRelease || !r.Prerelease))
                .Where(r => IsNewer(currentVersion, r.TagName))
                .OrderByDescending(r => ParseNumericVersion(r.TagName))
                .ThenByDescending(r => r.Prerelease ? 0 : 1)
                .FirstOrDefault();

            if (newer is not null)
            {
                var label = newer.Prerelease ? $"{newer.TagName} (pre-release)" : newer.TagName;
                statusText  = $"Update available: {label}";
                downloadUrl = newer.HtmlUrl;
            }
            else
            {
                statusText = "gUI# is up to date.";
            }
        }
        catch
        {
            statusText = "Could not check for updates.";
        }

        _updateStatusText = statusText;
        _updateDownloadUrl = downloadUrl;
        _updateChecked    = true;

        UpdateProgress.IsActive   = false;
        UpdateProgress.Visibility = Visibility.Collapsed;
        ApplyUpdateResult(statusText, downloadUrl);
    }

    private void ApplyUpdateResult(string? statusText, string? downloadUrl)
    {
        UpdateStatusText.Text = statusText ?? string.Empty;
        if (downloadUrl is not null)
        {
            UpdateDownloadLink.NavigateUri = new Uri(downloadUrl);
            UpdateDownloadLink.Content     = "View release on GitHub ↗";
            UpdateDownloadLink.Visibility  = Visibility.Visible;
        }
        else
        {
            UpdateDownloadLink.Visibility = Visibility.Collapsed;
        }
    }

    private static bool IsNewer(string current, string candidate)
    {
        var cv   = ParseNumericVersion(current);
        var nv   = ParseNumericVersion(candidate);
        var cpre = PreReleaseSuffix(current);
        var npre = PreReleaseSuffix(candidate);

        if (nv != cv) return nv > cv;
        if (cpre != "" && npre == "") return true;   // release > any pre-release of same version
        if (cpre != "" && npre != "") return string.CompareOrdinal(npre, cpre) > 0;
        return false;
    }

    private static Version ParseNumericVersion(string tag)
    {
        var s    = tag.TrimStart('v');
        var dash = s.IndexOf('-');
        if (dash >= 0) s = s[..dash];
        return Version.TryParse(s, out var v) ? v : new Version(0, 0, 0);
    }

    private static string PreReleaseSuffix(string tag)
    {
        var s    = tag.TrimStart('v');
        var dash = s.IndexOf('-');
        return dash >= 0 ? s[(dash + 1)..] : "";
    }

    private record GitHubRelease(
        [property: JsonPropertyName("tag_name")]   string TagName,
        [property: JsonPropertyName("prerelease")] bool   Prerelease,
        [property: JsonPropertyName("draft")]      bool   Draft,
        [property: JsonPropertyName("html_url")]   string HtmlUrl
    );
}
