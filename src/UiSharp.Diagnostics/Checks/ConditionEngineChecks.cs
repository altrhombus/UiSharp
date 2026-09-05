using UiSharp.Core.Scripting;

namespace UiSharp.Diagnostics.Checks;

/// <summary>
/// The condition engine, on this machine.
///
/// The differential tests prove the native engine agrees with vbscript.dll on a
/// development box. This proves the engine that actually shipped — trimmed,
/// single-file, in a boot image — still evaluates the same way. Trimming can
/// remove what reflection needs, and a condition that quietly changes meaning
/// is how a deployment images the wrong thing.
/// </summary>
public sealed class ConditionEngineChecks : ISelfCheck
{
    public string Area => "Condition engine";

    private static readonly (string Expression, bool Expected)[] Conditions =
    [
        // Comparison and boolean basics
        ("1 = 1", true),
        ("1 = 2", false),
        ("2048 >= 1024", true),
        ("\"LAPTOP\" = \"LAPTOP\"", true),
        // Binary comparison, as VBScript does it
        ("\"laptop\" = \"LAPTOP\"", false),
        ("\"A\" = \"A\" AND \"1\" = \"1\"", true),
        ("True AND False", false),
        ("NOT False", true),

        // Arithmetic and precedence
        ("1 + 1 = 2", true),
        ("2 + 3 * 4 = 14", true),
        ("10 Mod 3 = 1", true),

        // Strings
        ("InStr(\"SRV-001\", \"SRV\") > 0", true),
        ("UCase(\"abc\") = \"ABC\"", true),
        ("Len(Trim(\"  ab  \")) = 2", true),
        ("StrComp(\"a\", \"b\") = -1", true),

        // Arrays
        ("UBound(Split(\"a,b,c\", \",\")) = 2", true),
        ("Split(\"a,b,c\", \",\")(1) = \"b\"", true),
        ("Join(Split(\"a,b\", \",\"), \"-\") = \"a-b\"", true),

        // Dates
        ("DateDiff(\"d\", \"1/1/2020\", \"1/31/2020\") = 30", true),
        ("Year(DateAdd(\"yyyy\", 1, \"1/2/2020\")) = 2021", true),

        // UiSharp extensions
        ("EqualsIgnoreCase(\"LENOVO\", \"Lenovo\")", true),
        ("VersionCompare(\"10.0.19041\", \"10.0.9600\") > 0", true),
        ("InList(\"Fire,IST,HR\", \"ist\")", true),
        ("IsSet(\"8192\")", true),
        ("IsSet(\"%NeverSetAnywhere%\")", false),

        // Fail closed: an unresolved variable must not read as true
        ("%NeverSetAnywhere% >= 1024", false),
        ("SomeFunctionThatDoesNotExist(\"x\")", false),
    ];

    public IEnumerable<CheckResult> Run(SelfTestContext context)
    {
        var engine = new NativeConditionEvaluator();
        var vars = new Dictionary<string, string>();

        var wrong = new List<string>();

        foreach (var (expression, expected) in Conditions)
        {
            try
            {
                if (engine.Evaluate(expression, vars) != expected)
                    wrong.Add($"{expression} → {!expected}, expected {expected}");
            }
            catch (Exception ex)
            {
                wrong.Add($"{expression} threw {ex.GetType().Name}: {ex.Message}");
            }
        }

        yield return wrong.Count == 0
            ? CheckResult.Pass(Area, $"All {Conditions.Length} sample conditions evaluate correctly")
            : CheckResult.Fail(Area, $"All {Conditions.Length} sample conditions evaluate correctly",
                string.Join("; ", wrong.Take(5)) + (wrong.Count > 5 ? $" (+{wrong.Count - 5} more)" : ""));

        // Values, not just truth: this is what TSVar evaluation depends on.
        yield return ValueCheck(engine, "\"C:\"", "C:");
        yield return ValueCheck(engine, "1 + 1", "2");
        yield return ValueCheck(engine, "Left(\"CORP\", 2) & \"-\"", "CO-");

        // Plain text must survive unevaluated, or every literal value is mangled.
        yield return engine.TryEvaluateValue("Adobe Reader DC", out _)
            ? CheckResult.Fail(Area, "Plain text is left alone rather than evaluated",
                "the engine claimed to evaluate 'Adobe Reader DC'")
            : CheckResult.Pass(Area, "Plain text is left alone rather than evaluated");

        // The COM shim: the reason a config using FileSystemObject no longer
        // needs the WinPE-Scripting component.
        yield return ShimCheck(engine, context);

        // And the constructs that still need a script host must be reported,
        // never silently false.
        var result = engine.TryEvaluate("GetObject(\"winmgmts:\") = 1", vars);

        yield return result.Problems.Any(p => p.Kind == ConditionDiagnosticKind.RequiresComHost)
            ? CheckResult.Pass(Area, "A construct needing VBScript is reported, not silently false")
            : CheckResult.Fail(Area, "A construct needing VBScript is reported, not silently false",
                "GetObject produced no diagnostic");
    }

    private CheckResult ValueCheck(NativeConditionEvaluator engine, string expression, string expected)
    {
        var name = $"The expression {expression} evaluates to {expected}";

        try
        {
            return engine.TryEvaluateValue(expression, out var actual) && actual == expected
                ? CheckResult.Pass(Area, name)
                : CheckResult.Fail(Area, name, $"got '{actual}'");
        }
        catch (Exception ex)
        {
            return CheckResult.Fail(Area, name, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private CheckResult ShimCheck(NativeConditionEvaluator engine, SelfTestContext context)
    {
        const string name = "CreateObject(\"Scripting.FileSystemObject\") works without a script host";

        // Against a directory that certainly exists here.
        var expression =
            $"CreateObject(\"Scripting.FileSystemObject\").FolderExists(\"{context.ScratchDirectory}\")";

        try
        {
            var result = engine.TryEvaluate(expression, new Dictionary<string, string>());

            if (!result.Value)
                return CheckResult.Fail(Area, name,
                    $"FolderExists said no for {context.ScratchDirectory}");

            return result.IsReliable
                ? CheckResult.Pass(Area, name)
                : CheckResult.Fail(Area, name, result.DescribeProblems());
        }
        catch (Exception ex)
        {
            return CheckResult.Fail(Area, name, $"{ex.GetType().Name}: {ex.Message}");
        }
    }
}
