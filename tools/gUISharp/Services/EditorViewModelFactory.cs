using CommunityToolkit.Mvvm.ComponentModel;
using UiSharp.Editor.ViewModels.ActionEditors;
using UiSharp.Core.Configuration;
using C = UiSharp.Core.Configuration.XmlConstants;
using UiSharp.Editing;

namespace UiSharp.Editor.Services;

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
            [C.ActionTypes.UserInfo]      = m => new InfoViewModel(m),
            [C.ActionTypes.UserInfoFull]  = m => new InfoFullScreenViewModel(m),
            [C.ActionTypes.ErrorInfo]     = m => new ErrorInfoViewModel(m),
            [C.ActionTypes.RegRead]       = m => new RegReadViewModel(m),
            [C.ActionTypes.RegWrite]      = m => new RegWriteViewModel(m),
            [C.ActionTypes.AppTree]       = m => new AppTreeViewModel(m),
            [C.ActionTypes.WmiRead]       = m => new WmiReadViewModel(m),
            [C.ActionTypes.WmiWrite]      = m => new WmiWriteViewModel(m),
            [C.ActionTypes.UserAuth]      = m => new UserAuthViewModel(m),
            [C.ActionTypes.SoftwareDisc]  = m => new SoftwareDiscViewModel(m),
            [C.ActionTypes.Switch]        = m => new SwitchViewModel(m),
            [C.ActionTypes.Tpm]           = m => new TpmViewModel(m),
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
