namespace UiSharp.Editor.Services;

public interface IActionEditor
{
    void FlushToNode();

    /// <summary>Copy any UI-only transient state (e.g. expanded rows) from the previous
    /// version of this editor so a refresh doesn't reset the user's view.
    /// Default implementation is a no-op.</summary>
    void CopyUiStateFrom(IActionEditor previous) { }
}
