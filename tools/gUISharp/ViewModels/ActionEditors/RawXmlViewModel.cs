using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;

namespace GUISharp.ViewModels.ActionEditors;

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
