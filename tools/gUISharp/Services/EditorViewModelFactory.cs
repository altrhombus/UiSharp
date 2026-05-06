using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.ViewModels.ActionEditors;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.Services;

public sealed class EditorViewModelFactory
{
    private readonly Dictionary<string, Func<ActionNodeModel, ObservableObject>> _map;

    public EditorViewModelFactory()
    {
        _map = new(StringComparer.OrdinalIgnoreCase)
        {
            [C.ActionTypes.TSVar]         = m => new TSVarViewModel(m),
            [C.ActionTypes.ExternalCall]  = m => new ExternalCallViewModel(m),
            [C.ActionTypes.DefaultValues] = m => new DefaultValuesViewModel(m),
            [C.ActionTypes.RandomString]  = m => new RandomStringViewModel(m),
            [C.ActionTypes.FileRead]      = m => new FileReadViewModel(m),
            [C.ActionTypes.Vars]          = m => new VarsViewModel(m),
            [C.ActionTypes.FromJson]      = m => new FromJsonViewModel(m),
            [C.ActionTypes.Rest]          = m => new RestViewModel(m),
            [C.ActionTypes.SaveItems]     = m => new SaveItemsViewModel(m),
            [C.ActionTypes.ToJson]        = m => new ToJsonViewModel(m),
            [C.ActionTypes.TSVarList]     = m => new TSVarListViewModel(m),
            [C.ActionTypes.Preflight]     = m => new PreflightViewModel(m),
            [C.ActionTypes.UserInput]     = m => new InputActionViewModel(m),
        };
    }

    public ObservableObject Create(ActionNodeModel model)
    {
        if (model.IsGroup)
            return new ActionGroupViewModel(model);

        return _map.TryGetValue(model.TypeName, out var factory)
            ? factory(model)
            : new RawXmlViewModel(model);
    }
}
