using System.Text.RegularExpressions;
using Microsoft.Win32;
using UiSharp.Core.Actions;
using UiSharp.Core.Configuration;
using UiSharp.Core.Logging;

namespace UiSharp.Windows.Actions;

// Scans the Add/Remove Programs (Uninstall) registry keys, matches entries against
// <Match> child elements, and sets a variable to True/False for each.
[ActionType(XmlConstants.ActionTypes.SoftwareDisc)]
public sealed class ActionSoftwareDiscovery(ActionData data) : ActionBase(data)
{
    private record SoftwareEntry(string Name, string Version);

    public override ActionResult Go()
    {
        var includeSystemComponents = BoolAttr(XmlConstants.Attributes.SystemComponents);

        // Collect all installed software from both 32-bit and 64-bit hives.
        var installed = new HashSet<SoftwareEntry>(SoftwareEntryComparer.Instance);
        CollectFromHive(RegistryView.Registry64, installed, includeSystemComponents);
        CollectFromHive(RegistryView.Registry32, installed, includeSystemComponents);

        Data.Log.Write($"SoftwareDiscovery: found {installed.Count} installed entries.");

        // Process each <Match> child element.
        foreach (var matchEl in Data.ActionNode.Elements("Match"))
        {
            // C++ reads these through GetXMLAttribute (Actions.cpp:451-454), so
            // they are variable-substituted.
            var namePattern    = Attr(matchEl, XmlConstants.Attributes.DisplayName);
            var variable       = Attr(matchEl, XmlConstants.Attributes.Variable);
            var versionStr     = Attr(matchEl, XmlConstants.Attributes.Version);
            var versionOp      = Attr(matchEl, XmlConstants.Attributes.VersionOperator, "eq");
            var regexOpts      = RegexOptions.IgnoreCase | RegexOptions.Compiled;

            if (string.IsNullOrWhiteSpace(namePattern) || string.IsNullOrWhiteSpace(variable))
                continue;

            bool matched = false;
            try
            {
                // C++ uses std::regex_match() which requires the full string to match.
                // Wrap in ^(?:...)$ to replicate full-string semantics in .NET.
                var rx = new Regex($"^(?:{namePattern})$", regexOpts);
                foreach (var entry in installed)
                {
                    if (!rx.IsMatch(entry.Name)) continue;
                    if (!string.IsNullOrWhiteSpace(versionStr) &&
                        !VersionMatches(entry.Version, versionOp, versionStr))
                        continue;
                    matched = true;
                    break;
                }
            }
            catch (RegexParseException ex)
            {
                Data.Log.Write($"SoftwareDiscovery: invalid regex '{namePattern}': {ex.Message}", LogSeverity.Warning);
            }

            Data.TsEnv.Set(variable, matched ? XmlConstants.Values.True : XmlConstants.Values.False);
            Data.Log.Write($"SoftwareDiscovery: {variable}={matched} (pattern='{namePattern}')");
        }

        return ActionResult.Next;
    }

    private static void CollectFromHive(
        RegistryView view,
        HashSet<SoftwareEntry> results,
        bool includeSystemComponents)
    {
        const string uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        try
        {
            using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using var key  = hklm.OpenSubKey(uninstallKey, writable: false);
            if (key is null) return;

            foreach (var subName in key.GetSubKeyNames())
            {
                try
                {
                    using var sub = key.OpenSubKey(subName, writable: false);
                    if (sub is null) continue;

                    if (!includeSystemComponents &&
                        sub.GetValue("SystemComponent") is int sc && sc == 1)
                        continue;

                    var displayName    = sub.GetValue("DisplayName")    as string ?? string.Empty;
                    var displayVersion = sub.GetValue("DisplayVersion") as string ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(displayName))
                        results.Add(new SoftwareEntry(displayName.Trim(), displayVersion.Trim()));
                }
                catch { /* skip malformed entries */ }
            }
        }
        catch { /* hive not accessible */ }
    }

    internal static bool VersionMatches(string installed, string op, string target)
    {
        if (!Version.TryParse(installed, out var vi) || !Version.TryParse(target, out var vt))
        {
            // Fallback to string comparison.
            var cmp = string.Compare(installed, target, StringComparison.OrdinalIgnoreCase);
            return op.ToLowerInvariant() switch
            {
                "eq"  or "="  => cmp == 0,
                "ne"  or "!=" => cmp != 0,
                "lt"  or "<"  => cmp < 0,
                "lte" or "<=" => cmp <= 0,
                "gt"  or ">"  => cmp > 0,
                "gte" or ">=" => cmp >= 0,
                _             => false,
            };
        }

        var diff = vi.CompareTo(vt);
        return op.ToLowerInvariant() switch
        {
            "eq"  or "="  => diff == 0,
            "ne"  or "!=" => diff != 0,
            "lt"  or "<"  => diff < 0,
            "lte" or "<=" => diff <= 0,
            "gt"  or ">"  => diff > 0,
            "gte" or ">=" => diff >= 0,
            _             => false,
        };
    }

    private sealed class SoftwareEntryComparer : IEqualityComparer<SoftwareEntry>
    {
        public static readonly SoftwareEntryComparer Instance = new();
        public bool Equals(SoftwareEntry? x, SoftwareEntry? y) =>
            StringComparer.OrdinalIgnoreCase.Equals(x?.Name, y?.Name) &&
            StringComparer.OrdinalIgnoreCase.Equals(x?.Version, y?.Version);
        public int GetHashCode(SoftwareEntry obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Version));
    }
}
