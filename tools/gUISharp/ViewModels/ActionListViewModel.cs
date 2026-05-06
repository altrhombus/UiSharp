using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels;

public sealed partial class ActionListViewModel : ObservableObject
{
    private readonly EditorViewModelFactory _factory;

    public ObservableCollection<ActionNodeViewModel> ActionTree { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private ActionNodeViewModel? _selectedAction;

    public bool HasSelection => SelectedAction is not null;

    public ActionListViewModel(EditorViewModelFactory factory)
    {
        _factory = factory;
    }

    public void LoadActions(IEnumerable<ActionNodeModel> models)
    {
        ActionTree.Clear();
        foreach (var model in models)
            ActionTree.Add(new ActionNodeViewModel(model, _factory));
    }

    public List<ActionNodeModel> CollectModels()
    {
        FlushAll();
        return ActionTree.Select(vm => BuildModel(vm)).ToList();
    }

    [RelayCommand]
    private void AddAction(string typeName)
    {
        var node = new System.Xml.Linq.XElement(C.Elements.Action,
            new System.Xml.Linq.XAttribute(C.Attributes.Type, typeName));
        var model = new ActionNodeModel { Node = node };
        ActionTree.Add(new ActionNodeViewModel(model, _factory));
    }

    [RelayCommand]
    private void AddGroup()
    {
        var node = new System.Xml.Linq.XElement(C.Elements.ActionGroup,
            new System.Xml.Linq.XAttribute(C.Attributes.Name, "New Group"));
        var model = new ActionNodeModel { Node = node };
        ActionTree.Add(new ActionNodeViewModel(model, _factory));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RemoveAction()
    {
        if (SelectedAction is null) return;
        RemoveFromTree(ActionTree, SelectedAction);
        SelectedAction = null;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveUp()
    {
        if (SelectedAction is null) return;
        var list = FindOwningList(ActionTree, SelectedAction) ?? ActionTree;
        var idx = list.IndexOf(SelectedAction);
        if (idx > 0) list.Move(idx, idx - 1);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveDown()
    {
        if (SelectedAction is null) return;
        var list = FindOwningList(ActionTree, SelectedAction) ?? ActionTree;
        var idx = list.IndexOf(SelectedAction);
        if (idx >= 0 && idx < list.Count - 1) list.Move(idx, idx + 1);
    }

    partial void OnSelectedActionChanged(ActionNodeViewModel? value)
    {
        RemoveActionCommand.NotifyCanExecuteChanged();
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    // -------------------------------------------------------------------------

    private void FlushAll()
    {
        foreach (var vm in ActionTree)
            vm.FlushEditsToNode();
    }

    private static ActionNodeModel BuildModel(ActionNodeViewModel vm)
    {
        var model = vm.Model;
        if (vm.IsGroup)
            model.Children.Clear();

        foreach (var child in vm.Children)
        {
            if (vm.IsGroup)
                model.Children.Add(BuildModel(child));
        }
        return model;
    }

    private static bool RemoveFromTree(ObservableCollection<ActionNodeViewModel> list, ActionNodeViewModel target)
    {
        if (list.Remove(target)) return true;
        foreach (var item in list)
            if (RemoveFromTree(item.Children, target)) return true;
        return false;
    }

    private static ObservableCollection<ActionNodeViewModel>? FindOwningList(
        ObservableCollection<ActionNodeViewModel> list, ActionNodeViewModel target)
    {
        if (list.Contains(target)) return list;
        foreach (var item in list)
        {
            var found = FindOwningList(item.Children, target);
            if (found is not null) return found;
        }
        return null;
    }
}
