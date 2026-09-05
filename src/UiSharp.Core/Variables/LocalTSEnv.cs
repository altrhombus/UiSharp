using System.Text.Json;
using System.Text.RegularExpressions;

namespace UiSharp.Core.Variables;

// Non-TS dictionary-backed ITSEnv — used outside a real task sequence (dev/test/debug).
// Variable substitution supports %VAR% patterns resolved against stored variables,
// then falls back to environment variables for anything unmatched.
public sealed class LocalTSEnv : ITSEnv
{
    private readonly Dictionary<string, string> _vars =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Regex SubstPattern =
        new(@"%([^%]+)%", RegexOptions.Compiled);

    private readonly Func<string, string?> _environmentLookup;

    public LocalTSEnv() : this(Environment.GetEnvironmentVariable) { }

    /// <summary>
    /// Overrides the process-environment fallback used by <see cref="Substitute"/>.
    /// Exists so callers that need reproducible substitution — golden-file tests in
    /// particular — are not at the mercy of whatever the host machine happens to
    /// have set.
    /// </summary>
    public LocalTSEnv(Func<string, string?> environmentLookup) =>
        _environmentLookup = environmentLookup;

    public bool InTS => false;
    public string? LogDirectory => null;

    public IReadOnlyDictionary<string, string> GetAll() => _vars;

    public string Get(string name) =>
        _vars.TryGetValue(name, out var v) ? v : string.Empty;

    public bool TryGet(string name, out string value) =>
        _vars.TryGetValue(name, out value!);

    public bool Exists(string name) => _vars.ContainsKey(name);

    public void Set(string name, string value) => _vars[name] = value;
    public void Set(string name, ulong value)  => _vars[name] = value.ToString();

    public string Substitute(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.Contains('%'))
            return input;

        // Multi-pass: handles %var1%%var2% where var1/var2 themselves contain references.
        // Cap at 10 to prevent runaway on circular references.
        string current = input;
        for (int pass = 0; pass < 10; pass++)
        {
            var next = SubstPattern.Replace(current, m =>
            {
                var key = m.Groups[1].Value;
                if (_vars.TryGetValue(key, out var val)) return val;
                var env = _environmentLookup(key);
                return env ?? m.Value;
            });
            if (next == current) break;
            current = next;
            if (!current.Contains('%')) break;
        }
        return current;
    }

    // Format and exclusion rules live in VariableFile so every ITSEnv agrees.
    public void DumpToFile(string? path = null) =>
        VariableFile.Dump(_vars, ResolvePath(path));

    public void SaveToFile(string? path = null) =>
        VariableFile.Save(_vars, ResolvePath(path));

    public void LoadFromFile(string? path = null)
    {
        foreach (var (key, value) in VariableFile.Load(ResolvePath(path)))
            _vars[key] = value;
    }

    private string ResolvePath(string? path) =>
        Substitute(path ?? VariableFile.DefaultPath);
}
