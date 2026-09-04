namespace UiSharp.Core.Scripting;

/// <summary>
/// A native stand-in for a COM object a config obtained via <c>CreateObject</c>.
///
/// Member lookup is case-insensitive because VBScript's is. Returning false from
/// <see cref="TryInvoke"/> means "this object has no such member", which the
/// caller turns into a diagnostic rather than a wrong answer.
/// </summary>
internal abstract class ScriptObject(string progId)
{
    public string ProgId { get; } = progId;

    public abstract bool TryInvoke(string member, List<string> args, out string result);

    /// <summary>
    /// Maps a ProgID to a native equivalent. Unknown ProgIDs return null so the
    /// engine reports that a script host is required, leaving such configs
    /// working under ConditionEngine="vbscript".
    /// </summary>
    public static ScriptObject? Create(string progId, IScriptHostServices services) =>
        progId.Trim() switch
        {
            var p when Eq(p, "Scripting.FileSystemObject") => new FileSystemObject(services),
            var p when Eq(p, "WScript.Network")            => new NetworkObject(services),
            var p when Eq(p, "WScript.Shell")              => new ShellObject(services),
            _ => null,
        };

    protected static bool Eq(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    protected static string Arg(List<string> args, int index) =>
        index < args.Count ? args[index] : string.Empty;

    // VBScript renders booleans as True/False.
    protected static string Bool(bool value) => value ? "True" : "False";
}

// -----------------------------------------------------------------------------

/// <summary>
/// Scripting.FileSystemObject. The path members are pure string manipulation in
/// VBScript, using Windows separators regardless of the host, so they are
/// implemented by hand rather than through System.IO.Path — whose behaviour
/// varies by platform and would not match the original on the same input.
/// </summary>
internal sealed class FileSystemObject(IScriptHostServices services)
    : ScriptObject("Scripting.FileSystemObject")
{
    public override bool TryInvoke(string member, List<string> args, out string result)
    {
        result = string.Empty;

        if (Eq(member, "FileExists"))   { result = Bool(services.FileExists(Arg(args, 0)));   return true; }
        if (Eq(member, "FolderExists")) { result = Bool(services.FolderExists(Arg(args, 0))); return true; }
        if (Eq(member, "DriveExists"))  { result = Bool(services.DriveExists(Arg(args, 0)));  return true; }

        if (Eq(member, "GetParentFolderName")) { result = GetParentFolderName(Arg(args, 0)); return true; }
        if (Eq(member, "GetFileName"))         { result = GetFileName(Arg(args, 0));         return true; }
        if (Eq(member, "GetBaseName"))         { result = GetBaseName(Arg(args, 0));         return true; }
        if (Eq(member, "GetExtensionName"))    { result = GetExtensionName(Arg(args, 0));     return true; }
        if (Eq(member, "GetDriveName"))        { result = GetDriveName(Arg(args, 0));         return true; }
        if (Eq(member, "BuildPath"))           { result = BuildPath(Arg(args, 0), Arg(args, 1)); return true; }

        return false;
    }

    private static bool IsSeparator(char c) => c is '\\' or '/';

    private static int LastSeparator(string path)
    {
        for (var i = path.Length - 1; i >= 0; i--)
            if (IsSeparator(path[i])) return i;
        return -1;
    }

    // "C:\a\b\c.txt" -> "C:\a\b"; "C:\a" -> "C:\"; "C:\" -> ""; "c.txt" -> "".
    internal static string GetParentFolderName(string path)
    {
        if (path.Length == 0) return string.Empty;

        // A trailing separator is ignored, as FSO does.
        var end = path.Length;
        while (end > 0 && IsSeparator(path[end - 1])) end--;
        if (end == 0) return string.Empty;

        var trimmed = path[..end];
        var sep = LastSeparator(trimmed);
        if (sep < 0) return string.Empty;

        // Keep the separator when the parent is a drive root.
        if (sep == 0) return trimmed[..1];
        if (sep == 2 && trimmed[1] == ':') return trimmed[..3];

        return trimmed[..sep];
    }

    // "C:\a\b\c.txt" -> "c.txt"; "C:\a\" -> "a"; "C:\" -> "".
    internal static string GetFileName(string path)
    {
        if (path.Length == 0) return string.Empty;

        var end = path.Length;
        while (end > 0 && IsSeparator(path[end - 1])) end--;
        if (end == 0) return string.Empty;

        var trimmed = path[..end];

        // A bare drive has no file name.
        if (trimmed.Length == 2 && trimmed[1] == ':') return string.Empty;

        var sep = LastSeparator(trimmed);
        return sep < 0 ? trimmed : trimmed[(sep + 1)..];
    }

    internal static string GetBaseName(string path)
    {
        var name = GetFileName(path);
        var dot  = name.LastIndexOf('.');

        // A leading dot means the whole name is the extension, so the base name
        // is empty — GetBaseName(".hidden") is "" in FSO, not ".hidden".
        // Confirmed against the real engine by the differential tests.
        return dot < 0 ? name : name[..dot];
    }

    internal static string GetExtensionName(string path)
    {
        var name = GetFileName(path);
        var dot  = name.LastIndexOf('.');
        return dot < 0 || dot == name.Length - 1 ? string.Empty : name[(dot + 1)..];
    }

    // "C:\a\b" -> "C:"; "\\server\share\x" -> "\\server\share"; relative -> "".
    internal static string GetDriveName(string path)
    {
        if (path.Length >= 2 && path[1] == ':') return path[..2];

        if (path.Length > 2 && IsSeparator(path[0]) && IsSeparator(path[1]))
        {
            var server = path.IndexOfAny(['\\', '/'], 2);
            if (server < 0) return path;
            var share = path.IndexOfAny(['\\', '/'], server + 1);
            return share < 0 ? path : path[..share];
        }

        return string.Empty;
    }

    internal static string BuildPath(string path, string name)
    {
        if (path.Length == 0) return name;
        if (name.Length == 0) return path;

        return IsSeparator(path[^1])
            ? path + name
            : path + "\\" + name;
    }
}

// -----------------------------------------------------------------------------

internal sealed class NetworkObject(IScriptHostServices services)
    : ScriptObject("WScript.Network")
{
    public override bool TryInvoke(string member, List<string> args, out string result)
    {
        result = string.Empty;

        if (Eq(member, "ComputerName")) { result = services.ComputerName; return true; }
        if (Eq(member, "UserName"))     { result = services.UserName;     return true; }
        if (Eq(member, "UserDomain"))   { result = services.UserDomain;   return true; }

        return false;
    }
}

// -----------------------------------------------------------------------------

internal sealed class ShellObject(IScriptHostServices services)
    : ScriptObject("WScript.Shell")
{
    public override bool TryInvoke(string member, List<string> args, out string result)
    {
        result = string.Empty;

        if (Eq(member, "ExpandEnvironmentStrings"))
        {
            result = services.ExpandEnvironmentStrings(Arg(args, 0));
            return true;
        }

        // RegRead and Run are deliberately absent: reading the registry is
        // Windows-only and UI++ already has a RegRead action, while Run has side
        // effects that have no place in evaluating a condition.
        return false;
    }
}
