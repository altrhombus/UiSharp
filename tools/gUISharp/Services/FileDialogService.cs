using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;

namespace UiSharp.Editor.Services;

public sealed class FileDialogService : IFileDialogService
{
    public async Task<string?> PickOpenFileAsync()
    {
        var picker = new FileOpenPicker();
        InitializePicker(picker);
        picker.FileTypeFilter.Add(".xml");
        picker.FileTypeFilter.Add("*");

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    public async Task<string?> PickSaveFileAsync(string? suggestedName = null)
    {
        var picker = new FileSavePicker();
        InitializePicker(picker);
        picker.SuggestedFileName = suggestedName ?? "UI++.xml";
        picker.FileTypeChoices.Add("XML Files", [".xml"]);

        var file = await picker.PickSaveFileAsync();
        return file?.Path;
    }

    private static void InitializePicker(object picker)
    {
        // Unpackaged apps must initialize pickers with the window handle.
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
    }
}
