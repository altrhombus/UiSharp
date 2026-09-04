namespace UiSharp.Editor.Services;

public sealed class UndoService : IUndoService
{
    private const int MaxSnapshots = 50;
    private readonly LinkedList<AppStateSnapshot> _undoStack = new();
    private readonly LinkedList<AppStateSnapshot> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void Push(AppStateSnapshot snapshot)
    {
        if (_undoStack.Count >= MaxSnapshots)
            _undoStack.RemoveFirst();
        _undoStack.AddLast(snapshot);
        _redoStack.Clear();
    }

    public AppStateSnapshot? TryUndo(AppStateSnapshot current)
    {
        if (_undoStack.Count == 0) return null;
        var prev = _undoStack.Last!.Value;
        _undoStack.RemoveLast();
        _redoStack.AddFirst(current);
        return prev;
    }

    public AppStateSnapshot? TryRedo(AppStateSnapshot current)
    {
        if (_redoStack.Count == 0) return null;
        var next = _redoStack.First!.Value;
        _redoStack.RemoveFirst();
        _undoStack.AddLast(current);
        return next;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
