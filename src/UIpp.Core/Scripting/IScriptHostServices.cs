namespace UIpp.Core.Scripting;

/// <summary>
/// The outside-world lookups the native engine needs in order to stand in for
/// the COM objects UI++ configs create through <c>CreateObject</c>.
///
/// Injected rather than called directly so that golden-file tests can supply a
/// fixed filesystem and machine identity, and so a future Windows-only
/// implementation can add registry and WMI access without dragging those into
/// <c>UIpp.Core</c>.
/// </summary>
public interface IScriptHostServices
{
    // Scripting.FileSystemObject
    bool FileExists(string path);
    bool FolderExists(string path);
    bool DriveExists(string drive);

    // WScript.Network
    string ComputerName { get; }
    string UserName     { get; }
    string UserDomain   { get; }

    // WScript.Shell
    string ExpandEnvironmentStrings(string input);
}

/// <summary>
/// Talks to the real machine. Path parsing deliberately lives in
/// <c>ScriptObjects</c> rather than here, because VBScript's path functions are
/// pure string manipulation with Windows conventions and must not vary by host.
/// </summary>
public sealed class DefaultScriptHostServices : IScriptHostServices
{
    public static readonly DefaultScriptHostServices Instance = new();

    public bool FileExists(string path)
    {
        try   { return !string.IsNullOrWhiteSpace(path) && File.Exists(path); }
        catch { return false; }   // malformed paths raise rather than returning false
    }

    public bool FolderExists(string path)
    {
        try   { return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path); }
        catch { return false; }
    }

    public bool DriveExists(string drive)
    {
        if (string.IsNullOrWhiteSpace(drive)) return false;

        try
        {
            // FSO accepts "C", "C:" and "C:\" alike.
            var root = drive.TrimEnd('\\', '/');
            if (root.Length == 1) root += ":";
            return DriveInfo.GetDrives().Any(d =>
                d.Name.TrimEnd('\\', '/').Equals(root, StringComparison.OrdinalIgnoreCase));
        }
        catch { return false; }
    }

    public string ComputerName => Environment.MachineName;
    public string UserName     => Environment.UserName;
    public string UserDomain   => Environment.UserDomainName;

    public string ExpandEnvironmentStrings(string input)
    {
        try   { return Environment.ExpandEnvironmentVariables(input ?? string.Empty); }
        catch { return input ?? string.Empty; }
    }
}
