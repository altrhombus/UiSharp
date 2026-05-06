namespace UIpp.Core.Actions;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ActionTypeAttribute(string typeName) : Attribute
{
    public string TypeName { get; } = typeName;
}
