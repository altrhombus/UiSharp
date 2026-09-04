using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using UiSharp.Editor.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace UiSharp.Editor.Views;

public sealed partial class XmlEditorPanel : UserControl
{
    private bool _monacoReady;
    private bool _initialized;
    private string _pendingContent = string.Empty;
    private bool _isHandlingXmlEdit;
    private (int Start, int End) _lastSentDecoration = (-2, -2);
    private readonly JsonSerializerOptions _jsonOpts = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    // ── DependencyProperty: PanelViewModel ───────────────────────────────────

    public static readonly DependencyProperty PanelViewModelProperty =
        DependencyProperty.Register(
            nameof(PanelViewModel),
            typeof(object),
            typeof(XmlEditorPanel),
            new PropertyMetadata(null, OnPanelViewModelChanged));

    public IXmlEditorSource? PanelViewModel
    {
        get => (IXmlEditorSource?)GetValue(PanelViewModelProperty);
        set => SetValue(PanelViewModelProperty, value);
    }

    private static void OnPanelViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var panel = (XmlEditorPanel)d;
        if (e.OldValue is IXmlEditorSource old)
        {
            old.PropertyChanged -= panel.OnViewModelPropertyChanged;
            old.SelectionDecorationChanged -= panel.OnSelectionDecorationChanged;
        }
        if (e.NewValue is IXmlEditorSource vm)
        {
            vm.PropertyChanged += panel.OnViewModelPropertyChanged;
            vm.SelectionDecorationChanged += panel.OnSelectionDecorationChanged;
            panel.QueueContent(vm.CurrentXmlText);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not IXmlEditorSource vm) return;

        if (e.PropertyName == nameof(IXmlEditorSource.CurrentXmlText) && !_isHandlingXmlEdit)
            DispatcherQueue.TryEnqueue(() => QueueContent(vm.CurrentXmlText));
        else if (e.PropertyName == nameof(IXmlEditorSource.XmlValidationError))
            DispatcherQueue.TryEnqueue(() => ShowError(vm.XmlValidationError));
    }

    // Fires when the selected item changed but the XML content is identical — only the decoration needs to move.
    private void OnSelectionDecorationChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => _ = PushDecorationOnlyAsync());
    }

    // ── Construction & Loading ────────────────────────────────────────────────

    public XmlEditorPanel()
    {
        this.InitializeComponent();
        // When the page re-attaches to the visual tree (NavigationCacheMode reuse), Monaco is
        // already running but needs a layout() call to repaint after being hidden.
        Loaded += (_, _) =>
        {
            if (_monacoReady && MonacoWebView.CoreWebView2 is not null)
                _ = MonacoWebView.CoreWebView2.ExecuteScriptAsync(
                    "window.refreshLayout && window.refreshLayout()");
        };
        ActualThemeChanged += (_, _) =>
        {
            if (_monacoReady && MonacoWebView.CoreWebView2 is not null)
                _ = PushThemeAsync();
        };
    }

    private async void MonacoWebView_Loaded(object sender, RoutedEventArgs e)
    {
        // Guard: Loaded fires again each time the page re-attaches to the visual tree.
        // Re-running Navigate() would reload Monaco and lose content.
        if (_initialized) return;
        _initialized = true;
        try
        {
            await MonacoWebView.EnsureCoreWebView2Async();

            var assetsDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets");
            MonacoWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "uipp.editor", assetsDir, CoreWebView2HostResourceAccessKind.Allow);

            MonacoWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            var initialTheme = ActualTheme == ElementTheme.Light ? "vs" : "vs-dark";
            MonacoWebView.CoreWebView2.Navigate($"https://uipp.editor/editor.html?theme={initialTheme}");
        }
        catch (Exception ex)
        {
            ShowError("WebView2 initialization failed: " + ex.Message);
        }
    }

    // ── Message Passing ────────────────────────────────────────────────────────

    private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        var raw = args.TryGetWebMessageAsString();
        if (string.IsNullOrEmpty(raw)) return;

        MonacoMessage? msg;
        try { msg = JsonSerializer.Deserialize<MonacoMessage>(raw); }
        catch { return; }
        if (msg is null) return;

        switch (msg.Type)
        {
            case "ready":
                _monacoReady = true;
                var pending = _pendingContent;
                _pendingContent = string.Empty;
                DispatcherQueue.TryEnqueue(async () =>
                {
                    await PushThemeAsync();
                    if (!string.IsNullOrEmpty(pending))
                        await PushContentAsync(pending);
                });
                break;

            case "contentChanged":
                var edited = msg.Content ?? string.Empty;
                DispatcherQueue.TryEnqueue(() => HandleXmlEdit(edited));
                break;

            case "cursorLine":
                var line = msg.Line ?? 0;
                if (line > 0)
                    DispatcherQueue.TryEnqueue(() => PanelViewModel?.SelectAtLine(line));
                break;
        }
    }

    private void HandleXmlEdit(string xml)
    {
        _isHandlingXmlEdit = true;
        PanelViewModel?.OnXmlEdited(xml);
        _isHandlingXmlEdit = false;
    }

    // ── Content management ────────────────────────────────────────────────────

    private void QueueContent(string xml)
    {
        if (_monacoReady)
            _ = PushContentAsync(xml);
        else
            _pendingContent = xml;
    }

    private async Task PushThemeAsync()
    {
        if (MonacoWebView.CoreWebView2 is null) return;
        var theme = ActualTheme == ElementTheme.Light ? "vs" : "vs-dark";
        await MonacoWebView.CoreWebView2.ExecuteScriptAsync($"setTheme('{theme}')");
    }

    private async Task PushContentAsync(string xml)
    {
        if (MonacoWebView.CoreWebView2 is null) return;
        _lastSentDecoration = (-2, -2); // content changed — always scroll to the new position
        await MonacoWebView.CoreWebView2.ExecuteScriptAsync($"setContent({JsonSerializer.Serialize(xml)})");
        await PushDecorationOnlyAsync();
    }

    private async Task PushDecorationOnlyAsync()
    {
        if (!_monacoReady || MonacoWebView.CoreWebView2 is null) return;
        var vm = PanelViewModel;
        if (vm is null) return;
        var (start, end) = vm.SelectedLineRange;
        if ((start, end) == _lastSentDecoration) return; // range unchanged — skip to avoid distracting re-scroll
        _lastSentDecoration = (start, end);
        var decoJs = start > 0 ? $"setDecoration({start},{end})" : "clearDecoration()";
        try { await MonacoWebView.CoreWebView2.ExecuteScriptAsync(decoJs); }
        catch { }
    }

    // ── Error bar ─────────────────────────────────────────────────────────────

    private void ShowError(string? error)
    {
        if (string.IsNullOrEmpty(error))
        {
            XmlErrorBar.IsOpen = false;
        }
        else
        {
            XmlErrorBar.Title = "XML Error";
            XmlErrorBar.Message = error;
            XmlErrorBar.IsOpen = true;
            SetMarkersFromError(error);
        }
    }

    private async void SetMarkersFromError(string error)
    {
        if (!_monacoReady || MonacoWebView.CoreWebView2 is null) return;

        // Try to extract line/col from System.Xml.XmlException message format
        // "... Line N, position M."
        int line = 1, col = 1;
        var lineMatch = System.Text.RegularExpressions.Regex.Match(error, @"[Ll]ine\s+(\d+)");
        var posMatch  = System.Text.RegularExpressions.Regex.Match(error, @"[Pp]osition\s+(\d+)");
        if (lineMatch.Success) int.TryParse(lineMatch.Groups[1].Value, out line);
        if (posMatch.Success)  int.TryParse(posMatch.Groups[1].Value,  out col);

        var markers = new[]
        {
            new {
                severity = 8,
                startLineNumber = line,
                startColumn = Math.Max(1, col - 1),
                endLineNumber = line,
                endColumn = col + 20,
                message = error
            }
        };

        var js = $"setMarkers({JsonSerializer.Serialize(markers)})";
        try { await MonacoWebView.CoreWebView2.ExecuteScriptAsync(js); }
        catch { /* editor may not be ready */ }
    }

    private record MonacoMessage(
        [property: JsonPropertyName("type")]    string  Type,
        [property: JsonPropertyName("content")] string? Content,
        [property: JsonPropertyName("line")]    int?    Line);
}
