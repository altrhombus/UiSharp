using GUISharp.ViewModels;
using GUISharp.ViewModels.ActionEditors;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using UIpp.Core.Configuration;
using Windows.Foundation;
using Windows.UI;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.Views;

public sealed partial class ActionListPage : Page
{
    public MainWindowViewModel ViewModel => App.MainVm;

    private static double _savedTreeColWidth   = double.NaN;
    private static double _savedGuidedColWidth = double.NaN;
    private static double _savedXmlColWidth    = double.NaN;
    private int _seenConfigVersion = -1;

    public ActionListPage()
    {
        this.InitializeComponent();
        Loaded   += ActionListPage_Loaded;
        Unloaded += ActionListPage_Unloaded;
    }

    private void ActionListPage_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.ActionList.PropertyChanged += ActionList_PropertyChanged;

        if (!double.IsNaN(_savedTreeColWidth))   TreeCol.Width   = new GridLength(_savedTreeColWidth);
        if (!double.IsNaN(_savedGuidedColWidth)) GuidedCol.Width = new GridLength(_savedGuidedColWidth, GridUnitType.Star);
        if (!double.IsNaN(_savedXmlColWidth))    XmlCol.Width    = new GridLength(_savedXmlColWidth,    GridUnitType.Star);

        if (_seenConfigVersion != App.MainVm.ConfigVersion)
        {
            _seenConfigVersion = App.MainVm.ConfigVersion;
            GuidedScroller?.ChangeView(null, 0, null);
        }
    }

    private void ActionListPage_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.ActionList.PropertyChanged -= ActionList_PropertyChanged;
    }

    // ── Remove action ─────────────────────────────────────────────────────────

    private async void RemoveAction_Click(object sender, RoutedEventArgs e)
        => await TryRemoveSelectedActionAsync();

    // ── Copy / export / paste XML ─────────────────────────────────────────────

    private void CopyAsXml_Click(object sender, RoutedEventArgs e)
    {
        var xml = ViewModel.ActionList.GetSelectedActionXml();
        if (xml is null) return;
        var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
        dp.SetText(xml);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
    }

    private async void ExportGroupToFile_Click(object sender, RoutedEventArgs e)
    {
        var xml = ViewModel.ActionList.GetSelectedActionXml();
        if (xml is null) return;

        var picker = new Windows.Storage.Pickers.FileSavePicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));
        picker.SuggestedFileName = ViewModel.ActionList.SelectedAction?.DisplayLabel ?? "ActionGroup";
        picker.FileTypeChoices.Add("XML Files", [".xml"]);
        var file = await picker.PickSaveFileAsync();
        if (file is null) return;
        await Windows.Storage.FileIO.WriteTextAsync(file, xml);
    }

    private async void PasteFromClipboard_Click(object sender, RoutedEventArgs e)
    {
        AddFlyout.Hide();
        var view = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
        if (!view.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text)) return;
        var text = await view.GetTextAsync();
        if (!ViewModel.ActionList.TryPasteActionXml(text))
        {
            var dlg = new ContentDialog
            {
                Title             = "Invalid XML",
                Content           = "Clipboard does not contain a valid <Action> or <ActionGroup> element.",
                CloseButtonText   = "OK",
                DefaultButton     = ContentDialogButton.Close,
                XamlRoot          = XamlRoot,
            };
            await dlg.ShowAsync();
        }
    }

    private async void ActionTreeView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Delete)
        {
            e.Handled = true;
            await TryRemoveSelectedActionAsync();
        }
    }

    private void ActionTreeView_DragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
    {
        if (args.DropResult == Windows.ApplicationModel.DataTransfer.DataPackageOperation.None) return;
        if (args.Items.Count != 1 || args.Items[0] is not ActionNodeViewModel draggedVm) return;

        // Determine new parent and index from the TreeView's updated internal state.
        var newParentVm = args.NewParentItem as ActionNodeViewModel;
        var targetColl  = newParentVm?.Children ?? ViewModel.ActionList.ActionTree;

        var sourceColl = ActionListViewModel.FindOwningList(
            ViewModel.ActionList.ActionTree, draggedVm);
        if (sourceColl is null) return;

        // Find new index: search for draggedVm in target collection's current state
        // (CanReorderItems has already moved it visually; our ObservableCollection still
        // reflects the old order, so we sync based on the TreeView's RootNodes).
        int newIndex = GetIndexInNodes(
            newParentVm is null ? sender.RootNodes : GetNodeForVm(sender.RootNodes, newParentVm)?.Children,
            draggedVm);

        if (newIndex < 0) return;

        ViewModel.ActionList.MoveActionTo(draggedVm, sourceColl, targetColl, newIndex);
    }

    private static int GetIndexInNodes(IEnumerable<TreeViewNode>? nodes, ActionNodeViewModel vm)
    {
        if (nodes is null) return -1;
        int idx = 0;
        foreach (var node in nodes)
        {
            if (node.Content == vm) return idx;
            idx++;
        }
        return -1;
    }

    private static TreeViewNode? GetNodeForVm(IEnumerable<TreeViewNode> nodes, ActionNodeViewModel vm)
    {
        foreach (var node in nodes)
        {
            if (node.Content == vm) return node;
            var found = GetNodeForVm(node.Children, vm);
            if (found is not null) return found;
        }
        return null;
    }

    private async Task TryRemoveSelectedActionAsync()
    {
        var selected = ViewModel.ActionList.SelectedAction;
        if (selected is null) return;

        if (selected.IsGroup && selected.Children.Count > 0)
        {
            var n = selected.Children.Count;
            var dialog = new ContentDialog
            {
                Title             = "Delete Group",
                Content           = $"Delete \"{selected.DisplayLabel}\" and all {n} child action{(n == 1 ? "" : "s")}?",
                PrimaryButtonText = "Delete",
                CloseButtonText   = "Cancel",
                DefaultButton     = ContentDialogButton.Close,
                XamlRoot          = XamlRoot,
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        }
        else if (!selected.IsGroup && (selected.IsDirty || selected.HasComment))
        {
            var dialog = new ContentDialog
            {
                Title             = $"Delete \"{selected.DisplayLabel}\"?",
                Content           = "This action has unsaved changes. Delete it anyway?",
                PrimaryButtonText = "Delete",
                CloseButtonText   = "Cancel",
                DefaultButton     = ContentDialogButton.Close,
                XamlRoot          = XamlRoot,
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        }

        ViewModel.ActionList.RemoveActionCommand.Execute(null);
    }

    private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            ViewModel.ActionList.FilterText = string.Empty;
            ActionTreeView.Focus(FocusState.Keyboard);
            e.Handled = true;
        }
    }

    private void RemoveRecentItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: string path })
            ViewModel.RemoveRecentFileCommand.Execute(path);
    }

    private void WelcomeRecentList_Loaded(object sender, RoutedEventArgs e)
    {
        WelcomeRecentList.ItemsSource = ViewModel.RecentFiles
            .Select(p => new RecentFileEntry(System.IO.Path.GetFileName(p), p))
            .ToList();
        ViewModel.RecentFiles.CollectionChanged += (_, _) =>
            WelcomeRecentList.ItemsSource = ViewModel.RecentFiles
                .Select(p => new RecentFileEntry(System.IO.Path.GetFileName(p), p))
                .ToList();
    }

    // ── Add action flyout with search ─────────────────────────────────────────

    private static readonly (string Label, string TypeName, string Category)[] AllAddableActions =
    [
        ("Variables",              "TSVar",              "Variables"),
        ("Variable List",          "TSVarList",          "Variables"),
        ("Default Values",         "DefaultValues",      "Variables"),
        ("Switch",                 "Switch",             "Variables"),
        ("Input Dialog",           "Input",              "Input"),
        ("Preflight Checks",       "Preflight",          "Input"),
        ("Info Dialog",            "Info",               "Interactive"),
        ("Info (Full-Screen)",     "InfoFullScreen",     "Interactive"),
        ("Error Info",             "ErrorInfo",          "Interactive"),
        ("User Authentication",    "UserAuth",           "Interactive"),
        ("Application Tree",       "AppTree",            "Interactive"),
        ("External Call",          "ExternalCall",       "Utilities"),
        ("Random String",          "RandomString",       "Utilities"),
        ("File Read",              "FileRead",           "Utilities"),
        ("Save Items",             "SaveItems",          "Utilities"),
        ("Load / Save Variables",  "Vars",               "Utilities"),
        ("Software Discovery",     "SoftwareDiscovery",  "Utilities"),
        ("TPM Operations",         "TPM",                "Utilities"),
        ("Registry Read",          "RegRead",            "Registry / WMI"),
        ("Registry Write",         "RegWrite",           "Registry / WMI"),
        ("WMI Read",               "WMIRead",            "Registry / WMI"),
        ("WMI Write",              "WMIWrite",           "Registry / WMI"),
        ("HTTP / REST Request",    "REST",               "Network / Data"),
        ("Serialize to JSON",      "ToJSON",             "Network / Data"),
        ("Parse JSON",             "FromJSON",           "Network / Data"),
    ];

    private void AddFlyout_Opening(object sender, object e)
    {
        AddSearchBox.Text = string.Empty;
        RebuildAddCatalog(string.Empty);
        _ = AddSearchBox.Focus(FocusState.Keyboard);
    }

    private void AddSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        => RebuildAddCatalog(AddSearchBox.Text);

    private void AddSearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            AddFlyout.Hide();
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Enter)
        {
            var first = AddCatalogPanel.Children.OfType<Button>().FirstOrDefault();
            if (first is not null)
                AddFromTypeName((string)first.Tag);
            e.Handled = true;
        }
    }

    private void RebuildAddCatalog(string filter)
    {
        AddCatalogPanel.Children.Clear();
        bool isFiltering = filter.Length > 0;

        if (isFiltering)
        {
            var matches = AllAddableActions
                .Where(a => a.Label.Contains(filter, StringComparison.OrdinalIgnoreCase)
                         || a.Category.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                AddCatalogPanel.Children.Add(new TextBlock
                {
                    Text    = "No matches",
                    Opacity = 0.5,
                    Padding = new Thickness(8, 4, 8, 4),
                });
                return;
            }

            foreach (var (label, typeName, _) in matches)
                AddCatalogPanel.Children.Add(MakeCatalogButton(label, typeName));
        }
        else
        {
            AddCatalogPanel.Children.Add(MakeCatalogButton("Add Group", "Group"));
            AddCatalogPanel.Children.Add(MakePasteButton());
            AddSeparator(topMargin: 4, bottomMargin: 4);

            string? lastCategory = null;
            foreach (var (label, typeName, category) in AllAddableActions)
            {
                if (category != lastCategory)
                {
                    AddCatalogPanel.Children.Add(new TextBlock
                    {
                        Text       = category,
                        FontSize   = 11,
                        Opacity    = 0.6,
                        Padding    = new Thickness(8, lastCategory is null ? 2 : 8, 8, 2),
                        FontWeight = FontWeights.SemiBold,
                    });
                    lastCategory = category;
                }
                AddCatalogPanel.Children.Add(MakeCatalogButton(label, typeName));
            }
        }
    }

    private Button MakeCatalogButton(string label, string typeName)
    {
        var btn = new Button
        {
            Content             = label,
            Tag                 = typeName,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background          = new SolidColorBrush(Colors.Transparent),
            BorderThickness     = new Thickness(0),
            Padding             = new Thickness(8, 6, 8, 6),
        };
        btn.Click += CatalogButton_Click;
        return btn;
    }

    private Button MakePasteButton()
    {
        var btn = new Button
        {
            Content             = "Paste from Clipboard",
            Tag                 = "__paste__",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background          = new SolidColorBrush(Colors.Transparent),
            BorderThickness     = new Thickness(0),
            Padding             = new Thickness(8, 6, 8, 6),
        };
        btn.Click += PasteFromClipboard_Click;
        return btn;
    }

    private void CatalogButton_Click(object sender, RoutedEventArgs e)
        => AddFromTypeName((string)((Button)sender).Tag);

    private void AddFromTypeName(string typeName)
    {
        if (typeName == "Group")
            ViewModel.ActionList.AddGroupCommand.Execute(null);
        else
            ViewModel.ActionList.AddActionCommand.Execute(typeName);
        AddFlyout.Hide();
    }

    private void AddSeparator(double topMargin = 0, double bottomMargin = 0)
    {
        Brush divider = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"];
        AddCatalogPanel.Children.Add(new Border
        {
            Height     = 1,
            Margin     = new Thickness(0, topMargin, 0, bottomMargin),
            Background = divider,
        });
    }

    private void WelcomeOpenRecent_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RecentFileEntry entry)
            _ = ViewModel.OpenRecentCommand.ExecuteAsync(entry.FullPath);
    }

    private void PageRoot_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
        bool isCtrl = (ctrl & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;

        if (!isCtrl) return;

        switch (e.Key)
        {
            case Windows.System.VirtualKey.F:
                SearchBox.Focus(FocusState.Keyboard);
                SearchBox.SelectAll();
                e.Handled = true;
                break;

            case Windows.System.VirtualKey.D:
                if (ViewModel.ActionList.DuplicateActionCommand.CanExecute(null))
                    ViewModel.ActionList.DuplicateActionCommand.Execute(null);
                e.Handled = true;
                break;

            case Windows.System.VirtualKey.Enter:
                AddFlyout.ShowAt(AddButton);
                e.Handled = true;
                break;

            case Windows.System.VirtualKey.Up:
            {
                var alt = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu);
                if ((alt & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0 &&
                    ViewModel.ActionList.MoveUpCommand.CanExecute(null))
                {
                    ViewModel.ActionList.MoveUpCommand.Execute(null);
                    e.Handled = true;
                }
                break;
            }

            case Windows.System.VirtualKey.Down:
            {
                var alt = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu);
                if ((alt & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0 &&
                    ViewModel.ActionList.MoveDownCommand.CanExecute(null))
                {
                    ViewModel.ActionList.MoveDownCommand.Execute(null);
                    e.Handled = true;
                }
                break;
            }
        }
    }

    // ── Cascading rename TeachingTip ──────────────────────────────────────────

    private bool _renameUndoMode;

    private void RenameTip_ActionButtonClick(TeachingTip sender, object args)
    {
        if (_renameUndoMode)
        {
            ViewModel.ActionList.UndoRenameCommand.Execute(null);
            _renameUndoMode = false;
            RenameTip.IsOpen = false;
        }
        else
        {
            _renameUndoMode = true; // prevent PropertyChanged from closing the tip during Execute
            ViewModel.ActionList.AcceptRenameCommand.Execute(null);
            if (ViewModel.ActionList.HasRenameSnapshot)
            {
                RenameTip.Title               = "Rename applied";
                RenameTip.Subtitle            = "";
                RenameTip.ActionButtonContent = "Undo";
                RenameTip.CloseButtonContent  = "Done";
            }
            else
            {
                _renameUndoMode = false;
                RenameTip.IsOpen = false;
            }
        }
    }

    private void RenameTip_CloseButtonClick(TeachingTip sender, object args)
    {
        if (_renameUndoMode)
        {
            _renameUndoMode = false;
            ViewModel.ActionList.ClearRenameSnapshot();
        }
        else
        {
            ViewModel.ActionList.DismissPendingRename();
        }
        RenameTip.IsOpen = false;
    }

    private void ActionList_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ActionListViewModel.HasPendingRename)) return;
        if (ViewModel.ActionList.HasPendingRename)
        {
            _renameUndoMode               = false;
            RenameTip.Title               = "Rename variable references?";
            RenameTip.ActionButtonContent = "Update All";
            RenameTip.CloseButtonContent  = "Skip";
            RenameTip.IsOpen = true;
        }
        else if (!_renameUndoMode)
        {
            RenameTip.IsOpen = false;
        }
    }

    // ── Focus-based sync ──────────────────────────────────────────────────────

    private void GuidedPanel_GotFocus(object sender, RoutedEventArgs e)
    {
    }

    private void XmlPanel_GotFocus(object sender, RoutedEventArgs e)
    {
        ViewModel.ActionList.SyncGuidedToXml();
    }

    // ── Tree pane resize ─────────────────────────────────────────────────────

    private bool   _treeDragging;
    private double _treeDragStartX;
    private double _treeDragStartWidth;

    private void TreeSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        => this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);

    private void TreeSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_treeDragging)
            this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    }

    private void TreeSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _treeDragging = true;
        _treeDragStartX     = e.GetCurrentPoint(this).Position.X;
        _treeDragStartWidth = TreeCol.ActualWidth;
        ((UIElement)sender).CapturePointer(e.Pointer);
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }

    private void TreeSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_treeDragging) return;
        var delta    = e.GetCurrentPoint(this).Position.X - _treeDragStartX;
        var newWidth = Math.Max(200, Math.Min(this.ActualWidth - 300, _treeDragStartWidth + delta));
        TreeCol.Width = new GridLength(newWidth);
    }

    private void TreeSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _treeDragging = false;
        ((UIElement)sender).ReleasePointerCapture(e.Pointer);
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        _savedTreeColWidth = TreeCol.ActualWidth;
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
