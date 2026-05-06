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

    void DumpToFile(string? path = null);
    void SaveToFile(string? path = null);
    void LoadFromFile(string? path = null);
}
