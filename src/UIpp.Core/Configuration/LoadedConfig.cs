using System.Xml.Linq;
using UIpp.Core.Dialogs;
using UIpp.Core.Software;

namespace UIpp.Core.Configuration;

public sealed record LoadedConfig(
    XDocument                            Document,
    DialogTraits                         GlobalTraits,
    IReadOnlyDictionary<string, ISoftware> Software,
    string                               ConditionEngine);
