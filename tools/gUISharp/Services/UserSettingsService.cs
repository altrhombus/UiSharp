using System.Text.Json;
using System.Text.Json.Serialization;

namespace GUISharp.Services;

public sealed class UserSettings
{
    public int           RecentFilesLimit   { get; set; } = 10;
    public bool          NavPaneOpen        { get; set; } = true;
    public List<string>  RecentFiles        { get; set; } = new();
    public string        ConfigMgrServer    { get; set; } = string.Empty;
    public string        ConfigMgrSiteCode  { get; set; } = string.Empty;

    [JsonIgnore]
    public static UserSettings Default => new();
}

public sealed class UserSettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "gUISharp", "settings.json");

    private static readonly JsonSerializerOptions JsonOpts =
        new() { WriteIndented = true };

    public UserSettings Settings { get; } = Load();

    private static readonly string CrashLogPath = Path.Combine(
        Path.GetTempPath(), "guisharp_crash.txt");

    private static UserSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new UserSettings();
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<UserSettings>(json, JsonOpts)
                   ?? new UserSettings();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.Fail($"Failed to load user settings: {ex.Message}");
            return new UserSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Settings, JsonOpts));
        }
        catch (Exception ex)
        {
            File.AppendAllText(CrashLogPath,
                $"[{DateTime.Now:HH:mm:ss}] Settings save failed: {ex.Message}{Environment.NewLine}");
        }
    }
}
