using GUISharp.ViewModels;
using GUISharp.ViewModels.ActionEditors;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UIpp.Core.Configuration;
using Windows.Foundation;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.Views;

public sealed partial class ActionListPage : Page
{
    public MainWindowViewModel ViewModel => App.MainVm;

    public ActionListPage()
    {
        this.InitializeComponent();
    }

    // ── Focus-based sync ──────────────────────────────────────────────────────

    private void GuidedPanel_GotFocus(object sender, RoutedEventArgs e)
    {
        // SyncXmlToGuided is self-gating: no-op unless the XML was edited since the last sync.
        ViewModel.ActionList.SyncXmlToGuided();
    }

    private void XmlPanel_GotFocus(object sender, RoutedEventArgs e)
    {
        ViewModel.ActionList.SyncGuidedToXml();
    }

    // ── Splitter drag ─────────────────────────────────────────────────────────

    private bool _dragging;
    private double _dragStartX;
    private double _dragStartGuidedWidth;
    private double _dragStartXmlWidth;

    private void DragSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        => this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);

    private void DragSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging)
            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    }

    private void DragSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _dragging = true;
        _dragStartX = e.GetCurrentPoint(this).Position.X;
        _dragStartGuidedWidth = GuidedCol.ActualWidth;
        _dragStartXmlWidth = XmlCol.ActualWidth;
        ((UIElement)sender).CapturePointer(e.Pointer);
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }

    private void DragSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging) return;
        var delta = e.GetCurrentPoint(this).Position.X - _dragStartX;
        var newGuided = Math.Max(60, _dragStartGuidedWidth + delta);
        var newXml    = Math.Max(60, _dragStartXmlWidth - delta);
        GuidedCol.Width = new GridLength(newGuided, GridUnitType.Star);
        XmlCol.Width    = new GridLength(newXml, GridUnitType.Star);
    }

    private void DragSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _dragging = false;
        ((UIElement)sender).ReleasePointerCapture(e.Pointer);
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    }

    // ── Collapse / expand ─────────────────────────────────────────────────────

    private double _savedGuidedWidth = double.NaN;
    private double _savedXmlWidth    = double.NaN;
    private bool   _guidedCollapsed;
    private bool   _xmlCollapsed;

    private void CollapseGuidedBtn_Click(object sender, RoutedEventArgs e)
    {
        _savedGuidedWidth = GuidedCol.ActualWidth > 0 ? GuidedCol.ActualWidth : 400;
        _savedXmlWidth    = XmlCol.ActualWidth    > 0 ? XmlCol.ActualWidth    : 440;
        GuidedCol.MinWidth = 0;
        GuidedCol.Width    = new GridLength(0);
        SplitterCol.Width  = new GridLength(0);
        _guidedCollapsed   = true;
        CollapseGuidedBtn.Visibility = Visibility.Collapsed;
        ExpandGuidedBtn.Visibility   = Visibility.Visible;
        if (_xmlCollapsed) ExpandXmlInternal();
    }

    private void ExpandGuidedBtn_Click(object sender, RoutedEventArgs e) => ExpandGuidedInternal();

    private void ExpandGuidedInternal()
    {
        GuidedCol.MinWidth = 60;
        GuidedCol.Width    = new GridLength(double.IsNaN(_savedGuidedWidth) ? 400 : _savedGuidedWidth, GridUnitType.Star);
        XmlCol.Width       = new GridLength(double.IsNaN(_savedXmlWidth)    ? 440 : _savedXmlWidth,    GridUnitType.Star);
        SplitterCol.Width  = new GridLength(5);
        _guidedCollapsed   = false;
        CollapseGuidedBtn.Visibility = Visibility.Visible;
        ExpandGuidedBtn.Visibility   = Visibility.Collapsed;
    }

    private void CollapseXmlBtn_Click(object sender, RoutedEventArgs e)
    {
        _savedXmlWidth    = XmlCol.ActualWidth    > 0 ? XmlCol.ActualWidth    : 440;
        _savedGuidedWidth = GuidedCol.ActualWidth > 0 ? GuidedCol.ActualWidth : 400;
        XmlCol.MinWidth   = 0;
        XmlCol.Width      = new GridLength(0);
        SplitterCol.Width = new GridLength(0);
        _xmlCollapsed     = true;
        CollapseXmlBtn.Visibility = Visibility.Collapsed;
        ExpandXmlBtn.Visibility   = Visibility.Visible;
        if (_guidedCollapsed) ExpandGuidedInternal();
    }

    private void ExpandXmlBtn_Click(object sender, RoutedEventArgs e) => ExpandXmlInternal();

    private void ExpandXmlInternal()
    {
        XmlCol.MinWidth  = 60;
        XmlCol.Width     = new GridLength(double.IsNaN(_savedXmlWidth)    ? 440 : _savedXmlWidth,    GridUnitType.Star);
        GuidedCol.Width  = new GridLength(double.IsNaN(_savedGuidedWidth) ? 400 : _savedGuidedWidth, GridUnitType.Star);
        SplitterCol.Width = new GridLength(5);
        _xmlCollapsed    = false;
        CollapseXmlBtn.Visibility = Visibility.Visible;
        ExpandXmlBtn.Visibility   = Visibility.Collapsed;
    }
}

public sealed class ActionEditorTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TSVarTemplate         { get; set; }
    public DataTemplate? ExternalCallTemplate  { get; set; }
    public DataTemplate? DefaultValuesTemplate { get; set; }
    public DataTemplate? RandomStringTemplate  { get; set; }
    public DataTemplate? FileReadTemplate      { get; set; }
    public DataTemplate? VarsTemplate          { get; set; }
    public DataTemplate? FromJsonTemplate      { get; set; }
    public DataTemplate? RestTemplate          { get; set; }
    public DataTemplate? SaveItemsTemplate     { get; set; }
    public DataTemplate? ToJsonTemplate        { get; set; }
    public DataTemplate? TSVarListTemplate     { get; set; }
    public DataTemplate? PreflightTemplate     { get; set; }
    public DataTemplate? InputTemplate         { get; set; }
    public DataTemplate? ActionGroupTemplate   { get; set; }
    public DataTemplate? InfoTemplate          { get; set; }
    public DataTemplate? InfoFullScreenTemplate { get; set; }
    public DataTemplate? ErrorInfoTemplate     { get; set; }
    public DataTemplate? RegReadTemplate       { get; set; }
    public DataTemplate? RegWriteTemplate      { get; set; }
    public DataTemplate? AppTreeTemplate       { get; set; }
    public DataTemplate? WmiReadTemplate       { get; set; }
    public DataTemplate? WmiWriteTemplate      { get; set; }
    public DataTemplate? UserAuthTemplate      { get; set; }
    public DataTemplate? SoftwareDiscTemplate  { get; set; }
    public DataTemplate? SwitchTemplate        { get; set; }
    public DataTemplate? TpmTemplate           { get; set; }
    public DataTemplate? FallbackTemplate      { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is not ActionNodeViewModel vm) return FallbackTemplate!;

        if (vm.IsGroup) return ActionGroupTemplate ?? FallbackTemplate!;

        return vm.TypeName switch
        {
            C.ActionTypes.TSVar         => TSVarTemplate          ?? FallbackTemplate!,
            C.ActionTypes.ExternalCall  => ExternalCallTemplate   ?? FallbackTemplate!,
            C.ActionTypes.DefaultValues => DefaultValuesTemplate  ?? FallbackTemplate!,
            C.ActionTypes.RandomString  => RandomStringTemplate   ?? FallbackTemplate!,
            C.ActionTypes.FileRead      => FileReadTemplate       ?? FallbackTemplate!,
            C.ActionTypes.Vars          => VarsTemplate           ?? FallbackTemplate!,
            C.ActionTypes.FromJson      => FromJsonTemplate       ?? FallbackTemplate!,
            C.ActionTypes.Rest          => RestTemplate           ?? FallbackTemplate!,
            C.ActionTypes.SaveItems     => SaveItemsTemplate      ?? FallbackTemplate!,
            C.ActionTypes.ToJson        => ToJsonTemplate         ?? FallbackTemplate!,
            C.ActionTypes.TSVarList     => TSVarListTemplate      ?? FallbackTemplate!,
            C.ActionTypes.Preflight     => PreflightTemplate      ?? FallbackTemplate!,
            C.ActionTypes.UserInput     => InputTemplate          ?? FallbackTemplate!,
            C.ActionTypes.UserInfo      => InfoTemplate           ?? FallbackTemplate!,
            C.ActionTypes.UserInfoFull  => InfoFullScreenTemplate ?? FallbackTemplate!,
            C.ActionTypes.ErrorInfo     => ErrorInfoTemplate      ?? FallbackTemplate!,
            C.ActionTypes.RegRead       => RegReadTemplate        ?? FallbackTemplate!,
            C.ActionTypes.RegWrite      => RegWriteTemplate       ?? FallbackTemplate!,
            C.ActionTypes.AppTree       => AppTreeTemplate        ?? FallbackTemplate!,
            C.ActionTypes.WmiRead       => WmiReadTemplate        ?? FallbackTemplate!,
            C.ActionTypes.WmiWrite      => WmiWriteTemplate       ?? FallbackTemplate!,
            C.ActionTypes.UserAuth      => UserAuthTemplate       ?? FallbackTemplate!,
            C.ActionTypes.SoftwareDisc  => SoftwareDiscTemplate   ?? FallbackTemplate!,
            C.ActionTypes.Switch        => SwitchTemplate         ?? FallbackTemplate!,
            C.ActionTypes.Tpm           => TpmTemplate            ?? FallbackTemplate!,
            _                           => FallbackTemplate!,
        };
    }
}
