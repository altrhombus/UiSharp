using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UIpp.Core.Variables;

// Non-TS dictionary-backed ITSEnv — used outside a real task sequence (dev/test/debug).
// Variable substitution supports %VAR% patterns resolved against stored variables,
// then falls back to environment variables for anything unmatched.
public sealed class LocalTSEnv : ITSEnv
{
    private readonly Dictionary<string, string> _vars =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Regex SubstPattern =
        new(@"%([^%]+)%", RegexOptions.Compiled);

    public bool InTS => false;
    public string? LogPath => null;

    public string Get(string name) =>
        _vars.TryGetValue(name, out var v) ? v : string.Empty;

    public bool TryGet(string name, out string value) =>
        _vars.TryGetValue(name, out value!);

    public bool Exists(string name) => _vars.ContainsKey(name);

    public void Set(string name, string value) => _vars[name] = value;
    public void Set(string name, ulong value)  => _vars[name] = value.ToString();

    public string Substitute(string input)
    {
        if (string.IsNullOrEmpty(input) || !input.Contains('%'))
            return input;

        return SubstPattern.Replace(input, m =>
        {
            var key = m.Groups[1].Value;
            if (_vars.TryGetValue(key, out var val))
                return val;
            var env = Environment.GetEnvironmentVariable(key);
            return env ?? m.Value;
        });
    }

    // Variables starting with 'X' or '_' are excluded from save/load (matches C++ behaviour).
    private static bool IsExcluded(string key) =>
        key.Length > 0 && (key[0] == 'X' || key[0] == '_');

    public void DumpToFile(string? path = null)
    {
        path ??= @"%temp%\ui++vars.dat";
        path = Substitute(path);

        var sb = new StringBuilder();
        foreach (var (k, v) in _vars)
        {
            if (!IsExcluded(k))
                sb.AppendLine($"{k}={v}");
        }
        File.WriteAllText(path, sb.ToString());
    }

    public void SaveToFile(string? path = null)
    {
        path ??= @"%temp%\ui++vars.dat";
        path = Substitute(path);

        var filtered = _vars
            .Where(kv => !IsExcluded(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

        var json = JsonSerializer.Serialize(filtered, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public void LoadFromFile(string? path = null)
    {
        path ??= @"%temp%\ui++vars.dat";
        path = Substitute(path);

        if (!File.Exists(path))
            return;

        var json = File.ReadAllText(path);
        var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        if (loaded is null) return;

        foreach (var (k, v) in loaded)
        {
            if (!IsExcluded(k))
                _vars[k] = v;
        }
    }
}
