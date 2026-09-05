using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UiSharp.Core.Variables;

namespace UiSharp.Windows.Variables;

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
    // Get() returns "" rather than null when the variable is missing, so
    // normalise that here — otherwise a null-coalescing fallback never fires.
    public string? LogDirectory =>
        InTS && Get("_SMSTSLogPath") is { Length: > 0 } dir ? dir : null;

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

    public bool Exists(string name)
    {
        // COM (live TS): SMS_TSEnvironment returns "" for both absent and empty-string variables,
        // so we can't reliably distinguish them — use the non-empty check as the best available signal.
        if (_com is not null) return !string.IsNullOrEmpty(Get(name));
        // Local fallback: ContainsKey correctly handles variables set to "".
        return _local.ContainsKey(name);
    }

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

    // Format and exclusion rules live in VariableFile so every ITSEnv agrees.
    // SaveToFile used to be an alias for DumpToFile here, which meant the
    // shipping runtime wrote key=value while LocalTSEnv wrote the documented
    // JSON -- the same interface method, two formats, and only the one that
    // never ships had tests.
    public void DumpToFile(string? path = null) =>
        VariableFile.Dump(Snapshot(), ResolvePath(path));

    public void SaveToFile(string? path = null) =>
        VariableFile.Save(Snapshot(), ResolvePath(path));

    private string ResolvePath(string? path) =>
        Substitute(path ?? VariableFile.DefaultPath);

    public void LoadFromFile(string? path = null)
    {
        foreach (var (key, value) in VariableFile.Load(ResolvePath(path)))
            Set(key, value);
    }

    /// <summary>
    /// Every variable currently set, for writing to a file.
    /// </summary>
    /// <remarks>
    /// Inside a task sequence Set() writes to the COM object and never touches
    /// the local dictionary, so enumerating that dictionary wrote an empty file
    /// exactly where the feature is meant to work. SMS_TSEnvironment exposes
    /// GetVariables() for this; the local dictionary is only the fallback for
    /// running outside a task sequence.
    /// </remarks>
    private IEnumerable<KeyValuePair<string, string>> Snapshot()
    {
        if (_com is null) return _local;

        try
        {
            var names = ((dynamic)_com).GetVariables();
            var snapshot = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in (IEnumerable<object>)names)
            {
                if (name is string key && key.Length > 0)
                    snapshot[key] = Get(key);
            }

            return snapshot;
        }
        catch
        {
            // An older or unexpected environment object: better to write what
            // this process set than to write nothing at all.
            return _local;
        }
    }
}
