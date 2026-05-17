using Microsoft.Win32;
using System.Xml.Linq;
using UIpp.Core.Actions;
using UIpp.Core.Dialogs;
using UIpp.Core.Logging;
using UIpp.Core.Scripting;
using UIpp.Core.Variables;
using UIpp.Windows.Actions;
using UIpp.Windows.Tests;

namespace UIpp.Windows.Tests.Actions;

// Exercises ActionRegRead against a temporary HKCU test key that is created
// during setup and deleted during teardown.  Safe to run on any Windows machine.
public sealed class ActionRegReadTests : IDisposable
{
    private const string TestRoot = @"SOFTWARE\UiSharpTests";
    private readonly string _subKey;
    private readonly RegistryKey _key;
    private readonly LocalTSEnv _env = new();

    public ActionRegReadTests()
    {
        _subKey = $"{TestRoot}\\RegRead_{Guid.NewGuid():N}";
        _key    = Registry.CurrentUser.CreateSubKey(_subKey, writable: true);
        _key.SetValue("StringVal",  "hello");
        _key.SetValue("DwordVal",   42,       RegistryValueKind.DWord);
        _key.SetValue("EmptyVal",   string.Empty);
    }

    public void Dispose()
    {
        _key.Dispose();
        Registry.CurrentUser.DeleteSubKeyTree(_subKey, throwOnMissingSubKey: false);
    }

    private ActionResult Run(string xml)
    {
        var el   = XElement.Parse(xml);
        var data = new ActionData
        {
            ActionNode           = el,
            Conditions           = new NativeConditionEvaluator(),
            TsEnv                = _env,
            Log                  = NullLog.Instance,
            GlobalDialogTraits   = new DialogTraits(),
        };
        return new ActionRegRead(data).Go();
    }

    [Fact]
    public void StringValue_ReadCorrectly()
    {
        Run($"""<Action Type="RegRead" Hive="HKCU" Key="{_subKey}" Value="StringVal" Variable="Out" />""");
        Assert.Equal("hello", _env.Get("Out"));
    }

    [Fact]
    public void DwordValue_ReadAsString()
    {
        Run($"""<Action Type="RegRead" Hive="HKCU" Key="{_subKey}" Value="DwordVal" Variable="Out" />""");
        Assert.Equal("42", _env.Get("Out"));
    }

    [Fact]
    public void MissingValue_SetsEmpty()
    {
        Run($"""<Action Type="RegRead" Hive="HKCU" Key="{_subKey}" Value="NoSuchValue" Variable="Out" />""");
        Assert.Equal(string.Empty, _env.Get("Out"));
    }

    [Fact]
    public void MissingValue_UsesDefault()
    {
        Run($"""<Action Type="RegRead" Hive="HKCU" Key="{_subKey}" Value="NoSuchValue" Variable="Out" Default="fallback" />""");
        Assert.Equal("fallback", _env.Get("Out"));
    }

    [Fact]
    public void EmptyValue_UsesDefault()
    {
        Run($"""<Action Type="RegRead" Hive="HKCU" Key="{_subKey}" Value="EmptyVal" Variable="Out" Default="fallback" />""");
        Assert.Equal("fallback", _env.Get("Out"));
    }

    [Fact]
    public void MissingKey_UsesDefault()
    {
        Run($"""<Action Type="RegRead" Hive="HKCU" Key="{_subKey}\NoSuchKey" Value="StringVal" Variable="Out" Default="fallback" />""");
        Assert.Equal("fallback", _env.Get("Out"));
    }

    [Fact]
    public void MissingRequiredParam_ReturnsNextWithoutSetting()
    {
        // No Variable attr — should skip without throwing.
        var result = Run($"""<Action Type="RegRead" Hive="HKCU" Key="{_subKey}" Value="StringVal" />""");
        Assert.Equal(ActionResult.Next, result);
    }

    [Fact]
    public void AlwaysReturnsNext()
    {
        var result = Run($"""<Action Type="RegRead" Hive="HKCU" Key="{_subKey}" Value="StringVal" Variable="Out" />""");
        Assert.Equal(ActionResult.Next, result);
    }
}

