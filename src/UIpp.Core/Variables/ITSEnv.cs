namespace UIpp.Core.Variables;

public interface ITSEnv
{
    bool InTS { get; }
    string? LogPath { get; }

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
