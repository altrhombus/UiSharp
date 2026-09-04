using UIpp.Core.Configuration;

namespace UIpp.Core.Tests.Parity;

/// <summary>
/// Golden-file tests over the original C++ project's own sample configurations.
///
/// Each config is resolved into a text snapshot (see <see cref="ConfigParityRecorder"/>)
/// and compared against a checked-in file. The snapshots are not asserting that
/// any particular value is *correct* — they assert that nothing changes without a
/// human looking at the diff, which is what makes a port safe to refactor.
///
/// To regenerate after an intentional change:
///     UIPP_UPDATE_GOLDEN=1 dotnet test src/UIpp.Core.Tests
/// then review the diff before committing.
/// </summary>
public class SampleConfigParityTests
{
    private static bool UpdateGolden =>
        Environment.GetEnvironmentVariable("UIPP_UPDATE_GOLDEN") == "1";

    [Theory]
    [MemberData(nameof(SampleConfigs.AllAsTheoryData), MemberType = typeof(SampleConfigs))]
    public void SampleConfig_MatchesGoldenSnapshot(string fileName)
    {
        var actual = Normalize(ConfigParityRecorder.Record(fileName));

        var goldenPath = Path.Combine(
            SampleConfigs.GoldenDirectory,
            Path.ChangeExtension(SanitizeFileName(fileName), ".txt"));

        if (UpdateGolden)
        {
            Directory.CreateDirectory(SampleConfigs.GoldenDirectory);
            File.WriteAllText(goldenPath, actual);
            return;
        }

        Assert.True(File.Exists(goldenPath),
            $"No golden snapshot at {goldenPath}. Run with UIPP_UPDATE_GOLDEN=1 to create it.");

        var expected = Normalize(File.ReadAllText(goldenPath));

        if (expected == actual) return;

        Assert.Fail(
            $"Snapshot mismatch for {fileName}.\n" +
            $"Golden: {goldenPath}\n" +
            $"First difference:\n{FirstDifference(expected, actual)}\n\n" +
            "If this change is intended, regenerate with UIPP_UPDATE_GOLDEN=1 and review the diff.");
    }

    // Every sample config must be represented by a snapshot, so adding a config
    // without a snapshot fails rather than passing silently.
    [Fact]
    public void EverySampleConfigListed_ExistsOnDisk()
    {
        var missing = SampleConfigs.All
            .Where(name => !File.Exists(SampleConfigs.PathFor(name)))
            .ToList();

        Assert.Empty(missing);
    }

    // Guards the reverse direction: a new sample config added to UI++/ should be
    // brought into the parity set deliberately, not overlooked.
    [Fact]
    public void NoUnlistedSampleConfigs()
    {
        var onDisk = Directory
            .GetFiles(SampleConfigs.Directory, "*.xml")
            .Select(Path.GetFileName)
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unlisted = onDisk.Except(SampleConfigs.All, StringComparer.OrdinalIgnoreCase).ToList();

        Assert.True(unlisted.Count == 0,
            $"These configs exist in UI++/ but are not in SampleConfigs.All: {string.Join(", ", unlisted)}. " +
            "Add them to the parity set (and generate snapshots) or exclude them deliberately.");
    }

    // The recorder must be a pure function of the config — two runs in the same
    // process have to agree, or the golden files will flap in CI.
    [Theory]
    [MemberData(nameof(SampleConfigs.AllAsTheoryData), MemberType = typeof(SampleConfigs))]
    public void Recorder_IsDeterministic(string fileName)
    {
        var first  = ConfigParityRecorder.Record(fileName);
        var second = ConfigParityRecorder.Record(fileName);

        Assert.Equal(first, second);
    }

    // The host machine must not leak into a snapshot. %ComputerName% resolves
    // through the environment fallback, so the recorder pins it deliberately.
    [Theory]
    [MemberData(nameof(SampleConfigs.AllAsTheoryData), MemberType = typeof(SampleConfigs))]
    public void Snapshot_ContainsNoMachineSpecificValues(string fileName)
    {
        var snapshot = ConfigParityRecorder.Record(fileName);

        string[] leaks =
        [
            Environment.MachineName,
            Environment.UserName,
            SampleConfigs.RepoRoot,
        ];

        foreach (var leak in leaks.Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            Assert.DoesNotContain(leak, snapshot, StringComparison.OrdinalIgnoreCase);
        }
    }

    // Every config in the parity set must actually parse. Kept separate from the
    // snapshot test so a parse regression reports as a parse failure.
    [Theory]
    [MemberData(nameof(SampleConfigs.AllAsTheoryData), MemberType = typeof(SampleConfigs))]
    public void SampleConfig_Loads(string fileName)
    {
        var config = ConfigLoader.Load(SampleConfigs.PathFor(fileName));

        Assert.NotNull(config.Document.Root);
        Assert.NotNull(config.GlobalTraits);
        Assert.False(string.IsNullOrWhiteSpace(config.ConditionEngine));
    }

    // -------------------------------------------------------------------------

    // Line endings differ between the working tree and CI checkouts; the snapshot
    // content is what matters, not how git normalised it.
    private static string Normalize(string text) =>
        text.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd() + "\n";

    private static string SanitizeFileName(string fileName) =>
        fileName.Replace(' ', '_').Replace("(", "").Replace(")", "");

    private static string FirstDifference(string expected, string actual)
    {
        var e = expected.Split('\n');
        var a = actual.Split('\n');

        for (var i = 0; i < Math.Max(e.Length, a.Length); i++)
        {
            var el = i < e.Length ? e[i] : "(end of file)";
            var al = i < a.Length ? a[i] : "(end of file)";

            if (el != al)
            {
                return $"  line {i + 1}\n" +
                       $"    golden: {el}\n" +
                       $"    actual: {al}";
            }
        }

        return "  (files differ only in trailing whitespace)";
    }
}
