using System.Text;
using UIpp.Core.Configuration;
using UIpp.Core.Logging;

namespace UIpp.Core.Actions.Impl;

[ActionType(XmlConstants.ActionTypes.Rest)]
public sealed class ActionRest(ActionData data) : ActionBase(data)
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public override ActionResult Go()
    {
        var url      = Data.TsEnv.Substitute(Attr(XmlConstants.Attributes.Url));
        var variable = Attr(XmlConstants.Attributes.Variable, XmlConstants.Defaults.RestVariable);
        var json     = Data.TsEnv.Substitute(Attr(XmlConstants.Attributes.Json));

        if (string.IsNullOrWhiteSpace(url)) return ActionResult.Next;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            using var response = Http.Send(request);
            using var reader   = new System.IO.StreamReader(response.Content.ReadAsStream());
            var body           = reader.ReadToEnd();

            Data.TsEnv.Set(variable, body);
            Data.Log.Write($"REST: POST {url} → HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            Data.Log.Write($"REST: {ex.Message}", LogSeverity.Error);
        }

        return ActionResult.Next;
    }
}
