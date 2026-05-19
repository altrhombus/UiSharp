using System.Xml.Linq;
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

    [ObservableProperty] public partial bool IsDirty { get; set; }

    public int OrderIndex { get; set; }

    private string? _comment;
    public string? Comment
    {
        get => _comment;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? null
                           : value.Replace("\r\n", "\n").Replace("\r", "\n");
            if (_comment == normalized) return;
            _comment = normalized;
            OnPropertyChanged();
        }
    }

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

    public XElement ToXElement()
    {
        var elementName = IsApplication ? C.Elements.Application : C.Elements.Package;
        var el = new XElement(elementName,
            new XAttribute(C.Attributes.Id, Id),
            new XAttribute(C.Attributes.Label, Label));
        if (!string.IsNullOrEmpty(Info))       el.Add(new XAttribute(C.Attributes.SoftwareInfo, Info));
        if (IsApplication)
        {
            if (!string.IsNullOrEmpty(AppName)) el.Add(new XAttribute(C.Attributes.AppName, AppName));
        }
        else
        {
            if (!string.IsNullOrEmpty(PkgId))       el.Add(new XAttribute(C.Attributes.PkgId, PkgId));
            if (!string.IsNullOrEmpty(ProgramName)) el.Add(new XAttribute(C.Attributes.ProgramName, ProgramName));
        }
        if (!string.IsNullOrEmpty(IncludeIds)) el.Add(new XAttribute(C.Attributes.IncludeId, IncludeIds));
        if (!string.IsNullOrEmpty(ExcludeIds)) el.Add(new XAttribute(C.Attributes.ExcludeId, ExcludeIds));
        return el;
    }
}
