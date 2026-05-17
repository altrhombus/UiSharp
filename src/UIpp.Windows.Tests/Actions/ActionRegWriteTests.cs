using Microsoft.Win32;
using System.Xml.Linq;
using UIpp.Core.Actions;
using UIpp.Core.Dialogs;
using UIpp.Core.Scripting;
using UIpp.Core.Variables;
using UIpp.Windows.Actions;
using UIpp.Windows.Tests;

namespace UIpp.Windows.Tests.Actions;

// Exercises ActionRegWrite against a temporary HKCU test key.
public sealed class ActionRegWriteTests : IDisposable
{
    private const string TestRoot = @"SOFTWARE\UiSharpTests";
    private readonly string _subKey;

    public ActionRegWriteTests()
    {
        _subKey = $"{TestRoot}\\RegWrite_{Guid.NewGuid():N}";
        // Key is created on first write; ensure the parent exists.
        Registry.CurrentUser.CreateSubKey(TestRoot).Dispose();
    }

    public void Dispose() =>
        Registry.CurrentUser.DeleteSubKeyTree(_subKey, throwOnMissingSubKey: false);

    private void Run(string xml, LocalTSEnv? env = null)
    {
        var e    = XElement.Parse(xml);
        var data = new ActionData
        {
            ActionNode         = e,
            Conditions         = new NativeConditionEvaluator(),
            TsEnv              = env ?? new LocalTSEnv(),
            Log                = NullLog.Instance,
            GlobalDialogTraits = new DialogTraits(),
        };
        new ActionRegWrite(data).Go();
    }

    private object? ReadBack(string valueName, RegistryValueKind? expectedKind = null)
    {
        using var k = Registry.CurrentUser.OpenSubKey(_subKey);
        if (k is null) return null;
        if (expectedKind.HasValue)
            Assert.Equal(expectedKind.Value, k.GetValueKind(valueName));
        return k.GetValue(valueName, null);
    }

    private RegistryValueKind GetValueKind(string valueName)
    {
        using var k = Registry.CurrentUser.OpenSubKey(_subKey)!;
        return k.GetValueKind(valueName);
    }

    [Fact]
    public void StringValue_WrittenAndReadBack()
    {
        Run($"""<Action Type="RegWrite" Hive="HKCU" Key="{_subKey}" Value="Str">hello</Action>""");
        Assert.Equal("hello", ReadBack("Str", RegistryValueKind.String));
    }

    [Fact]
    public void DwordValue_WrittenAndReadBack()
    {
        Run($"""<Action Type="RegWrite" Hive="HKCU" Key="{_subKey}" Value="DW" ValueType="REG_DWORD">42</Action>""");
        Assert.Equal(42, ReadBack("DW", RegistryValueKind.DWord));
    }

    [Fact]
    public void QwordValue_WrittenAndReadBack()
    {
        Run($"""<Action Type="RegWrite" Hive="HKCU" Key="{_subKey}" Value="QW" ValueType="REG_QWORD">9999999999</Action>""");
        Assert.Equal(9999999999L, ReadBack("QW", RegistryValueKind.QWord));
    }

    [Fact]
    public void ExpandSzValue_KindIsExpandString()
    {
        // Use a plain literal — ActionRegWrite substitutes %VAR% before writing,
        // so testing TS-variable expansion is separate from testing the registry kind.
        Run($"""<Action Type="RegWrite" Hive="HKCU" Key="{_subKey}" Value="ESZ" ValueType="REG_EXPAND_SZ">some path value</Action>""");
        Assert.Equal(RegistryValueKind.ExpandString, GetValueKind("ESZ"));
        Assert.Equal("some path value", ReadBack("ESZ"));
    }

    [Fact]
    public void VariableSubstitution_InValue()
    {
        var env = new LocalTSEnv();
        env.Set("MyVar", "substituted");
        Run($"""<Action Type="RegWrite" Hive="HKCU" Key="{_subKey}" Value="Sub">%MyVar%</Action>""", env);
        Assert.Equal("substituted", ReadBack("Sub"));
    }

    [Fact]
    public void MissingKeyParam_DoesNotThrow()
    {
        // No Key attr — action should skip gracefully.
        var ex = Record.Exception(() =>
            Run($"""<Action Type="RegWrite" Hive="HKCU" Value="SomeVal">data</Action>"""));
        Assert.Null(ex);
    }

    [Fact]
    public void CreatesSubKeyIfMissing()
    {
        var deepKey = $"{_subKey}\\Deep\\Sub";
        Run($"""<Action Type="RegWrite" Hive="HKCU" Key="{deepKey}" Value="V">created</Action>""");
        using var k = Registry.CurrentUser.OpenSubKey(deepKey);
        Assert.NotNull(k);
    }

    [Fact]
    public void AlwaysReturnsNext()
    {
        var e    = XElement.Parse($"""<Action Type="RegWrite" Hive="HKCU" Key="{_subKey}" Value="V">x</Action>""");
        var data = new ActionData
        {
            ActionNode         = e,
            Conditions         = new NativeConditionEvaluator(),
            TsEnv              = new LocalTSEnv(),
            Log                = NullLog.Instance,
            GlobalDialogTraits = new DialogTraits(),
        };
        Assert.Equal(ActionResult.Next, new ActionRegWrite(data).Go());
    }
}
