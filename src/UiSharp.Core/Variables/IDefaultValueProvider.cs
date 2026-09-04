using UiSharp.Core.Logging;

namespace UiSharp.Core.Variables;

public interface IDefaultValueProvider
{
    IReadOnlySet<string> SupportedCategories { get; }
    void Collect(string category, ITSEnv env, ICMLog log);
}
