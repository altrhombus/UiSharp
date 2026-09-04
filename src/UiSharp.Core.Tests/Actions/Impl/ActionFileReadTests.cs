using UiSharp.Core.Actions;
using UiSharp.Core.Actions.Impl;

namespace UiSharp.Core.Tests.Actions.Impl;

public class ActionFileReadTests
{
    [Fact]
    public void ReadsFirstNonBlankLine()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(path, ["", "  ", "Hello", "World"]);
            var el = ActionTestData.ActionEl(
                $"""<Action Type="FileRead" Filename="{path}" DeleteLine="False" Variable="V" />""");
            var (env, _, data) = ActionTestData.Make(el);
            new ActionFileRead(data).Go();
            Assert.Equal("Hello", env.Get("V"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void DeleteLine_RemovesReadLine()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(path, ["First", "Second", "Third"]);
            var el = ActionTestData.ActionEl(
                $"""<Action Type="FileRead" Filename="{path}" DeleteLine="True" Variable="V" />""");
            var (env, _, data) = ActionTestData.Make(el);
            new ActionFileRead(data).Go();

            Assert.Equal("First", env.Get("V"));
            var remaining = File.ReadAllLines(path);
            Assert.Equal(["Second", "Third"], remaining);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void EmptyFile_SetsNothingAndReturnsNext()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "");
            var el = ActionTestData.ActionEl(
                $"""<Action Type="FileRead" Filename="{path}" Variable="V" />""");
            var (env, _, data) = ActionTestData.Make(el);
            var result = new ActionFileRead(data).Go();
            Assert.Equal(ActionResult.Next, result);
            Assert.Equal(string.Empty, env.Get("V"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void MissingFile_ReturnsNextWithoutThrowing()
    {
        var el = ActionTestData.ActionEl(
            """<Action Type="FileRead" Filename="/no/such/file.txt" Variable="V" />""");
        var (_, log, data) = ActionTestData.Make(el);
        var result = new ActionFileRead(data).Go();
        Assert.Equal(ActionResult.Next, result);
        Assert.Single(log.Messages);   // error was logged
    }
}
