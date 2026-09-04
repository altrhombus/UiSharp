namespace UIpp.Core.Scripting;

/// <summary>
/// Maps each COM member the compatibility shim supports to its UiSharp-native
/// replacement, so the engine can tell an administrator exactly what to write
/// instead.
///
/// The shim exists so that existing XML runs unchanged and without the
/// WinPE-Scripting component — it is a bridge, not the destination. The native
/// functions it points at are UiSharp-only: a config using them will not run
/// under the original C++ UI++, which is the deliberate trade for dropping the
/// dependency on a deprecated scripting engine.
/// </summary>
internal static class ScriptObjectMigration
{
    private static readonly Dictionary<string, string> Map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Scripting.FileSystemObject.FileExists"]          = "FileExists(path)",
            ["Scripting.FileSystemObject.FolderExists"]        = "FolderExists(path)",
            ["Scripting.FileSystemObject.DriveExists"]         = "DriveExists(drive)",
            ["Scripting.FileSystemObject.GetParentFolderName"] = "PathParent(path)",
            ["Scripting.FileSystemObject.GetFileName"]         = "PathFileName(path)",
            ["Scripting.FileSystemObject.GetBaseName"]         = "PathBaseName(path)",
            ["Scripting.FileSystemObject.GetExtensionName"]    = "PathExtension(path)",
            ["Scripting.FileSystemObject.GetDriveName"]        = "PathDrive(path)",
            ["Scripting.FileSystemObject.BuildPath"]           = "PathCombine(path, name)",
            ["WScript.Network.ComputerName"]                   = "ComputerName()",
            ["WScript.Network.UserName"]                       = "UserName()",
            ["WScript.Network.UserDomain"]                     = "UserDomain()",
            ["WScript.Shell.ExpandEnvironmentStrings"]         = "ExpandEnvironment(text)",
        };

    /// <summary>
    /// The native function that replaces a COM member, or null when there is no
    /// direct equivalent to recommend.
    /// </summary>
    public static string? NativeEquivalentOf(string progId, string member) =>
        Map.TryGetValue($"{progId}.{member}", out var native) ? native : null;

    /// <summary>Every native function name the shim can point at.</summary>
    public static IEnumerable<string> NativeFunctions => Map.Values.Distinct();
}
