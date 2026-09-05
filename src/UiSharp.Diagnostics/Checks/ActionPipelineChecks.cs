using System.Xml.Linq;
using UiSharp.Core.Actions;
using UiSharp.Core.Configuration;
using UiSharp.Core.Dialogs;
using UiSharp.Core.Scripting;
using UiSharp.Core.Variables;

namespace UiSharp.Diagnostics.Checks;

/// <summary>
/// The action pipeline, driven the way a configuration file drives it.
///
/// Action types are discovered by reflecting over assembly attributes, and the
/// runtime ships trimmed and single-file — a combination that can drop a type
/// with no error at all. A missing action type is logged as "unknown action" and
/// skipped, which during a deployment looks like a configuration that simply did
/// not do very much.
///
/// Interactive actions are not run: they need a person in front of the screen.
/// See the companion self-test configuration for those.
/// </summary>
public sealed class ActionPipelineChecks : ISelfCheck
{
    public string Area => "Action pipeline";

    /// <summary>
    /// Every action type a configuration may name. Dialog types are included:
    /// they live in a third assembly, and losing that one to trimming is exactly
    /// the failure this is here to catch.
    /// </summary>
    private static readonly string[] Expected =
    [
        XmlConstants.ActionTypes.TSVar, XmlConstants.ActionTypes.TSVarList,
        XmlConstants.ActionTypes.Switch, XmlConstants.ActionTypes.RandomString,
        XmlConstants.ActionTypes.FileRead, XmlConstants.ActionTypes.Vars,
        XmlConstants.ActionTypes.ToJson, XmlConstants.ActionTypes.FromJson,
        XmlConstants.ActionTypes.SaveItems, XmlConstants.ActionTypes.ExternalCall,
        XmlConstants.ActionTypes.Rest, XmlConstants.ActionTypes.DefaultValues,
        XmlConstants.ActionTypes.RegRead, XmlConstants.ActionTypes.RegWrite,
        XmlConstants.ActionTypes.WmiRead, XmlConstants.ActionTypes.WmiWrite,
        XmlConstants.ActionTypes.Tpm, XmlConstants.ActionTypes.SoftwareDisc,
        XmlConstants.ActionTypes.UserInput, XmlConstants.ActionTypes.UserInfo,
        XmlConstants.ActionTypes.UserInfoFull, XmlConstants.ActionTypes.ErrorInfo,
        XmlConstants.ActionTypes.AppTree, XmlConstants.ActionTypes.UserAuth,
        XmlConstants.ActionTypes.Preflight,
    ];

    public IEnumerable<CheckResult> Run(SelfTestContext context)
    {
        const string discoverable = "Every action type is discoverable";

        var absent = Expected.Where(t => !context.Factory.IsRegistered(t)).ToList();

        yield return absent.Count == 0
            ? CheckResult.Pass(Area, discoverable, $"{Expected.Length} types")
            : CheckResult.Fail(Area, discoverable,
                "missing: " + string.Join(", ", absent) +
                " — reflection over assembly types has probably been trimmed away");

        // ---- run a configuration and inspect what it produced
        var env      = new LocalTSEnv();
        var savePath = Path.Combine(context.ScratchDirectory, "pipeline.dat");

        var xml = $"""
            <Actions>
              <Action Type="TSVar" Variable="Suffix">CTG</Action>
              <Action Type="TSVar" Variable="Quoted">"%Suffix%"</Action>
              <Action Type="TSVar" Variable="Sum">1 + 1</Action>
              <Action Type="TSVar" Variable="Literal" DontEval="True">"%Suffix%"</Action>
              <Action Type="TSVar" Variable="Skipped" Condition="1 = 2">no</Action>
              <Action Type="TSVar" Variable="Taken" Condition="1 = 1">yes</Action>
              <Action Type="Switch" OnValue="%Suffix%">
                <Case RegEx="^CTG$">
                  <Variable Name="Switched">matched</Variable>
                </Case>
                <Default>
                  <Variable Name="Switched">fell through</Variable>
                </Default>
              </Action>
              <ActionGroup Name="Group">
                <Action Type="TSVar" Variable="InGroup">nested</Action>
              </ActionGroup>
              <ActionGroup Name="SkippedGroup" Condition="1 = 2">
                <Action Type="TSVar" Variable="InSkippedGroup">should not run</Action>
              </ActionGroup>
              <Action Type="RandomString" Variable="Random" Length="8" />
              <Action Type="Vars" Direction="Save" Filename="{savePath}" />
            </Actions>
            """;

        var (outcome, failure) = RunActions(xml, env, context);

        if (failure is not null)
        {
            yield return CheckResult.Fail(Area, "A configuration runs to completion", failure);
            yield break;
        }

        yield return outcome == ActionResult.Next
            ? CheckResult.Pass(Area, "A configuration runs to completion")
            : CheckResult.Fail(Area, "A configuration runs to completion",
                $"the processor returned {outcome}");

        foreach (var (variable, expected, what) in new[]
        {
            ("Suffix",   "CTG",     "plain text survives evaluation"),
            ("Quoted",   "CTG",     "a quoted value is evaluated and loses its quotes"),
            ("Sum",      "2",       "an expression is evaluated"),
            ("Literal",  "\"CTG\"", "DontEval leaves a value exactly as written"),
            ("Taken",    "yes",     "a true condition runs its action"),
            ("Switched", "matched", "Switch picks the matching case"),
            ("InGroup",  "nested",  "an action inside a group runs"),
        })
        {
            var actual = env.Get(variable);

            yield return actual == expected
                ? CheckResult.Pass(Area, $"TSVar {variable}: {what}")
                : CheckResult.Fail(Area, $"TSVar {variable}: {what}",
                    $"expected '{expected}', got '{actual}'");
        }

        foreach (var (variable, what) in new[]
        {
            ("Skipped",        "A false condition skips its action"),
            ("InSkippedGroup", "A false condition skips a whole group"),
        })
        {
            yield return string.IsNullOrEmpty(env.Get(variable))
                ? CheckResult.Pass(Area, what)
                : CheckResult.Fail(Area, what, $"{variable} = '{env.Get(variable)}'");
        }

        var random = env.Get("Random");

        yield return random.Length == 8
            ? CheckResult.Pass(Area, "RandomString honours its length")
            : CheckResult.Fail(Area, "RandomString honours its length",
                $"got '{random}' ({random.Length} characters)");

        yield return File.Exists(savePath)
            ? CheckResult.Pass(Area, "The Vars action writes a file",
                $"{new FileInfo(savePath).Length} bytes")
            : CheckResult.Fail(Area, "The Vars action writes a file",
                $"{savePath} was not created");

        // An unknown action type is skipped with a warning rather than being
        // fatal. That is also what a trimmed-away type looks like, so this
        // confirms the actions around it still run.
        yield return UnknownTypeCheck(context);
    }

    private CheckResult UnknownTypeCheck(SelfTestContext context)
    {
        const string name = "An unknown action type is skipped, not fatal";

        var env = new LocalTSEnv();

        var (outcome, failure) = RunActions(
            """
            <Actions>
              <Action Type="NoSuchActionType" Variable="Ignored">x</Action>
              <Action Type="TSVar" Variable="After">ran</Action>
            </Actions>
            """, env, context);

        if (failure is not null)
            return CheckResult.Fail(Area, name, failure);

        return outcome == ActionResult.Next && env.Get("After") == "ran"
            ? CheckResult.Pass(Area, name)
            : CheckResult.Fail(Area, name,
                $"the processor returned {outcome}, After='{env.Get("After")}'");
    }

    private static (ActionResult Outcome, string? Failure) RunActions(
        string xml, ITSEnv env, SelfTestContext context)
    {
        try
        {
            var actions   = XElement.Parse(xml);
            var evaluator = new NativeConditionEvaluator();

            var result = new ActionProcessor(context.Factory, evaluator)
                .Run(actions, new ActionData
                {
                    ActionNode         = actions,
                    Conditions         = evaluator,
                    TsEnv              = env,
                    Log                = context.Log,
                    GlobalDialogTraits = new DialogTraits(),
                    InTS               = env.InTS,
                });

            return (result, null);
        }
        catch (Exception ex)
        {
            return (ActionResult.Next, $"{ex.GetType().Name}: {ex.Message}");
        }
    }
}
