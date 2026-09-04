using UiSharp.Editor.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace UiSharp.Editor.Views;

public sealed partial class SoftwarePage : Page
{
    public MainWindowViewModel ViewModel => App.MainVm;

    private static double _savedGuidedColWidth = double.NaN;
    private static double _savedXmlColWidth    = double.NaN;

    public SoftwarePage()
    {
        this.InitializeComponent();
        Loaded += SoftwarePage_Loaded;
    }

    private void SoftwarePage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!double.IsNaN(_savedListColWidth))   ListCol.Width   = new GridLength(_savedListColWidth);
        if (!double.IsNaN(_savedGuidedColWidth)) GuidedCol.Width = new GridLength(_savedGuidedColWidth, GridUnitType.Star);
        if (!double.IsNaN(_savedXmlColWidth))    XmlCol.Width    = new GridLength(_savedXmlColWidth,    GridUnitType.Star);
        if (double.IsNaN(_savedGuidedColWidth) && double.IsNaN(_savedXmlColWidth))
            ApplyDefaultLayout(App.UserSettings.Settings.DefaultPanelLayout);
    }

    private void ApplyDefaultLayout(string layout)
    {
        if (layout == "GuidedOnly")
        {
            XmlCol.MinWidth   = 0;
            XmlCol.Width      = new GridLength(0);
            SplitterCol.Width = new GridLength(0);
            _xmlCollapsed     = true;
            CollapseXmlBtn.Visibility = Visibility.Collapsed;
            ExpandXmlBtn.Visibility   = Visibility.Visible;
        }
        else if (layout == "XmlOnly")
        {
            GuidedCol.MinWidth = 0;
            GuidedCol.Width    = new GridLength(0);
            SplitterCol.Width  = new GridLength(0);
            _guidedCollapsed   = true;
            CollapseGuidedBtn.Visibility = Visibility.Collapsed;
            ExpandGuidedBtn.Visibility   = Visibility.Visible;
        }
    }

    private async void ImportFromConfigMgr_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ConfigMgrImportDialog { XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ViewModel.Software.ImportItems(dialog.GetSelectedItems());
            ViewModel.MarkModified();
        }
    }

    private async void GenerateId_Click(object sender, RoutedEventArgs e)
    {
        var item = ViewModel.Software.SelectedItem;
        if (item is null) return;

        var oldId  = item.Id;
        var newId  = Guid.NewGuid().ToString("D").ToUpperInvariant();
        var refCount = string.IsNullOrWhiteSpace(oldId)
            ? 0
            : ViewModel.ActionList.CountSoftwareIdReferences(oldId);

        if (refCount > 0)
        {
            var dialog = new ContentDialog
            {
                Title               = "Update References?",
                Content             = $"{refCount} AppTree SoftwareRef element{(refCount == 1 ? "" : "s")} reference{(refCount == 1 ? "s" : "")} the current ID \"{oldId}\". Update them to the new ID?",
                PrimaryButtonText   = "Update All",
                SecondaryButtonText = "Change ID Only",
                CloseButtonText     = "Cancel",
                DefaultButton       = ContentDialogButton.Primary,
                XamlRoot            = XamlRoot,
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.None) return;

            item.Id = newId;
            ViewModel.MarkModified();

            if (result == ContentDialogResult.Primary)
                ViewModel.ActionList.ReplaceSoftwareId(oldId, newId);
        }
        else
        {
            item.Id = newId;
            ViewModel.MarkModified();
        }
    }

    private async void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        var selected = ViewModel.Software.SelectedItem;
        if (selected is null) return;

        var dialog = new ContentDialog
        {
            Title             = "Remove Software Item",
            Content           = $"Remove \"{selected.Label}\"? Any TSVarList references to this item will become unresolved.",
            PrimaryButtonText = "Remove",
            CloseButtonText   = "Cancel",
            DefaultButton     = ContentDialogButton.Close,
            XamlRoot          = XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        ViewModel.Software.RemoveItemCommand.Execute(null);
        ViewModel.MarkModified();
    }

    // ── Focus-based sync ──────────────────────────────────────────────────────

    private void GuidedPanel_GotFocus(object sender, RoutedEventArgs e)
    {
        ViewModel.Software.SyncXmlToGuided();
    }

    private void XmlPanel_GotFocus(object sender, RoutedEventArgs e)
    {
        ViewModel.Software.SyncGuidedToXml();
    }

    // ── List column resize splitter ───────────────────────────────────────────

    private bool _listDragging;
    private double _listDragStartX;
    private double _listDragStartWidth;
    private static double _savedListColWidth = double.NaN;

    private void ListSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        => this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);

    private void ListSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_listDragging)
            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    }

    private void ListSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _listDragging = true;
        _listDragStartX     = e.GetCurrentPoint(this).Position.X;
        _listDragStartWidth = ListCol.ActualWidth;
        ((UIElement)sender).CapturePointer(e.Pointer);
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }

    private void ListSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_listDragging) return;
        var delta    = e.GetCurrentPoint(this).Position.X - _listDragStartX;
        var newWidth = Math.Max(180, Math.Min(this.ActualWidth - 300, _listDragStartWidth + delta));
        ListCol.Width = new GridLength(newWidth);
    }

    private void ListSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _listDragging = false;
        ((UIElement)sender).ReleasePointerCapture(e.Pointer);
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        _savedListColWidth = ListCol.ActualWidth;
    }

    // ── Guided/XML splitter drag ──────────────────────────────────────────────

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
        _dragStartX           = e.GetCurrentPoint(this).Position.X;
        _dragStartGuidedWidth = GuidedCol.ActualWidth;
        _dragStartXmlWidth    = XmlCol.ActualWidth;
        ((UIElement)sender).CapturePointer(e.Pointer);
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }

    private void DragSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging) return;
        var delta     = e.GetCurrentPoint(this).Position.X - _dragStartX;
        var newGuided = Math.Max(60, _dragStartGuidedWidth + delta);
        var newXml    = Math.Max(60, _dragStartXmlWidth    - delta);
        GuidedCol.Width = new GridLength(newGuided, GridUnitType.Star);
        XmlCol.Width    = new GridLength(newXml,    GridUnitType.Star);
    }

    private void DragSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _dragging = false;
        ((UIElement)sender).ReleasePointerCapture(e.Pointer);
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        _savedGuidedColWidth = GuidedCol.ActualWidth;
        _savedXmlColWidth    = XmlCol.ActualWidth;
    }

    // ── Collapse / expand ─────────────────────────────────────────────────────

    private double _savedGuidedWidth = double.NaN;
    private double _savedXmlWidth    = double.NaN;
    private bool   _guidedCollapsed;
    private bool   _xmlCollapsed;

    private void CollapseGuidedBtn_Click(object sender, RoutedEventArgs e)
    {
        _savedGuidedWidth  = GuidedCol.ActualWidth > 0 ? GuidedCol.ActualWidth : 400;
        _savedXmlWidth     = XmlCol.ActualWidth    > 0 ? XmlCol.ActualWidth    : 440;
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
        _savedXmlWidth     = XmlCol.ActualWidth    > 0 ? XmlCol.ActualWidth    : 440;
        _savedGuidedWidth  = GuidedCol.ActualWidth > 0 ? GuidedCol.ActualWidth : 400;
        XmlCol.MinWidth    = 0;
        XmlCol.Width       = new GridLength(0);
        SplitterCol.Width  = new GridLength(0);
        _xmlCollapsed      = true;
        CollapseXmlBtn.Visibility = Visibility.Collapsed;
        ExpandXmlBtn.Visibility   = Visibility.Visible;
        if (_guidedCollapsed) ExpandGuidedInternal();
    }

    private void ExpandXmlBtn_Click(object sender, RoutedEventArgs e) => ExpandXmlInternal();

    private void ExpandXmlInternal()
    {
        XmlCol.MinWidth    = 60;
        XmlCol.Width       = new GridLength(double.IsNaN(_savedXmlWidth)    ? 440 : _savedXmlWidth,    GridUnitType.Star);
        GuidedCol.Width    = new GridLength(double.IsNaN(_savedGuidedWidth) ? 400 : _savedGuidedWidth, GridUnitType.Star);
        SplitterCol.Width  = new GridLength(5);
        _xmlCollapsed      = false;
        CollapseXmlBtn.Visibility = Visibility.Visible;
        ExpandXmlBtn.Visibility   = Visibility.Collapsed;
    }
}
