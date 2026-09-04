using UIpp.Core.Scripting;

namespace UIpp.Core.Tests.Scripting;

/// <summary>
/// Cross-platform tests for the COM compatibility shim and the UiSharp-native
/// functions that replace it, against a fixed <see cref="IScriptHostServices"/>.
///
/// The shim lets existing XML run without the WinPE-Scripting component; it is a
/// bridge, not the destination. Correctness against the real VBScript engine is
/// asserted separately by the differential tests in UIpp.Windows.Tests, which
/// only run on a machine with vbscript.dll — hence this suite.
/// </summary>
public class ComCompatibilityTests
{
    private sealed class FakeHost : IScriptHostServices
    {
        public bool FileExists(string path)   => path == @"C:\present.txt";
        public bool FolderExists(string path) => path == @"C:\Windows";
        public bool DriveExists(string drive) => drive is "C:" or "C" or @"C:\";

        public string ComputerName => "TESTPC";
        public string UserName     => "tester";
        public string UserDomain   => "TESTDOMAIN";

        public string ExpandEnvironmentStrings(string input) =>
            input.Replace("%TESTVAR%", "expanded", StringComparison.OrdinalIgnoreCase);
    }

    private readonly NativeConditionEvaluator _eval = new(new FakeHost());
    private readonly IReadOnlyDictionary<string, string> _empty = new Dictionary<string, string>();

    private ConditionResult Run(string expr) => _eval.TryEvaluate(expr, _empty);
    private string? Value(string expr) => _eval.TryEvaluateValue(expr, out var v) ? v : null;

    // -------------------------------------------------------------------------
    // The compatibility shim
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(@"CreateObject(""Scripting.FileSystemObject"").FileExists(""C:\present.txt"")", true)]
    [InlineData(@"CreateObject(""Scripting.FileSystemObject"").FileExists(""C:\absent.txt"")", false)]
    [InlineData(@"CreateObject(""Scripting.FileSystemObject"").FolderExists(""C:\Windows"")", true)]
    [InlineData(@"CreateObject(""Scripting.FileSystemObject"").FolderExists(""C:\Nope"")", false)]
    [InlineData(@"CreateObject(""Scripting.FileSystemObject"").DriveExists(""C:"")", true)]
    [InlineData(@"CreateObject(""Scripting.FileSystemObject"").DriveExists(""Q:"")", false)]
    public void ShimConditions_Evaluate(string expr, bool expected)
    {
        var result = Run(expr);

        Assert.Equal(expected, result.Value);
        Assert.True(result.IsReliable, result.DescribeProblems());
    }

    [Theory]
    [InlineData(@"CreateObject(""WScript.Network"").ComputerName", "TESTPC")]
    [InlineData(@"CreateObject(""WScript.Network"").UserName", "tester")]
    [InlineData(@"CreateObject(""WScript.Network"").UserDomain", "TESTDOMAIN")]
    [InlineData(@"CreateObject(""WScript.Shell"").ExpandEnvironmentStrings(""x-%TESTVAR%"")", "x-expanded")]
    public void ShimProperties_ReturnHostValues(string expr, string expected) =>
        Assert.Equal(expected, Value(expr));

    // Property access takes no parentheses, method calls do — both must work.
    [Fact]
    public void ShimMember_WorksWithAndWithoutParentheses()
    {
        Assert.Equal("TESTPC", Value(@"CreateObject(""WScript.Network"").ComputerName"));
        Assert.Equal("TESTPC", Value(@"CreateObject(""WScript.Network"").ComputerName()"));
    }

    // ProgIDs and member names are case-insensitive in VBScript.
    [Fact]
    public void ShimLookup_IsCaseInsensitive()
    {
        Assert.True(Run(@"CreateObject(""scripting.filesystemobject"").fileexists(""C:\present.txt"")").Value);
        Assert.True(Run(@"CreateObject(""WSCRIPT.NETWORK"").COMPUTERNAME <> """"").Value);
    }

    [Fact]
    public void ShimResult_ComposesWithTheRestOfTheGrammar()
    {
        Assert.True(Run(
            @"CreateObject(""Scripting.FileSystemObject"").FileExists(""C:\present.txt"") = True").Value);

        Assert.True(Run(
            @"NOT CreateObject(""Scripting.FileSystemObject"").FileExists(""C:\absent.txt"")").Value);

        Assert.Equal("PRESENT", Value(
            @"UCase(CreateObject(""Scripting.FileSystemObject"").GetBaseName(""C:\present.txt""))"));
    }

    // -------------------------------------------------------------------------
    // Migration advice
    // -------------------------------------------------------------------------

    [Fact]
    public void Shim_AdvisesTheNativeReplacement()
    {
        var result = Run(@"CreateObject(""Scripting.FileSystemObject"").FileExists(""C:\present.txt"")");

        var advice = Assert.Single(result.Advice);
        Assert.Equal(ConditionDiagnosticKind.ComCompatibilityShim, advice.Kind);
        Assert.Contains("FileExists(path)", advice.Detail);
    }

    // The critical property: advice must never change the answer, or every
    // config using CreateObject would start evaluating false.
    [Fact]
    public void Advice_IsNotBlocking()
    {
        var result = Run(@"CreateObject(""Scripting.FileSystemObject"").FolderExists(""C:\Windows"")");

        Assert.True(result.Value);
        Assert.True(result.IsReliable);
        Assert.NotEmpty(result.Advice);
        Assert.Empty(result.Problems);
    }

    [Fact]
    public void NativeFunctions_CarryNoAdvice()
    {
        var result = Run(@"FileExists(""C:\present.txt"")");

        Assert.True(result.Value);
        Assert.Empty(result.Advice);
    }

    // -------------------------------------------------------------------------
    // Native replacements agree with the COM form they replace
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(@"FileExists(""C:\present.txt"")",   @"CreateObject(""Scripting.FileSystemObject"").FileExists(""C:\present.txt"")")]
    [InlineData(@"FileExists(""C:\absent.txt"")",    @"CreateObject(""Scripting.FileSystemObject"").FileExists(""C:\absent.txt"")")]
    [InlineData(@"FolderExists(""C:\Windows"")",     @"CreateObject(""Scripting.FileSystemObject"").FolderExists(""C:\Windows"")")]
    [InlineData(@"DriveExists(""C:"")",              @"CreateObject(""Scripting.FileSystemObject"").DriveExists(""C:"")")]
    [InlineData(@"PathParent(""C:\a\b\c.txt"")",     @"CreateObject(""Scripting.FileSystemObject"").GetParentFolderName(""C:\a\b\c.txt"")")]
    [InlineData(@"PathFileName(""C:\a\b\c.txt"")",   @"CreateObject(""Scripting.FileSystemObject"").GetFileName(""C:\a\b\c.txt"")")]
    [InlineData(@"PathBaseName(""C:\a\c.txt"")",     @"CreateObject(""Scripting.FileSystemObject"").GetBaseName(""C:\a\c.txt"")")]
    [InlineData(@"PathExtension(""C:\a\c.txt"")",    @"CreateObject(""Scripting.FileSystemObject"").GetExtensionName(""C:\a\c.txt"")")]
    [InlineData(@"PathDrive(""C:\a\b"")",            @"CreateObject(""Scripting.FileSystemObject"").GetDriveName(""C:\a\b"")")]
    [InlineData(@"PathCombine(""C:\a"", ""b"")",     @"CreateObject(""Scripting.FileSystemObject"").BuildPath(""C:\a"", ""b"")")]
    [InlineData("ComputerName()",                    @"CreateObject(""WScript.Network"").ComputerName")]
    [InlineData("UserName()",                        @"CreateObject(""WScript.Network"").UserName")]
    [InlineData("UserDomain()",                      @"CreateObject(""WScript.Network"").UserDomain")]
    [InlineData(@"ExpandEnvironment(""x-%TESTVAR%"")", @"CreateObject(""WScript.Shell"").ExpandEnvironmentStrings(""x-%TESTVAR%"")")]
    public void NativeFunction_MatchesTheComForm(string native, string com) =>
        Assert.Equal(Value(com), Value(native));

    // Path helpers are pure string manipulation with Windows conventions, so
    // they must behave the same regardless of the host OS.
    [Theory]
    [InlineData(@"PathParent(""C:\a\b\c.txt"")", @"C:\a\b")]
    [InlineData(@"PathParent(""C:\a"")", @"C:\")]
    [InlineData(@"PathFileName(""C:\a\b\c.txt"")", "c.txt")]
    [InlineData(@"PathBaseName(""C:\a\archive.tar.gz"")", "archive.tar")]
    [InlineData(@"PathExtension(""C:\a\c.txt"")", "txt")]
    [InlineData(@"PathDrive(""C:\a\b"")", "C:")]
    [InlineData(@"PathCombine(""C:\a"", ""b"")", @"C:\a\b")]
    [InlineData(@"PathCombine(""C:\a\"", ""b"")", @"C:\a\b")]
    public void PathHelpers_UseWindowsConventions(string expr, string expected) =>
        Assert.Equal(expected, Value(expr));

    // -------------------------------------------------------------------------
    // What is still refused
    // -------------------------------------------------------------------------

    [Fact]
    public void UnknownProgId_IsRefusedAndNamed()
    {
        var result = Run(@"CreateObject(""Scripting.Dictionary"")");

        Assert.False(result.Value);
        Assert.Contains(result.Problems,
            d => d.Kind == ConditionDiagnosticKind.RequiresComHost &&
                 d.Detail.Contains("Scripting.Dictionary"));
    }

    [Fact]
    public void UnimplementedMember_IsRefusedAndNamed()
    {
        var result = Run(@"CreateObject(""WScript.Shell"").RegRead(""HKLM\Software"")");

        var problem = Assert.Single(result.Problems);
        Assert.Equal(ConditionDiagnosticKind.RequiresComHost, problem.Kind);
        Assert.Contains("WScript.Shell", problem.Detail);
        Assert.Contains("RegRead", problem.Detail);
    }

    [Fact]
    public void ObjectReference_IsNotAValue()
    {
        // Nothing useful can be stored in a task-sequence variable, so the value
        // path declines and the caller keeps the literal text.
        Assert.Null(Value(@"CreateObject(""Scripting.FileSystemObject"")"));
    }

    [Theory]
    [InlineData(@"GetObject(""winmgmts:root\cimv2"")")]
    [InlineData(@"Eval(""1 = 1"")")]
    [InlineData(@"Execute(""x = 1"")")]
    public void ConstructsWithNoNativeForm_AreStillRefused(string expr)
    {
        var result = Run(expr);

        Assert.False(result.Value);
        Assert.Contains(result.Problems,
            d => d.Kind == ConditionDiagnosticKind.RequiresComHost);
    }

    // -------------------------------------------------------------------------
    // Regression: a version string in free text once crashed the lexer
    // -------------------------------------------------------------------------

    // "4.5.2" scans as one run of digits and dots, and double.Parse threw an
    // unhandled FormatException on it — which during a deployment killed UIpp
    // rather than reporting a bad condition.
    [Theory]
    [InlineData("Install .Net 4.5.2")]
    [InlineData("1.2.3.4")]
    [InlineData("Version 10.0.19041.1")]
    public void VersionLikeText_IsReportedNotThrown(string expr)
    {
        var result = Run(expr);

        Assert.False(result.Value);
        Assert.False(result.IsReliable);
    }

    [Fact]
    public void OrdinaryDecimals_StillParse()
    {
        Assert.True(Run("3.14 > 3").Value);
        Assert.Equal("3.5", Value("3.5"));
    }
}
