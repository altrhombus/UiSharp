using CommunityToolkit.Mvvm.ComponentModel;
using UIpp.Core.Configuration;
using UIpp.Core.Software;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels;

public sealed partial class SoftwareItemViewModel : ObservableObject
{
    [ObservableProperty] private string _id          = string.Empty;
    [ObservableProperty] private string _label       = string.Empty;
    [ObservableProperty] private string _info        = string.Empty;
    [ObservableProperty] private string _includeIds  = string.Empty;
    [ObservableProperty] private string _excludeIds  = string.Empty;
    [ObservableProperty] private bool   _isApplication = true;

    // Application-specific
    [ObservableProperty] private string _appName     = string.Empty;

    // Package-specific
    [ObservableProperty] private string _pkgId       = string.Empty;
    [ObservableProperty] private string _programName = string.Empty;

    public int OrderIndex { get; set; }

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
