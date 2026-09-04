namespace UiSharp.Editor.Services;

public interface IFileDialogService
{
    Task<string?> PickOpenFileAsync();
    Task<string?> PickSaveFileAsync(string? suggestedName = null);
}
