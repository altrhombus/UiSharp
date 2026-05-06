using UIpp.Core.Logging;

namespace UIpp.Core.Variables;

public interface IDefaultValueProvider
{
    IReadOnlySet<string> SupportedCategories { get; }
    void Collect(string category, ITSEnv env, ICMLog log);
}
