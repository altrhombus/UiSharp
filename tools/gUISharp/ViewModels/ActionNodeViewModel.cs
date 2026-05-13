using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels;

public sealed partial class ActionNodeViewModel : ObservableObject
{
    private readonly EditorViewModelFactory _factory;

    public ActionNodeModel Model { get; }

    public string TypeName => Model.TypeName;

    public bool IsGroup => Model.IsGroup;

    public string DisplayLabel => BuildDisplayLabel();

    public ObservableCollection<ActionNodeViewModel> Children { get; } = [];

    [ObservableProperty]
    public partial ObservableObject? EditorViewModel { get; set; }

    public event EventHandler? Dirtied;

    public ActionNodeViewModel(ActionNodeModel model, EditorViewModelFactory factory)
    {
        _factory = factory;
        Model = model;

        foreach (var child in model.Children)
        {
            var childVm = new ActionNodeViewModel(child, factory);
            childVm.Dirtied += (_, _) => Dirtied?.Invoke(this, EventArgs.Empty);
            Children.Add(childVm);
        }

        EditorViewModel = factory.Create(model);
        if (EditorViewModel is not null)
            EditorViewModel.PropertyChanged += (_, _) => Dirtied?.Invoke(this, EventArgs.Empty);
    }

    public void RefreshEditorViewModel()
    {
        var vm = _factory.Create(Model);
        if (EditorViewModel is IActionEditor old && vm is IActionEditor next)
            next.CopyUiStateFrom(old);
        vm.PropertyChanged += (_, _) => Dirtied?.Invoke(this, EventArgs.Empty);
        EditorViewModel = vm;
    }

    public void FlushEditsToNode()
    {
        if (EditorViewModel is IActionEditor editor)
            editor.FlushToNode();

        foreach (var child in Children)
            child.FlushEditsToNode();
    }

    private string BuildDisplayLabel()
    {
        if (IsGroup)
        {
            var name = Attr(C.Attributes.Name);
            return string.IsNullOrEmpty(name) ? "[Group]" : $"[Group] {name}";
        }

        return TypeName switch
        {
            C.ActionTypes.TSVar        => $"TSVar: {Attr(C.Attributes.Variable) ?? Attr(C.Attributes.Name) ?? "?"}",
            C.ActionTypes.ExternalCall => $"ExternalCall: {Model.Node.Value.Trim().Split('\n')[0].Trim()}",
            C.ActionTypes.DefaultValues => $"DefaultValues: {Attr(C.Attributes.DefaultValueTypes) ?? "All"}",
            C.ActionTypes.RandomString => $"RandomString → {Attr(C.Attributes.Variable) ?? "?"}",
            C.ActionTypes.FileRead     => $"FileRead: {Attr(C.Attributes.Filename) ?? "?"}",
            C.ActionTypes.Vars         => $"Vars ({Attr(C.Attributes.Direction) ?? "?"})",
            C.ActionTypes.FromJson     => $"FromJSON → {Attr(C.Attributes.Variable) ?? "?"}",
            C.ActionTypes.Rest         => $"REST: {Attr(C.Attributes.Url) ?? "?"}",
            C.ActionTypes.SaveItems    => $"SaveItems → {Attr(C.Attributes.Path) ?? "?"}",
            C.ActionTypes.ToJson       => $"ToJSON → {Attr(C.Attributes.Variable) ?? "?"}",
            C.ActionTypes.TSVarList    => "TSVarList",
            C.ActionTypes.Preflight    => $"Preflight: {Attr(C.Attributes.Title) ?? "Preflight"}",
            C.ActionTypes.UserInput    => $"Input: {Attr(C.Attributes.Title) ?? "User Input"}",
            C.ActionTypes.UserInfo     => $"Info: {Attr(C.Attributes.Title) ?? "?"}",
            C.ActionTypes.UserInfoFull => $"InfoFullScreen: {Attr(C.Attributes.Title) ?? "?"}",
            C.ActionTypes.ErrorInfo    => $"ErrorInfo: {Attr(C.Attributes.Title) ?? "?"}",
            C.ActionTypes.RegRead      => $"RegRead → {Attr(C.Attributes.Variable) ?? "?"}",
            C.ActionTypes.RegWrite     => $"RegWrite: {Attr(C.Attributes.Key) ?? "?"}",
            C.ActionTypes.AppTree      => $"AppTree: {Attr(C.Attributes.Title) ?? "?"}",
            C.ActionTypes.WmiRead      => $"WMIRead → {Attr(C.Attributes.Variable) ?? "?"}",
            C.ActionTypes.WmiWrite     => $"WMIWrite: {Attr(C.Attributes.Class) ?? "?"}",
            C.ActionTypes.UserAuth     => $"UserAuth: {Attr(C.Attributes.Title) ?? "?"}",
            C.ActionTypes.SoftwareDisc => "SoftwareDiscovery",
            C.ActionTypes.Switch       => $"Switch: {Attr(C.Attributes.OnValue) ?? "?"}",
            C.ActionTypes.Tpm          => "TPM",
            _ => string.IsNullOrEmpty(Attr(C.Attributes.Name))
                    ? TypeName
                    : $"{TypeName}: {Attr(C.Attributes.Name)}"
        };
    }

    private string? Attr(string name) => (string?)Model.Node.Attribute(name);
}
