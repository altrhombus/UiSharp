using UIpp.Core.Actions;

namespace UIpp.UI.Actions;

internal static class DialogHelpers
{
    internal static ActionResult MapTimeoutAction(string? act) => act?.ToLowerInvariant() switch
    {
        "cancel" => ActionResult.Cancel,
        "back"   => ActionResult.Back,
        _        => ActionResult.Next,
    };
}
