using UIpp.Core.Configuration;
using UIpp.Core.Logging;

namespace UIpp.Core.Actions.Impl;

[ActionType(XmlConstants.ActionTypes.FileRead)]
public sealed class ActionFileRead(ActionData data) : ActionBase(data)
{
    public override ActionResult Go()
    {
        var filename   = Data.TsEnv.Substitute(Attr(XmlConstants.Attributes.Filename));
        var deleteLine = BoolAttr(XmlConstants.Attributes.DeleteLine, def: true);
        var variable   = Attr(XmlConstants.Attributes.Variable, XmlConstants.Defaults.Variable);

        if (string.IsNullOrEmpty(filename)) return ActionResult.Next;

        try
        {
            var lines = File.ReadAllLines(filename).ToList();
            var idx   = lines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
            if (idx < 0) return ActionResult.Next;

            var value = lines[idx].Trim();

            if (deleteLine)
            {
                lines.RemoveAt(idx);
                File.WriteAllLines(filename, lines);
            }

            if (value.Length > 0)
                Data.TsEnv.Set(variable, value);
        }
        catch (Exception ex)
        {
            Data.Log.Write($"FileRead: error reading '{filename}': {ex.Message}", LogSeverity.Error);
        }

        return ActionResult.Next;
    }
}
