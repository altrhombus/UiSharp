using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using GUISharp.Services;

namespace GUISharp.ViewModels;

public sealed partial class CmSelectableItem : ObservableObject
{
    [ObservableProperty] public partial bool IsSelected { get; set; }
    public string Name    { get; init; } = string.Empty;
    public string SubLabel { get; init; } = string.Empty;
    public bool   IsApp   { get; init; }
    public string PkgId   { get; init; } = string.Empty;
}

public sealed partial class ConfigMgrImportViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConnect))]
    public partial string ServerName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConnect))]
    public partial string SiteCode { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConnect))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty] public partial bool   ShowApps        { get; set; } = true;
    [ObservableProperty] public partial string SearchText      { get; set; } = string.Empty;
    [ObservableProperty] public partial string AltUsername     { get; set; } = string.Empty;
    [ObservableProperty] public partial string AltPassword     { get; set; } = string.Empty;
    [ObservableProperty] public partial bool   ShowCredentials { get; set; }

    private readonly IConfigMgrService _service;
    private readonly List<CmSelectableItem> _allApps = [];
    private readonly List<CmSelectableItem> _allPkgs = [];

    public ObservableCollection<CmSelectableItem> DisplayItems { get; } = [];

    public bool IsConnected  => _allApps.Count > 0 || _allPkgs.Count > 0;
    public bool HasError     => !string.IsNullOrEmpty(ErrorMessage);
    public bool CanConnect   => !string.IsNullOrWhiteSpace(ServerName)
                             && !string.IsNullOrWhiteSpace(SiteCode)
                             && !IsLoading;
    public bool HasSelection => _allApps.Any(a => a.IsSelected) || _allPkgs.Any(p => p.IsSelected);

    public string SelectionLabel
    {
        get
        {
            int n = _allApps.Count(a => a.IsSelected) + _allPkgs.Count(p => p.IsSelected);
            return n == 0 ? "No items selected"
                 : n == 1 ? "1 item selected"
                          : $"{n} items selected";
        }
    }

    public ConfigMgrImportViewModel(IConfigMgrService service) => _service = service;

    public async Task ConnectAsync()
    {
        if (!CanConnect) return;
        IsLoading    = true;
        ErrorMessage = string.Empty;
        try
        {
            var server = ServerName.Trim();
            var site   = SiteCode.Trim().ToUpperInvariant();

            NetworkCredential? credential = null;
            if (ShowCredentials && !string.IsNullOrWhiteSpace(AltUsername))
            {
                var idx = AltUsername.IndexOf('\\');
                credential = idx >= 0
                    ? new NetworkCredential(AltUsername[(idx + 1)..], AltPassword, AltUsername[..idx])
                    : new NetworkCredential(AltUsername, AltPassword);
            }

            var apps = await _service.GetApplicationsAsync(server, site, credential);
            var pkgs = await _service.GetPackagesAsync(server, site, credential);

            _allApps.Clear();
            _allPkgs.Clear();

            foreach (var a in apps)
            {
                var item = new CmSelectableItem { Name = a.Name, SubLabel = a.Description, IsApp = true };
                item.PropertyChanged += OnItemPropertyChanged;
                _allApps.Add(item);
            }
            foreach (var p in pkgs)
            {
                var item = new CmSelectableItem { Name = p.Name, SubLabel = p.PackageId, IsApp = false, PkgId = p.PackageId };
                item.PropertyChanged += OnItemPropertyChanged;
                _allPkgs.Add(item);
            }

            OnPropertyChanged(nameof(IsConnected));
            RebuildDisplayItems();
        }
        catch (Exception ex) when (IsAccessDenied(ex))
        {
            ErrorMessage    = "Access denied. Enter alternate credentials below to connect with a different account.";
            ShowCredentials = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = (uint)ex.HResult == 0x800706BA
                ? $"Cannot reach the SMS Provider — verify the server name and network access. ({ex.GetType().Name})"
                : $"Connection failed ({ex.GetType().Name}, 0x{(uint)ex.HResult:X8}): {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnShowAppsChanged(bool value)      => RebuildDisplayItems();
    partial void OnSearchTextChanged(string value)  => RebuildDisplayItems();
    partial void OnServerNameChanged(string value)  => InvalidateConnection();
    partial void OnSiteCodeChanged(string value)    => InvalidateConnection();

    private void InvalidateConnection()
    {
        _allApps.Clear();
        _allPkgs.Clear();
        ErrorMessage    = string.Empty;
        ShowCredentials = false;
        OnPropertyChanged(nameof(IsConnected));
        DisplayItems.Clear();
        NotifySelectionChanged();
    }

    private void RebuildDisplayItems()
    {
        DisplayItems.Clear();
        var source = ShowApps ? _allApps : _allPkgs;
        var filter = SearchText.Trim();
        foreach (var item in source)
        {
            if (filter.Length == 0 || item.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                DisplayItems.Add(item);
        }
        NotifySelectionChanged();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CmSelectableItem.IsSelected))
            NotifySelectionChanged();
    }

    private void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SelectionLabel));
    }

    public IEnumerable<CmSelectableItem> GetSelectedItems() =>
        _allApps.Where(a => a.IsSelected).Concat(_allPkgs.Where(p => p.IsSelected));

    private static bool IsAccessDenied(Exception ex) =>
        (ex is ManagementException me && me.ErrorCode == ManagementStatus.AccessDenied) ||
        ex is UnauthorizedAccessException ||
        (uint)ex.HResult == 0x80070005;
}
