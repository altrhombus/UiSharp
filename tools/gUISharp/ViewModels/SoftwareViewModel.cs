using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UIpp.Core.Software;

namespace GUISharp.ViewModels;

public sealed partial class SoftwareViewModel : ObservableObject
{
    public ObservableCollection<SoftwareItemViewModel> Items { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    public partial SoftwareItemViewModel? SelectedItem { get; set; }

    public bool HasSelection => SelectedItem is not null;

    public void LoadFrom(IEnumerable<ISoftware> software)
    {
        Items.Clear();
        foreach (var sw in software)
            Items.Add(SoftwareItemViewModel.FromSoftware(sw));
    }

    public List<ISoftware> CollectSoftware()
    {
        var result = new List<ISoftware>();
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].OrderIndex = i;
            result.Add(Items[i].ToSoftware());
        }
        return result;
    }

    [RelayCommand]
    private void AddApplication()
    {
        var vm = new SoftwareItemViewModel
        {
            IsApplication = true,
            Id            = Guid.NewGuid().ToString("D").ToUpper(),
            Label         = "New Application",
            OrderIndex    = Items.Count,
        };
        Items.Add(vm);
        SelectedItem = vm;
    }

    [RelayCommand]
    private void AddPackage()
    {
        var vm = new SoftwareItemViewModel
        {
            IsApplication = false,
            Id            = Guid.NewGuid().ToString("D").ToUpper(),
            Label         = "New Package",
            OrderIndex    = Items.Count,
        };
        Items.Add(vm);
        SelectedItem = vm;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RemoveItem()
    {
        if (SelectedItem is null) return;
        Items.Remove(SelectedItem);
        SelectedItem = null;
    }

    partial void OnSelectedItemChanged(SoftwareItemViewModel? value) =>
        RemoveItemCommand.NotifyCanExecuteChanged();
}
