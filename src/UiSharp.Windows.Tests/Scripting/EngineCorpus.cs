namespace UiSharp.Windows.Tests.Scripting;

/// <summary>
/// The expression corpus used to compare the native engine against the real
/// VBScript host. Deliberately includes the shapes that appear in the original
/// project's own sample configs alongside synthetic coverage of every operator,
/// built-in and CreateObject member the native engine claims to support.
///
/// Written with verbatim strings so the VBScript source reads as it would in a
/// config file: <c>""</c> is a quote and a single backslash is a backslash.
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
        @"""LAPTOP"" = ""LAPTOP""",
        @"""LAPTOP"" = ""DESKTOP""",
        @"""LAPTOP"" <> ""DESKTOP""",
        @"""A"" < ""B""", @"""B"" > ""A""", @"""A"" <= ""A""", @"""A"" >= ""B""",
        @""""" = """"",
        @"""False"" = ""False""",
        @"""True"" = ""True""",

        // ---- Comparisons, string (differing case) — probes compare semantics
        @"""laptop"" = ""LAPTOP""",
        @"""Laptop"" = ""laptop""",

        // ---- Boolean operators
        @"""A"" = ""A"" AND ""1"" = ""1""",
        @"""A"" = ""A"" AND ""1"" = ""2""",
        @"""A"" = ""B"" OR ""1"" = ""1""",
        @"""A"" = ""B"" OR ""1"" = ""2""",
        @"NOT ""1"" = ""2""",
        @"NOT ""1"" = ""1""",
        @"(""A"" = ""A"" OR ""B"" = ""C"") AND NOT 1 = 2",
        "True AND True",
        "True AND False",
        "True OR False",
        "NOT False",
        "NOT True",

        // ---- Arithmetic
        "1 + 1", "10 - 4", "3 * 4", "10 / 4", @"10 \ 4", "10 Mod 3", "2 ^ 10",
        "2 + 3 * 4", "(2 + 3) * 4", "-5 + 2", "2 ^ -1", "7 Mod 4", "16384 Mod 1024",
        "100 / 8", "1 + 2 + 3 + 4",

        // ---- Concatenation
        @"""a"" & ""b""",
        "1 & 2",
        @"""1"" + ""2""",
        @"""1"" + 2",
        @"""pre"" & ""-"" & ""post""",

        // ---- String built-ins (matching case, so compare mode is not a factor)
        @"InStr(""SRV-001"", ""SRV"")",
        @"InStr(""WKS-001"", ""SRV"")",
        @"InStr(""Hello"", ""ell"")",
        @"InStr(""Hello"", ""xyz"")",
        @"InStr(3, ""abcabc"", ""a"")",
        @"InStrRev(""a/b/c"", ""/"")",
        @"UCase(""abc"")", @"LCase(""ABC"")",
        @"Len(""abcd"")", @"Len("""")",
        @"Mid(""abcdef"", 2, 3)", @"Left(""abcdef"", 2)", @"Right(""abcdef"", 2)",
        @"Trim(""  ab  "")", @"LTrim(""  ab"")", @"RTrim(""ab  "")",
        @"Replace(""a-b"", ""-"", ""_"")",
        @"StrReverse(""abc"")",
        @"Asc(""A"")", "Chr(65)",
        "Space(3)",

        // ---- String built-ins with differing case — probes InStr compare mode
        @"InStr(""Hello"", ""ELL"")",
        @"InStr(1, ""Hello"", ""ELL"", 1)",
        @"Replace(""A-b"", ""a"", ""_"")",

        // ---- Numeric built-ins
        @"IsNumeric(""42"")", @"IsNumeric(""3.14"")", @"IsNumeric(""abc"")",
        "Int(3.9)", "Int(-3.5)", "Fix(-3.5)", "Fix(3.9)",
        "Abs(-3)", "Sgn(-7)", "Sqr(16)",
        @"CInt(""3.6"")", "CInt(0.5)", "CInt(1.5)", "CInt(2.5)",
        @"CDbl(""3.5"")", "CLng(2.5)",
        "CBool(1)", "CBool(0)",
        "Round(3.14159, 2)", "Round(2.5)", "Round(3.5)", "Round(1.005, 2)",
        "Hex(255)",
        "CStr(42)",

        // ---- The functions the documentation calls "common", which the native
        //      engine previously lacked. Split and StrComp are named on the
        //      prerequisites page; Split needs a real array value.
        @"StrComp(""a"", ""b"")",
        @"StrComp(""b"", ""a"")",
        @"StrComp(""a"", ""a"")",
        @"StrComp(""a"", ""A"")",
        @"StrComp(""a"", ""A"", 1)",
        @"UBound(Split(""a,b,c"", "",""))",
        @"LBound(Split(""a,b,c"", "",""))",
        @"Split(""a,b,c"", "","")(0)",
        @"Split(""a,b,c"", "","")(2)",
        @"Split(""a b c"")(1)",
        @"Split(""a,b,c"", "","", 2)(1)",
        @"Split(""one::two"", ""::"")(1)",
        @"Split("""", "","")(0)",
        @"Join(Split(""a,b,c"", "",""), ""-"")",
        @"Join(Split(""a,b,c"", "",""))",
        @"UBound(Filter(Split(""ab,cd,ce"", "",""), ""c""))",
        @"Join(Filter(Split(""ab,cd,ce"", "",""), ""c""), ""|"")",
        @"Join(Filter(Split(""ab,cd,ce"", "",""), ""c"", False), ""|"")",
        @"IsArray(Split(""a,b"", "",""))",
        @"IsArray(""a,b"")",
        @"String(3, ""x"")",
        @"String(0, ""x"")",

        // ---- Dates. Fixed inputs only: anything derived from the clock cannot
        //      be compared between two engines.
        @"DateAdd(""d"", 1, ""1/2/2020"")",
        @"DateAdd(""d"", -1, ""1/2/2020"")",
        @"DateAdd(""m"", 2, ""1/2/2020"")",
        @"DateAdd(""yyyy"", 1, ""1/2/2020"")",
        @"DateAdd(""ww"", 1, ""1/2/2020"")",
        @"Year(DateAdd(""yyyy"", 5, ""1/2/2020""))",
        @"Month(DateAdd(""m"", 13, ""1/2/2020""))",
        @"DateDiff(""d"", ""1/1/2020"", ""1/31/2020"")",
        @"DateDiff(""m"", ""1/1/2020"", ""6/1/2020"")",
        @"DateDiff(""yyyy"", ""1/1/2020"", ""1/1/2024"")",
        @"DateDiff(""ww"", ""1/1/2020"", ""1/29/2020"")",
        @"DatePart(""yyyy"", ""3/4/2021"")",
        @"DatePart(""m"", ""3/4/2021"")",
        @"DatePart(""d"", ""3/4/2021"")",
        @"DatePart(""q"", ""8/4/2021"")",
        @"DatePart(""w"", ""3/4/2021"")",
        @"Hour(""3/4/2021 13:45:12"")",
        @"Minute(""3/4/2021 13:45:12"")",
        @"Second(""3/4/2021 13:45:12"")",
        @"IsDate(""3/4/2021"")",
        @"IsDate(""not a date"")",
        @"MonthName(3)",
        @"MonthName(3, True)",
        @"WeekdayName(1)",
        @"WeekdayName(1, True)",

        // ---- Remaining numeric conversions.
        @"Oct(8)",
        @"Oct(64)",
        @"Log(1)",
        @"Exp(0)",

        // ---- Shapes a real config would use these for.
        @"UBound(Split(""%Depts%"", "","")) >= 0",
        @"Join(Split(""Fire,IST,HR"", "",""), "" / "")",
        @"StrComp(""LENOVO"", ""LENOVO"") = 0",
        @"DateDiff(""d"", ""1/1/2020"", ""1/1/2021"") > 300",

        // ---- The rest of the VBScript surface.
        @"CByte(200)", @"CByte(2.6)", @"CSng(1.5)", @"CCur(1.2345)",
        @"RGB(255, 0, 0)", @"RGB(0, 255, 0)", @"RGB(0, 0, 255)", @"RGB(1, 2, 3)",
        @"Sin(0)", @"Cos(0)", @"Tan(0)", @"Atn(0)", @"Atn(1) > 0.78",
        @"UBound(Array(1, 2, 3))",
        @"Join(Array(""a"", ""b""), ""-"")",
        @"IsObject(""x"")",
        @"TypeName(""abc"")",
        @"TypeName(42)",
        @"TypeName(42.5)",
        @"TypeName(True)",
        @"TypeName(100000)",
        @"TypeName(Array(1))",
        @"VarType(""abc"")",
        @"VarType(42)",
        @"VarType(42.5)",
        @"VarType(True)",
        @"CDate(""3/4/2021"")",
        @"DateValue(""3/4/2021 13:45:12"")",
        @"TimeValue(""3/4/2021 13:45:12"")",
        @"DateSerial(2020, 1, 2)",
        @"DateSerial(2020, 13, 1)",
        @"TimeSerial(13, 45, 12)",
        @"Year(DateSerial(2020, 1, 2))",
        @"FormatDateTime(""3/4/2021 13:45:12"", 4)",
        // FormatDateTime with a date pattern, and FormatCurrency, are absent on
        // purpose: both render per locale, which the runtime deliberately does
        // not follow. See NativeFormattingTests.
        @"FormatNumber(1234.5678)",
        @"FormatNumber(1234.5678, 1)",
        @"FormatPercent(0.125)",
        @"FormatPercent(0.125, 1)",

        // ---- Nested calls
        @"UCase(Left(""abcdef"", 3))",
        @"InStr(UCase(""srv-01""), ""SRV"")",
        @"Len(Trim(""  ab  ""))",
        "Round(1024 / 3, 1)",

        // ---- Shapes taken from the original sample configs, post-substitution
        @"""CORP"" = """"",
        @"Left(""CORP"",2) & Left(""abrown"",2)",
        @"Trim(""10.0.50.1"")",
        @"LCase(Trim(""Fire""))",
        "Round(64424509440 / 1024 / 1024 / 1024, 2) > 30",
        @"""C:""",
        @"""Adobe Reader DC 2019""",
    ];

    /// <summary>
    /// CreateObject expressions handled by the compatibility shim. These must
    /// agree with real VBScript exactly — that is the whole point of the shim,
    /// since existing configs are written against the COM surface.
    ///
    /// The path members are pure string manipulation so they compare precisely;
    /// the *Exists members are compared against the same filesystem both engines
    /// can see.
    /// </summary>
    public static readonly string[] ComCompatibility =
    [
        @"CreateObject(""Scripting.FileSystemObject"").GetParentFolderName(""C:\a\b\c.txt"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetParentFolderName(""C:\a"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetParentFolderName(""C:\"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetParentFolderName(""c.txt"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetParentFolderName(""C:\a\b\"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetFileName(""C:\a\b\c.txt"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetFileName(""C:\a\"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetFileName(""C:\"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetFileName(""C:"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetBaseName(""C:\a\c.txt"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetBaseName(""C:\a\archive.tar.gz"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetBaseName(""C:\a\noext"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetBaseName(""C:\a\.hidden"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetBaseName(""C:\a\.hidden.txt"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetBaseName(""C:\a\"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetExtensionName(""C:\a\.hidden"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetExtensionName(""C:\a\c.txt"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetExtensionName(""C:\a\noext"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetExtensionName(""C:\a\trailing."")",
        @"CreateObject(""Scripting.FileSystemObject"").GetDriveName(""C:\a\b"")",
        @"CreateObject(""Scripting.FileSystemObject"").GetDriveName(""relative\path"")",
        @"CreateObject(""Scripting.FileSystemObject"").BuildPath(""C:\a"", ""b"")",
        @"CreateObject(""Scripting.FileSystemObject"").BuildPath(""C:\a\"", ""b"")",
        @"CreateObject(""Scripting.FileSystemObject"").BuildPath("""", ""b"")",
        @"CreateObject(""Scripting.FileSystemObject"").FileExists(""C:\Windows\explorer.exe"")",
        @"CreateObject(""Scripting.FileSystemObject"").FileExists(""C:\does\not\exist.txt"")",
        @"CreateObject(""Scripting.FileSystemObject"").FolderExists(""C:\Windows"")",
        @"CreateObject(""Scripting.FileSystemObject"").FolderExists(""C:\does\not\exist"")",
        @"CreateObject(""Scripting.FileSystemObject"").DriveExists(""C:"")",
        @"CreateObject(""Scripting.FileSystemObject"").DriveExists(""Q:"")",
        @"CreateObject(""WScript.Network"").ComputerName",
        @"CreateObject(""WScript.Network"").UserName",
        @"CreateObject(""WScript.Network"").UserDomain",

        // Composed with the rest of the grammar, as a real condition would be.
        @"CreateObject(""Scripting.FileSystemObject"").FileExists(""C:\Windows\explorer.exe"") = True",
        @"NOT CreateObject(""Scripting.FileSystemObject"").FileExists(""C:\nope"")",
        @"UCase(CreateObject(""Scripting.FileSystemObject"").GetFileName(""C:\a\c.txt""))",
        @"Len(CreateObject(""WScript.Network"").ComputerName) > 0",
        @"CreateObject(""Scripting.FileSystemObject"").GetExtensionName(""C:\a\c.txt"") = ""txt""",
    ];

    /// <summary>
    /// UiSharp-native replacements for the shim, paired with the COM expression
    /// they replace. Both sides must produce the same answer, which is what makes
    /// a migration safe — but only the COM side runs under VBScript.
    /// </summary>
    public static readonly (string Native, string Com)[] NativeEquivalents =
    [
        (@"FileExists(""C:\Windows\explorer.exe"")",
         @"CreateObject(""Scripting.FileSystemObject"").FileExists(""C:\Windows\explorer.exe"")"),
        (@"FileExists(""C:\does\not\exist.txt"")",
         @"CreateObject(""Scripting.FileSystemObject"").FileExists(""C:\does\not\exist.txt"")"),
        (@"FolderExists(""C:\Windows"")",
         @"CreateObject(""Scripting.FileSystemObject"").FolderExists(""C:\Windows"")"),
        (@"FolderExists(""C:\does\not\exist"")",
         @"CreateObject(""Scripting.FileSystemObject"").FolderExists(""C:\does\not\exist"")"),
        (@"DriveExists(""C:"")",
         @"CreateObject(""Scripting.FileSystemObject"").DriveExists(""C:"")"),
        (@"PathParent(""C:\a\b\c.txt"")",
         @"CreateObject(""Scripting.FileSystemObject"").GetParentFolderName(""C:\a\b\c.txt"")"),
        (@"PathFileName(""C:\a\b\c.txt"")",
         @"CreateObject(""Scripting.FileSystemObject"").GetFileName(""C:\a\b\c.txt"")"),
        (@"PathBaseName(""C:\a\archive.tar.gz"")",
         @"CreateObject(""Scripting.FileSystemObject"").GetBaseName(""C:\a\archive.tar.gz"")"),
        (@"PathExtension(""C:\a\c.txt"")",
         @"CreateObject(""Scripting.FileSystemObject"").GetExtensionName(""C:\a\c.txt"")"),
        (@"PathDrive(""C:\a\b"")",
         @"CreateObject(""Scripting.FileSystemObject"").GetDriveName(""C:\a\b"")"),
        (@"PathCombine(""C:\a"", ""b"")",
         @"CreateObject(""Scripting.FileSystemObject"").BuildPath(""C:\a"", ""b"")"),
        ("ComputerName()", @"CreateObject(""WScript.Network"").ComputerName"),
        ("UserName()",     @"CreateObject(""WScript.Network"").UserName"),
        ("UserDomain()",   @"CreateObject(""WScript.Network"").UserDomain"),
    ];

    /// <summary>
    /// UiSharp extensions with no VBScript counterpart of any kind — not a COM
    /// idiom being replaced, but capability VBScript never had. Real VBScript
    /// must reject these, which is exactly the cost of using them: such a config
    /// will not run under the original C++ UI++.
    /// </summary>
    public static readonly string[] NativeOnlyExtensions =
    [
        @"EqualsIgnoreCase(""LENOVO"", ""Lenovo"")",
        @"IsSet(""%XHWMemory%"")",
        @"IsSet(""8192"")",
        @"InList(""Fire,IST,HR"", ""ist"")",
        @"VersionCompare(""10.0.19041"", ""10.0.9600"")",
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
        @"root\cimv2",
        "Install .Net 4.5.2",
        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion",
    ];

    /// <summary>
    /// Expressions that genuinely error at runtime in VBScript.
    /// </summary>
    public static readonly string[] RuntimeErrors =
    [
        "1 / 0",
        "1 Mod 0",
        @"10 \ 0",
        @"""abc"" - 1",
        @"""abc"" * 2",
    ];

    /// <summary>
    /// Constructs that still genuinely need a script host. VBScript can evaluate
    /// these; the native engine must decline and say why, so configs using them
    /// keep working under ConditionEngine="vbscript" rather than silently
    /// producing a wrong answer.
    ///
    /// Split used to be here. It is implemented natively now, because the
    /// documentation lists it among the functions most often used with UI++.
    /// </summary>
    public static readonly string[] RequireScriptHost =
    [
        @"GetObject(""winmgmts:\\.\root\cimv2"")",
        @"Eval(""1 = 1"")",
        @"Execute(""x = 1"")",
        // A ProgID with no native equivalent.
        @"CreateObject(""Scripting.Dictionary"")",
        // A supported object, but a member that is not implemented.
        @"CreateObject(""WScript.Shell"").RegRead(""HKLM\Software"")",
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
