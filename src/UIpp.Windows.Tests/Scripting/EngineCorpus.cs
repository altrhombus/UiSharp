namespace UIpp.Windows.Tests.Scripting;

/// <summary>
/// The expression corpus used to compare the native engine against the real
/// VBScript host. Deliberately includes the shapes that appear in the original
/// project's own sample configs alongside synthetic coverage of every operator
/// and built-in the native engine claims to support.
/// </summary>
internal static class EngineCorpus
{
    /// <summary>
    /// Expressions whose result is deterministic and locale-independent, so the
    /// two engines can be compared directly.
    /// </summary>
    public static readonly string[] Deterministic =
    [
        // ---- Comparisons, numeric
        "1 = 1", "1 = 2", "1 <> 2", "1 < 2", "2 > 1", "1 <= 1", "2 >= 3", "-5 < 0",
        "3.14 > 3", "1024 >= 1024", "2048 >= 1024", "0 = 0",

        // ---- Comparisons, string (same case)
        "\"LAPTOP\" = \"LAPTOP\"",
        "\"LAPTOP\" = \"DESKTOP\"",
        "\"LAPTOP\" <> \"DESKTOP\"",
        "\"A\" < \"B\"", "\"B\" > \"A\"", "\"A\" <= \"A\"", "\"A\" >= \"B\"",
        "\"\" = \"\"",
        "\"False\" = \"False\"",
        "\"True\" = \"True\"",

        // ---- Comparisons, string (differing case) — probes compare semantics
        "\"laptop\" = \"LAPTOP\"",
        "\"Laptop\" = \"laptop\"",

        // ---- Boolean operators
        "\"A\" = \"A\" AND \"1\" = \"1\"",
        "\"A\" = \"A\" AND \"1\" = \"2\"",
        "\"A\" = \"B\" OR \"1\" = \"1\"",
        "\"A\" = \"B\" OR \"1\" = \"2\"",
        "NOT \"1\" = \"2\"",
        "NOT \"1\" = \"1\"",
        "(\"A\" = \"A\" OR \"B\" = \"C\") AND NOT 1 = 2",
        "True AND True",
        "True AND False",
        "True OR False",

        // ---- Arithmetic
        "1 + 1", "10 - 4", "3 * 4", "10 / 4", "10 \\ 4", "10 Mod 3", "2 ^ 10",
        "2 + 3 * 4", "(2 + 3) * 4", "-5 + 2", "2 ^ -1", "7 Mod 4", "16384 Mod 1024",
        "100 / 8", "1 + 2 + 3 + 4",

        // ---- Concatenation
        "\"a\" & \"b\"",
        "1 & 2",
        "\"1\" + \"2\"",
        "\"1\" + 2",
        "\"pre\" & \"-\" & \"post\"",

        // ---- String built-ins (matching case, so compare mode is not a factor)
        "InStr(\"SRV-001\", \"SRV\")",
        "InStr(\"WKS-001\", \"SRV\")",
        "InStr(\"Hello\", \"ell\")",
        "InStr(\"Hello\", \"xyz\")",
        "InStr(3, \"abcabc\", \"a\")",
        "InStrRev(\"a/b/c\", \"/\")",
        "UCase(\"abc\")", "LCase(\"ABC\")",
        "Len(\"abcd\")", "Len(\"\")",
        "Mid(\"abcdef\", 2, 3)", "Left(\"abcdef\", 2)", "Right(\"abcdef\", 2)",
        "Trim(\"  ab  \")", "LTrim(\"  ab\")", "RTrim(\"ab  \")",
        "Replace(\"a-b\", \"-\", \"_\")",
        "StrReverse(\"abc\")",
        "Asc(\"A\")", "Chr(65)",
        "Space(3)",

        // ---- String built-ins with differing case — probes InStr compare mode
        "InStr(\"Hello\", \"ELL\")",
        "Replace(\"A-b\", \"a\", \"_\")",

        // ---- Numeric built-ins
        "IsNumeric(\"42\")", "IsNumeric(\"3.14\")", "IsNumeric(\"abc\")",
        "Int(3.9)", "Int(-3.5)", "Fix(-3.5)", "Fix(3.9)",
        "Abs(-3)", "Sgn(-7)", "Sqr(16)",
        "CInt(\"3.6\")", "CInt(0.5)", "CInt(1.5)", "CInt(2.5)",
        "CDbl(\"3.5\")", "CLng(2.5)",
        "CBool(1)", "CBool(0)",
        "Round(3.14159, 2)", "Round(2.5)", "Round(3.5)", "Round(1.005, 2)",
        "Hex(255)",
        "CStr(42)",

        // ---- Nested calls
        "UCase(Left(\"abcdef\", 3))",
        "InStr(UCase(\"srv-01\"), \"SRV\")",
        "Len(Trim(\"  ab  \"))",
        "Round(1024 / 3, 1)",

        // ---- Shapes taken from the original sample configs, post-substitution
        "\"CORP\" = \"\"",
        "Left(\"CORP\",2) & Left(\"abrown\",2)",
        "Trim(\"10.0.50.1\")",
        "LCase(Trim(\"Fire\"))",
        "Round(64424509440 / 1024 / 1024 / 1024, 2) > 30",
        "\"C:\"",
        "\"Adobe Reader DC 2019\"",
    ];

    /// <summary>
    /// Expressions VBScript rejects outright. Both engines must decline to
    /// produce a value for these so the caller keeps the literal text.
    /// </summary>
    public static readonly string[] NotExpressions =
    [
        "Please choose a volume",
        "Adobe Reader DC 2019",
        "Microsoft Office 365 Pro Plus",
        "root\\cimv2",
        "Install .Net 4.5.2",
        "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion",
    ];

    /// <summary>
    /// Expressions that genuinely error at runtime in VBScript.
    /// </summary>
    public static readonly string[] RuntimeErrors =
    [
        "1 / 0",
        "1 Mod 0",
        "10 \\ 0",
        "\"abc\" - 1",
        "\"abc\" * 2",
    ];

    /// <summary>
    /// Left out of direct comparison because the value depends on the clock or
    /// the host locale, not on engine semantics.
    /// </summary>
    public static readonly string[] NonDeterministic =
    [
        "Now()", "Date()", "Time()",
        "Year(Date())", "Month(Date())", "Day(Date())", "Weekday(Date())",
    ];
}
