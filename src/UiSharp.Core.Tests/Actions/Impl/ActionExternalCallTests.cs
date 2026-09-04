using UiSharp.Core.Actions;
using UiSharp.Core.Actions.Impl;

namespace UiSharp.Core.Tests.Actions.Impl;

public class ActionExternalCallTests
{
    private static string TrueCommand =>
        OperatingSystem.IsWindows() ? "exit 0" : "true";

    private static string ExitCodeCommand(int code) =>
        OperatingSystem.IsWindows() ? $"exit {code}" : $"exit {code}";

    [Fact]
    public void SuccessfulProcess_SetsExitCodeVariable()
    {
        var el = ActionTestData.ActionEl(
            $"""<Action Type="ExternalCall" ExitCodeVariable="RC" MaxRunTime="10">{ExitCodeCommand(0)}</Action>""");
        var (env, _, data) = ActionTestData.Make(el);
        new ActionExternalCall(data).Go();
        Assert.Equal("0", env.Get("RC"));
    }

    [Fact]
    public void NonZeroExitCode_CapturedCorrectly()
    {
        var el = ActionTestData.ActionEl(
            $"""<Action Type="ExternalCall" ExitCodeVariable="RC" MaxRunTime="10">{ExitCodeCommand(42)}</Action>""");
        var (env, _, data) = ActionTestData.Make(el);
        new ActionExternalCall(data).Go();
        Assert.Equal("42", env.Get("RC"));
    }

    [Fact]
    public void NoExitCodeVariable_DoesNotThrow()
    {
        var el = ActionTestData.ActionEl(
            $"""<Action Type="ExternalCall" MaxRunTime="10">{TrueCommand}</Action>""");
        var (_, _, data) = ActionTestData.Make(el);
        var ex = Record.Exception(() => new ActionExternalCall(data).Go());
        Assert.Null(ex);
    }

    [Fact]
    public void EmptyCommandLine_ReturnsNextWithoutStarting()
    {
        var el = ActionTestData.ActionEl("""<Action Type="ExternalCall" ExitCodeVariable="RC" />""");
        var (env, _, data) = ActionTestData.Make(el);
        new ActionExternalCall(data).Go();
        Assert.Equal(string.Empty, env.Get("RC")); // variable never set
    }

    [Fact]
    public void Returns_Next()
    {
        var el = ActionTestData.ActionEl(
            $"""<Action Type="ExternalCall">{TrueCommand}</Action>""");
        var (_, _, data) = ActionTestData.Make(el);
        Assert.Equal(ActionResult.Next, new ActionExternalCall(data).Go());
    }
}
