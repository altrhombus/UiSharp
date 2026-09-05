using System.Xml.Linq;
using UiSharp.Core.Actions;
using UiSharp.Core.Configuration;
using UiSharp.Core.Dialogs;
using UiSharp.Core.Logging;
using UiSharp.Core.Scripting;
using UiSharp.Core.Variables;
using UiSharp.Windows.Variables;

namespace UiSharp.Diagnostics.Checks;

/// <summary>
/// The parts that talk to the machine: the registry, WMI, and the collected
/// X-variables that configurations branch on.
///
/// WinPE is where these diverge. It carries a reduced WMI repository, a registry
/// mounted from the boot image, and no ConfigMgr client, so a query that is
/// reliable on a desktop can return nothing here. The actions are written to
/// treat "nothing" as a non-fatal empty value, which is right for a deployment
/// and useless for finding out why a condition never matched — hence this.
/// </summary>
public sealed class PlatformChecks : ISelfCheck
{
    public string Area => "Platform";

    public IEnumerable<CheckResult> Run(SelfTestContext context)
    {
        foreach (var result in RegistryChecks(context)) yield return result;
        foreach (var result in WmiChecks(context))      yield return result;
        foreach (var result in DefaultValues(context))  yield return result;
    }

    // -------------------------------------------------------------------------

    private IEnumerable<CheckResult> RegistryChecks(SelfTestContext context)
    {
        // CurrentVersion exists in WinPE as well as a full OS, so an empty
        // result means the read path is broken rather than the key being absent.
        var env = new LocalTSEnv();

        var (failure, _) = Run(context, env,
            """
            <Actions>
              <Action Type="RegRead" Hive="HKLM"
                      Key="SOFTWARE\Microsoft\Windows NT\CurrentVersion"
                      Value="CurrentBuildNumber" Variable="Build" />
              <Action Type="RegRead" Hive="HKLM" Key="SOFTWARE\NoSuchKey\AtAll"
                      Value="Nothing" Variable="Missing" Default="fallback" />
            </Actions>
            """);

        if (failure is not null)
        {
            yield return CheckResult.Fail(Area, "The registry can be read", failure);
            yield break;
        }

        var build = env.Get("Build");

        yield return build.Length > 0
            ? CheckResult.Pass(Area, "The registry can be read", $"Windows build {build}")
            : CheckResult.Fail(Area, "The registry can be read",
                @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\CurrentBuildNumber " +
                "came back empty");

        yield return env.Get("Missing") == "fallback"
            ? CheckResult.Pass(Area, "A missing registry key falls back to Default")
            : CheckResult.Fail(Area, "A missing registry key falls back to Default",
                $"got '{env.Get("Missing")}'");
    }

    // -------------------------------------------------------------------------

    private IEnumerable<CheckResult> WmiChecks(SelfTestContext context)
    {
        var env = new LocalTSEnv();

        var (failure, elapsed) = Run(context, env,
            """
            <Actions>
              <Action Type="WMIRead" Class="Win32_OperatingSystem"
                      Property="Caption" Variable="Caption" />
              <Action Type="WMIRead" Class="Win32_ComputerSystem"
                      Property="Manufacturer" Variable="Manufacturer" />
              <Action Type="WMIRead" Query="SELECT Name FROM Win32_Processor"
                      Property="Name" Variable="Cpu" />
            </Actions>
            """);

        if (failure is not null)
        {
            yield return CheckResult.Fail(Area, "WMI can be queried", failure);
            yield break;
        }

        var caption = env.Get("Caption");

        yield return caption.Length > 0
            ? CheckResult.Pass(Area, "WMI can be queried",
                $"{caption} (in {elapsed.TotalSeconds:0.0}s)")
            : CheckResult.Fail(Area, "WMI can be queried",
                "Win32_OperatingSystem.Caption came back empty — the WMI repository " +
                "may be missing from this boot image");

        yield return env.Get("Cpu").Length > 0
            ? CheckResult.Pass(Area, "A WQL query returns a value", env.Get("Cpu"))
            : CheckResult.Fail(Area, "A WQL query returns a value",
                "SELECT Name FROM Win32_Processor came back empty");

        yield return CheckResult.Info(Area, "Manufacturer reported by WMI",
            env.Get("Manufacturer") is { Length: > 0 } m ? m : "(empty)");
    }

    // -------------------------------------------------------------------------

    private IEnumerable<CheckResult> DefaultValues(SelfTestContext context)
    {
        // The X-variables. Conditions in the field lean on these more than on
        // anything else, and an empty one reads as a legitimate "no" rather than
        // as a collection that did not happen.
        var provider = new WindowsDefaultValueProvider();
        var env      = new LocalTSEnv();

        foreach (var category in XmlConstants.DefaultValueCategories.Ordered)
        {
            if (!provider.SupportedCategories.Contains(category))
            {
                yield return CheckResult.Skip(Area, $"DefaultValues category {category}",
                    "not supported by this provider");
                continue;
            }

            var before = env.GetAll().Count;

            CheckResult result;
            try
            {
                provider.Collect(category, env, context.Log);

                var added = env.GetAll().Count - before;

                result = added > 0
                    ? CheckResult.Pass(Area, $"DefaultValues category {category}",
                        $"{added} variables")
                    : CheckResult.Fail(Area, $"DefaultValues category {category}",
                        "collected nothing — every condition on these variables will " +
                        "be false here");
            }
            catch (Exception ex)
            {
                result = CheckResult.Fail(Area, $"DefaultValues category {category}",
                    $"{ex.GetType().Name}: {ex.Message}");
            }

            yield return result;
        }

        // The handful worth printing: these are the ones a configuration almost
        // always branches on, and seeing the actual values is how you find out
        // that a machine was classified as the wrong kind of thing.
        foreach (var name in new[]
        {
            XmlConstants.Variables.IsWinPe,
            XmlConstants.Variables.OsArch,
            XmlConstants.Variables.ChassisType,
            XmlConstants.Variables.VmType,
            XmlConstants.Variables.Manufacturer,
            XmlConstants.Variables.Model,
            XmlConstants.Variables.SerialNumber,
            XmlConstants.Variables.IsUefi,
            XmlConstants.Variables.TpmEnabled,
            XmlConstants.Variables.Memory,
        })
        {
            yield return CheckResult.Info(Area, name,
                env.Exists(name) ? Display(env.Get(name)) : "(not collected)");
        }
    }

    private static string Display(string value) =>
        value.Length == 0 ? "(empty)" : value;

    // -------------------------------------------------------------------------

    /// <summary>
    /// Runs a fragment through the real pipeline, so what is measured is the
    /// action a configuration would use rather than a private copy of it.
    /// </summary>
    private static (string? Failure, TimeSpan Elapsed) Run(
        SelfTestContext context, ITSEnv env, string xml)
    {
        var started = DateTime.UtcNow;

        try
        {
            var actions   = XElement.Parse(xml);
            var evaluator = new NativeConditionEvaluator();

            new ActionProcessor(context.Factory, evaluator)
                .Run(actions, new ActionData
                {
                    ActionNode         = actions,
                    Conditions         = evaluator,
                    TsEnv              = env,
                    Log                = context.Log,
                    GlobalDialogTraits = new DialogTraits(),
                    InTS               = env.InTS,
                });

            return (null, DateTime.UtcNow - started);
        }
        catch (Exception ex)
        {
            return ($"{ex.GetType().Name}: {ex.Message}", DateTime.UtcNow - started);
        }
    }
}
