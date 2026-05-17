using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UIpp.Core.Variables;

namespace UIpp.Windows.Variables;

// ITSEnv backed by the ConfigMgr SMS_TSEnvironment COM object.
// Falls back to an in-memory dictionary when not running inside a task sequence.
public sealed class ConfigMgrTSEnv : ITSEnv
{
    private const string ProgId = "Microsoft.SMS.TSEnvironment";

    private static readonly Regex SubstPattern =
        new(@"%([^%]+)%", RegexOptions.Compiled);

    // COM object — null when not in a task sequence.
    private readonly object? _com;

    // In-process fallback when COM is unavailable.
    private readonly Dictionary<string, string> _local =
        new(StringComparer.OrdinalIgnoreCase);

    public ConfigMgrTSEnv()
    {
        try
        {
            var type = Type.GetTypeFromProgID(ProgId);
            if (type is not null)
                _com = Activator.CreateInstance(type);
        }
        catch { /* not in a task sequence */ }
    }

    public bool InTS    => _com is not null;
    public string? LogPath => InTS ? Get("_SMSTSLogPath") : null;

    public string Get(string name)
    {
        if (_com is not null)
        {
            try { return ((dynamic)_com)[name] as string ?? string.Empty; }
            catch { return string.Empty; }
        }
        return _local.TryGetValue(name, out var v) ? v : string.Empty;
    }

    public bool TryGet(string name, out string value)
    {
        value = Get(name);
        return !string.IsNullOrEmpty(value) || Exists(name);
    }

    public bool Exists(string name) => !string.IsNullOrEmpty(Get(name));

    public void Set(string name, string value)
    {
        if (_com is not null)
        {
            try { ((dynamic)_com)[name] = value; return; }
            catch { /* fall through to local */ }
        }
        _local[name] = value;
    }

    public void Set(string name, ulong value) => Set(name, value.ToString());

    public string Substitute(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.Contains('%'))
            return input;

        string current = input;
        for (int pass = 0; pass < 10; pass++)
        {
            var next = SubstPattern.Replace(current, m =>
            {
                var key = m.Groups[1].Value;
                var val = Get(key);
                if (!string.IsNullOrEmpty(val)) return val;
                var env = Environment.GetEnvironmentVariable(key);
                return env ?? m.Value;
            });
            if (next == current) break;
            current = next;
            if (!current.Contains('%')) break;
        }
        return current;
    }

    private static bool IsExcluded(string key) =>
        key.Length > 0 && (key[0] == 'X' || key[0] == '_');

    public void DumpToFile(string? path = null)
    {
        path = Substitute(path ?? @"%temp%\ui++vars.dat");
        var lines = _local
            .Where(kv => !IsExcluded(kv.Key))
            .Select(kv => $"{kv.Key}={kv.Value}");
        File.WriteAllLines(path, lines);
    }

    public void SaveToFile(string? path = null) => DumpToFile(path);

    public void LoadFromFile(string? path = null)
    {
        path = Substitute(path ?? @"%temp%\ui++vars.dat");
        if (!File.Exists(path)) return;

        foreach (var line in File.ReadAllLines(path))
        {
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line[..eq].Trim();
            var val = line[(eq + 1)..];
            if (!IsExcluded(key))
                Set(key, val);
        }
    }
}
