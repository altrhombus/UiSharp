using System.Security.Cryptography;
using UIpp.Core.Configuration;

namespace UIpp.Core.Actions.Impl;

[ActionType(XmlConstants.ActionTypes.RandomString)]
public sealed class ActionRandomString(ActionData data) : ActionBase(data)
{
    public override ActionResult Go()
    {
        var chars    = Attr(XmlConstants.Attributes.AllowedChars, XmlConstants.Defaults.AllowedChars);
        var variable = Attr(XmlConstants.Attributes.Variable, XmlConstants.Defaults.Variable);

        if (!int.TryParse(Attr(XmlConstants.Attributes.Length,
                XmlConstants.Defaults.Length.ToString()), out var len)
            || len < 1 || len > 36)
        {
            len = XmlConstants.Defaults.Length;
        }

        if (chars.Length == 0) return ActionResult.Next;

        var buf = new char[len];
        for (var i = 0; i < len; i++)
            buf[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];

        Data.TsEnv.Set(variable, new string(buf));
        return ActionResult.Next;
    }
}
