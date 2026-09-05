using System.Text.Json;

namespace UiSharp.Core.Variables;

/// <summary>
/// Reading and writing the variable files behind <c>&lt;Action Type="Vars"&gt;</c>
/// and the SaveItems variable dump.
///
/// Shared by every <see cref="ITSEnv"/> so the two cannot disagree about the
/// format. They did: the task-sequence environment wrote key=value while the
/// local one wrote JSON, from the same interface method, and only the local one
/// — the one that never ships — had tests.
/// </summary>
public static class VariableFile
{
    /// <summary>The path used when a config names none, as in the original.</summary>
    public const string DefaultPath = @"%temp%\ui++vars.dat";

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <summary>
    /// Variables beginning with X or _ are left out of every file. X-prefixed
    /// names are collected facts about the machine and _ names belong to the
    /// task sequence, so neither is the operator's data to carry between runs.
    /// Matches the C++ original.
    /// </summary>
    public static bool IsExcluded(string key) =>
        key.Length > 0 && (key[0] == 'X' || key[0] == '_');

    private static Dictionary<string, string> Included(IEnumerable<KeyValuePair<string, string>> variables) =>
        variables
            .Where(kv => !IsExcluded(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Writes variables for later reloading, as JSON.
    ///
    /// JSON rather than key=value because a value may contain a newline, which
    /// a line-per-variable format silently corrupts — and the reload would then
    /// take the remainder of that value as a new variable name.
    /// </summary>
    public static void Save(IEnumerable<KeyValuePair<string, string>> variables, string path) =>
        File.WriteAllText(path, JsonSerializer.Serialize(Included(variables), Options));

    /// <summary>
    /// Reads variables previously written by <see cref="Save"/>. Returns nothing
    /// when the file is missing or unreadable: a damaged variable file should
    /// not stop a deployment, and there is no channel to report it on here.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new Dictionary<string, string>();

            var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
            return loaded is null
                ? new Dictionary<string, string>()
                : Included(loaded);
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Writes variables for a person to read — one <c>name=value</c> per line.
    /// This is the SaveItems dump, not the reload format; see <see cref="Save"/>
    /// for why the two differ.
    /// </summary>
    public static void Dump(IEnumerable<KeyValuePair<string, string>> variables, string path) =>
        File.WriteAllLines(path, Included(variables).Select(kv => $"{kv.Key}={kv.Value}"));
}
