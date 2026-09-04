namespace UIpp.Core.Variables;

public interface ITSEnv
{
    bool InTS { get; }
    /// <summary>
    /// The DIRECTORY logs belong in — <c>_SMSTSLogPath</c> inside a task
    /// sequence — or null when there is none. Named for what it is: reading it
    /// as a file path made the runtime throw at startup in every real task
    /// sequence. Use <see cref="Logging.LogFile.ResolvePath"/> to get a file.
    /// </summary>
    string? LogDirectory { get; }

    string Get(string name);
    bool TryGet(string name, out string value);
    bool Exists(string name);
    void Set(string name, string value);
    void Set(string name, ulong value);

    string Substitute(string input);

    /// <summary>Writes non-system variables as plain key=value pairs (for ActionSaveItems).
    /// Matches C++ CTSEnv::DumpToFile().</summary>
    void DumpToFile(string? path = null);

    /// <summary>Writes variables in JSON format for later reload (for ActionVars Save/Load).
    /// Replaces the MFC CArchive binary format used by the original C++ SaveToFile().</summary>
    void SaveToFile(string? path = null);

    void LoadFromFile(string? path = null);
}
