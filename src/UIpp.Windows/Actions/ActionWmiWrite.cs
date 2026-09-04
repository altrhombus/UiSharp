using System.Management;
using UIpp.Core.Actions;
using UIpp.Core.Configuration;
using UIpp.Core.Logging;

namespace UIpp.Windows.Actions;

// Creates a WMI namespace, class, or instance with the specified properties.
[ActionType(XmlConstants.ActionTypes.WmiWrite)]
public sealed class ActionWmiWrite(ActionData data) : ActionBase(data)
{
    public override ActionResult Go()
    {
        var ns  = Attr(XmlConstants.Attributes.Namespace, @"root\cimv2");
        var cls = Attr(XmlConstants.Attributes.Class);

        if (string.IsNullOrWhiteSpace(cls))
            return ActionResult.Next;

        // Collect Property child elements: <Property Name="..." Value="..." Type="..." Key="true|false"/>
        var props = Data.ActionNode
            .Elements(XmlConstants.Elements.Property)
            .Select(p => (
                // C++ reads all four through GetXMLAttribute (Actions.cpp:121-124),
                // so every one is variable-substituted.
                Name:  Attr(p, XmlConstants.Attributes.Name),
                Value: Attr(p, XmlConstants.Attributes.Value),
                Type:  Attr(p, XmlConstants.Attributes.PropertyType, XmlConstants.Defaults.CimType),
                IsKey: BoolAttr(p, XmlConstants.Attributes.PropertyKey)
            ))
            // C++: includes element if name OR value OR type is non-empty; type always defaults to
            // CIM_STRING so in practice every <Property> element is included.
            .Where(p => !string.IsNullOrWhiteSpace(p.Name) ||
                        !string.IsNullOrWhiteSpace(p.Value) ||
                        !string.IsNullOrWhiteSpace(p.Type))
            .ToList();

        try
        {
            EnsureNamespace(ns);

            var scope = new ManagementScope(ns);
            scope.Connect();

            // Ensure the class exists.
            using var mgmtClass = new ManagementClass(scope, new ManagementPath(cls), null);
            bool classExists;
            try { mgmtClass.Get(); classExists = true; }
            catch { classExists = false; }

            if (!classExists)
            {
                using var newClass = new ManagementClass(scope, new ManagementPath(), null);
                newClass["__CLASS"] = cls;
                foreach (var p in props)
                    newClass.Properties.Add(p.Name, MapCimType(p.Type));
                foreach (var p in props.Where(p => p.IsKey))
                    newClass.Properties[p.Name].Qualifiers.Add("key", true);
                newClass.Put();
            }

            // Create an instance.
            using var inst = mgmtClass.CreateInstance()
                ?? throw new InvalidOperationException("CreateInstance returned null.");
            foreach (var p in props)
                inst[p.Name] = p.Value;
            inst.Put();

            Data.Log.Write($"WmiWrite: created instance of {ns}:{cls}");
        }
        catch (Exception ex)
        {
            Data.Log.Write($"WmiWrite: failed for {ns}:{cls}: {ex.Message}", LogSeverity.Warning);
        }

        return ActionResult.Next;
    }

    private static void EnsureNamespace(string namespacePath)
    {
        // namespacePath like "root\MyNS\Sub" — ensure each level exists.
        var parts = namespacePath.Split('\\');
        for (int i = 2; i <= parts.Length; i++)
        {
            var parent = string.Join('\\', parts[..^(parts.Length - i + 1)]);
            var child  = parts[i - 1];
            if (parent.Length == 0) continue;
            try
            {
                var scope  = new ManagementScope(parent);
                scope.Connect();
                using var ns = new ManagementClass(scope, new ManagementPath("__NAMESPACE"), null);
                using var inst = ns.CreateInstance()!;
                inst["Name"] = child;
                inst.Put();
            }
            catch { /* namespace already exists or parent unreachable */ }
        }
    }

    private static CimType MapCimType(string typeStr) =>
        typeStr.ToUpperInvariant() switch
        {
            "CIM_SINT8"    => CimType.SInt8,
            "CIM_UINT8"    => CimType.UInt8,
            "CIM_SINT16"   => CimType.SInt16,
            "CIM_UINT16"   => CimType.UInt16,
            "CIM_SINT32"   => CimType.SInt32,
            "CIM_UINT32"   => CimType.UInt32,
            "CIM_SINT64"   => CimType.SInt64,
            "CIM_UINT64"   => CimType.UInt64,
            "CIM_REAL32"   => CimType.Real32,
            "CIM_REAL64"   => CimType.Real64,
            "CIM_BOOLEAN"  => CimType.Boolean,
            "CIM_DATETIME" => CimType.DateTime,
            _              => CimType.String,
        };
}
