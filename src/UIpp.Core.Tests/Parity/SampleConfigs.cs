namespace UIpp.Core.Tests.Parity;

/// <summary>
/// Locates the original C++ project's own sample configuration files, which live
/// in the repository at <c>UI++/</c>. These are the closest thing to real-world
/// configs available — written by the original author, not by this port's tests.
/// </summary>
internal static class SampleConfigs
{
    // Walks up from the test assembly until the repository root is recognisable.
    private static readonly Lazy<string> _repoRoot = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "UiSharp.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find UiSharp.slnx above {AppContext.BaseDirectory}.");
    });

    public static string RepoRoot => _repoRoot.Value;

    /// <summary>Directory holding the original project's sample XML configs.</summary>
    public static string Directory => Path.Combine(RepoRoot, "UI++");

    /// <summary>Directory holding the checked-in golden snapshots.</summary>
    public static string GoldenDirectory =>
        Path.Combine(RepoRoot, "src", "UIpp.Core.Tests", "Parity", "Golden");

    /// <summary>
    /// Every sample config, as xUnit theory data. Named rather than globbed so a
    /// file appearing or disappearing in <c>UI++/</c> is a deliberate test change.
    /// </summary>
    public static readonly string[] All =
    [
        "UI++.xml",
        "UI++.1.xml",
        "UI++.A.xml",
        "UI++2.xml",
        "UI++3.xml",
        "UI++5.xml",
        "UI++6.xml",
        "UI++ (Logical Disks Snippet).xml",
    ];

    public static IEnumerable<object[]> AllAsTheoryData() =>
        All.Select(name => new object[] { name });

    public static string PathFor(string fileName) => Path.Combine(Directory, fileName);
}
