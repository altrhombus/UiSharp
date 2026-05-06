namespace UIpp.Core.Actions;

public interface IAction
{
    ActionResult Go();
    bool IsGuiAction { get; }
}

public abstract class ActionBase(ActionData data) : IAction
{
    protected readonly ActionData Data = data;

    public abstract ActionResult Go();
    public virtual bool IsGuiAction => false;
}
