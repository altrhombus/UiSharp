namespace GUISharp.Services;

public record AppStateSnapshot(
    string GlobalSettingsXml,
    string SoftwareXml,
    string ActionsXml);

public interface IUndoService
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    void Push(AppStateSnapshot snapshot);
    AppStateSnapshot? TryUndo(AppStateSnapshot current);
    AppStateSnapshot? TryRedo(AppStateSnapshot current);
    void Clear();
}
