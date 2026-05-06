namespace GUISharp.Services;

public interface IFileDialogService
{
    Task<string?> PickOpenFileAsync();
    Task<string?> PickSaveFileAsync(string? suggestedName = null);
}
