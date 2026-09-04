using System.Xml.Linq;
using UiSharp.Core.Dialogs;
using UiSharp.Core.Software;

namespace UiSharp.Core.Configuration;

public sealed record LoadedConfig(
    XDocument                              Document,
    DialogTraits                           GlobalTraits,
    IReadOnlyDictionary<string, ISoftware> Software,
    string                                 ConditionEngine,
    int?                                   SchemaVersion,
    XElement?                              Messages);
