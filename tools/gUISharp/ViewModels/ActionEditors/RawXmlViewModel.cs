using CommunityToolkit.Mvvm.ComponentModel;
using UiSharp.Editor.Services;
using UiSharp.Core.Configuration;

namespace UiSharp.Editor.ViewModels.ActionEditors;

public sealed class RawXmlViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    public string RawXml => _model.Node.ToString();

    public RawXmlViewModel(ActionNodeModel model)
    {
        _model = model;
    }

    public void FlushToNode()
    {
        // Read-only — no edits to flush.
    }
}
