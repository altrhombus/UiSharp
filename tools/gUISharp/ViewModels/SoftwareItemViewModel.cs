using CommunityToolkit.Mvvm.ComponentModel;
using UIpp.Core.Configuration;
using UIpp.Core.Software;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels;

public sealed partial class SoftwareItemViewModel : ObservableObject
{
    [ObservableProperty] public partial string Id          { get; set; }
    [ObservableProperty] public partial string Label       { get; set; }
    [ObservableProperty] public partial string Info        { get; set; }
    [ObservableProperty] public partial string IncludeIds  { get; set; }
    [ObservableProperty] public partial string ExcludeIds  { get; set; }
    [ObservableProperty] public partial bool   IsApplication { get; set; }

    // Application-specific
    [ObservableProperty] public partial string AppName     { get; set; }

    // Package-specific
    [ObservableProperty] public partial string PkgId       { get; set; }
    [ObservableProperty] public partial string ProgramName { get; set; }

    public int OrderIndex { get; set; }

    public SoftwareItemViewModel()
    {
        Id            = string.Empty;
        Label         = string.Empty;
        Info          = string.Empty;
        IncludeIds    = string.Empty;
        ExcludeIds    = string.Empty;
        IsApplication = true;
        AppName       = string.Empty;
        PkgId         = string.Empty;
        ProgramName   = string.Empty;
    }

    public static SoftwareItemViewModel FromSoftware(ISoftware sw)
    {
        var vm = new SoftwareItemViewModel
        {
            Id         = sw.Id,
            Label      = sw.Label,
            Info       = sw.Info,
            IncludeIds = sw.IncludeIds,
            ExcludeIds = sw.ExcludeIds,
            OrderIndex = sw.OrderIndex,
        };

        if (sw is Application app)
        {
            vm.IsApplication = true;
            vm.AppName       = app.AppName;
        }
        else if (sw is Package pkg)
        {
            vm.IsApplication = false;
            vm.PkgId         = pkg.PkgId;
            vm.ProgramName   = pkg.ProgramName;
        }

        return vm;
    }

    public ISoftware ToSoftware() => IsApplication
        ? new Application(Id, Label, Info, AppName, IncludeIds, ExcludeIds, OrderIndex)
        : new Package(Id, Label, Info, PkgId, ProgramName, IncludeIds, ExcludeIds, OrderIndex);
}
