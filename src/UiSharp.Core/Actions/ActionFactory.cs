using System.Reflection;

namespace UiSharp.Core.Actions;

public sealed class ActionFactory
{
    private readonly Dictionary<string, Func<ActionData, IAction>> _registry =
        new(StringComparer.OrdinalIgnoreCase);

    // Scans an assembly for ActionBase subclasses tagged with [ActionType] and registers them.
    public void RegisterFromAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract) continue;
            if (!type.IsAssignableTo(typeof(IAction))) continue;

            var attr = type.GetCustomAttribute<ActionTypeAttribute>();
            if (attr is null) continue;

            var ctor = type.GetConstructor([typeof(ActionData)]);
            if (ctor is null) continue;

            _registry[attr.TypeName] = data => (IAction)ctor.Invoke([data]);
        }
    }

    public void Register(string typeName, Func<ActionData, IAction> factory) =>
        _registry[typeName] = factory;

    public IAction? Create(string typeName, ActionData data) =>
        _registry.TryGetValue(typeName, out var factory) ? factory(data) : null;

    public bool IsRegistered(string typeName) => _registry.ContainsKey(typeName);
}
