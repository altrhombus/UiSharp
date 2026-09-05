using System.Xml.Linq;
using UiSharp.Core.Configuration;
using UiSharp.Core.Scripting;
using UiSharp.Core.Variables;

namespace UiSharp.Diagnostics.Tests;

/// <summary>
/// The interactive companion at <c>tools/selftest/UiSharp-SelfTest.xml</c>.
///
/// That file is driven by a person, so nothing here can judge whether the
/// dialogs looked right. What it can do is make sure the operator is not sent to
/// a machine in a lab only to find the configuration does not load, names an
/// action type that no longer exists, or references a variable nothing sets —
/// the failures that waste a trip.
/// </summary>
public class InteractiveConfigTests
{
    private static string ConfigPath
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);

            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, "tools", "selftest", "UiSharp-SelfTest.xml");
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            throw new FileNotFoundException(
                $"Could not find tools/selftest/UiSharp-SelfTest.xml above {AppContext.BaseDirectory}.");
        }
    }

    private static readonly Lazy<LoadedConfig> Config = new(() => ConfigLoader.Load(ConfigPath));

    [Fact]
    public void It_loads()
    {
        var config = Config.Value;

        Assert.NotNull(config.Document.Root);
        Assert.Equal(XmlConstants.Elements.Root, config.Document.Root!.Name.LocalName);
        Assert.NotNull(config.Document.Root.Element(XmlConstants.Elements.Actions));
    }

    [Fact]
    public void Every_action_type_it_names_exists()
    {
        var types = Config.Value.Document
            .Descendants(XmlConstants.Elements.Action)
            .Select(a => (string?)a.Attribute(XmlConstants.Attributes.Type) ?? "")
            .Distinct()
            .ToList();

        Assert.NotEmpty(types);

        var unknown = types.Where(t => !CheckHarness.Factory.IsRegistered(t)).ToList();

        Assert.True(unknown.Count == 0,
            "The interactive self-test names action types that do not exist: " +
            string.Join(", ", unknown));
    }

    [Fact]
    public void It_exercises_every_dialog_the_runtime_can_show()
    {
        // The point of the file. If a dialog type is added and not covered here,
        // nobody will ever look at it on real hardware.
        var types = Config.Value.Document
            .Descendants(XmlConstants.Elements.Action)
            .Select(a => (string?)a.Attribute(XmlConstants.Attributes.Type) ?? "")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var dialog in new[]
        {
            XmlConstants.ActionTypes.UserInput,
            XmlConstants.ActionTypes.UserInfo,
            XmlConstants.ActionTypes.UserInfoFull,
            XmlConstants.ActionTypes.ErrorInfo,
            XmlConstants.ActionTypes.AppTree,
            XmlConstants.ActionTypes.Preflight,
        })
        {
            Assert.True(types.Contains(dialog),
                $"The interactive self-test never shows the {dialog} dialog.");
        }
    }

    [Fact]
    public void Every_input_field_it_declares_writes_to_a_variable()
    {
        // A field with no Variable collects an answer and throws it away, which
        // looks like the dialog working right up until the summary is empty.
        var fields = Config.Value.Document
            .Descendants()
            .Where(e => e.Name.LocalName.StartsWith("Input", StringComparison.OrdinalIgnoreCase)
                        && !e.Name.LocalName.Equals(XmlConstants.InputTypes.Info,
                                StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.NotEmpty(fields);

        Assert.All(fields, f => Assert.False(
            string.IsNullOrWhiteSpace((string?)f.Attribute(XmlConstants.Attributes.Variable)),
            $"<{f.Name.LocalName}> has no Variable, so its answer goes nowhere."));
    }

    [Fact]
    public void Every_software_reference_resolves()
    {
        var declared = Config.Value.Software.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var referenced = Config.Value.Document
            .Descendants(XmlConstants.Elements.SoftwareRef)
            .Select(r => (string?)r.Attribute(XmlConstants.Attributes.Id) ?? "")
            .ToList();

        Assert.NotEmpty(referenced);

        var dangling = referenced.Where(id => !declared.Contains(id)).ToList();

        Assert.True(dangling.Count == 0,
            "The software tree references items that are not declared in <Software>: " +
            string.Join(", ", dangling));
    }

    [Fact]
    public void Every_condition_it_uses_evaluates_without_a_diagnostic()
    {
        // The conditions are meant to demonstrate passing, warning and failing
        // checks. One that cannot be evaluated fails closed, which would look
        // like a failing check rather than like a broken configuration.
        var engine = new NativeConditionEvaluator();
        var env    = new LocalTSEnv();

        // Values the config expects the DefaultValues action to have collected
        // by the time these are evaluated.
        env.Set(XmlConstants.Variables.OsArch, "x64");
        env.Set(XmlConstants.Variables.Memory, "8192");

        var problems = new List<string>();

        foreach (var (element, attribute) in Conditions(Config.Value.Document))
        {
            var raw = (string?)element.Attribute(attribute);
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var result = engine.TryEvaluate(env.Substitute(raw), new Dictionary<string, string>());

            if (!result.IsReliable)
                problems.Add($"<{element.Name.LocalName} {attribute}=\"{raw}\">: {result.DescribeProblems()}");
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    private static IEnumerable<(XElement Element, string Attribute)> Conditions(XDocument doc)
    {
        foreach (var element in doc.Descendants())
        {
            foreach (var attribute in new[]
            {
                XmlConstants.Attributes.Condition,
                XmlConstants.Attributes.CheckCondition,
                XmlConstants.Attributes.WarnCondition,
            })
            {
                if (element.Attribute(attribute) is not null)
                    yield return (element, attribute);
            }
        }
    }
}
