using UiSharp.Core.Actions;
using UiSharp.Core.Actions.Impl;
using UiSharp.Core.Configuration;
using UiSharp.Core.Logging;
using UiSharp.Core.Variables;

namespace UiSharp.Core.Tests.Actions.Impl;

file sealed class FakeProvider(IReadOnlySet<string>? supported = null) : IDefaultValueProvider
{
    public List<string> Collected { get; } = [];
    public IReadOnlySet<string> SupportedCategories { get; } =
        supported ?? XmlConstants.DefaultValueCategories.Ordered.ToHashSet();

    public void Collect(string category, ITSEnv env, ICMLog log)
    {
        Collected.Add(category);
        env.Set(category, "collected");
    }
}

public class ActionDefaultValuesTests
{
    [Fact]
    public void NoProvider_LogsWarning_ReturnsNext()
    {
        var el = ActionTestData.ActionEl("""<Action Type="DefaultValues" />""");
        var (_, log, data) = ActionTestData.Make(el);
        var result = new ActionDefaultValues(data).Go();
        Assert.Equal(ActionResult.Next, result);
        Assert.Single(log.Messages);
        Assert.Contains("no provider", log.Messages[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AllTypes_CallsAllSupportedCategories()
    {
        var el = ActionTestData.ActionEl("""<Action Type="DefaultValues" ValueTypes="All" />""");
        var (_, _, data) = ActionTestData.Make(el, provider: new FakeProvider());
        new ActionDefaultValues(data).Go();
        var fake = (FakeProvider)data.DefaultValueProvider!;
        Assert.Equal(XmlConstants.DefaultValueCategories.Ordered.Count, fake.Collected.Count);
    }

    [Fact]
    public void DefaultValueTypes_IsAll()
    {
        // no ValueTypes attr — defaults to "All"
        var el = ActionTestData.ActionEl("""<Action Type="DefaultValues" />""");
        var (_, _, data) = ActionTestData.Make(el, provider: new FakeProvider());
        new ActionDefaultValues(data).Go();
        var fake = (FakeProvider)data.DefaultValueProvider!;
        Assert.Equal(XmlConstants.DefaultValueCategories.Ordered.Count, fake.Collected.Count);
    }

    [Fact]
    public void SpecificTypes_OnlyThoseCategoriesCollected()
    {
        var el = ActionTestData.ActionEl("""<Action Type="DefaultValues" ValueTypes="OS,Asset" />""");
        var (_, _, data) = ActionTestData.Make(el, provider: new FakeProvider());
        new ActionDefaultValues(data).Go();
        var fake = (FakeProvider)data.DefaultValueProvider!;
        Assert.Equal(2, fake.Collected.Count);
        Assert.Contains("OS",    fake.Collected);
        Assert.Contains("Asset", fake.Collected);
    }

    [Fact]
    public void UnsupportedCategory_LogsWarning()
    {
        // Provider supports nothing
        var el = ActionTestData.ActionEl("""<Action Type="DefaultValues" ValueTypes="OS" />""");
        var (_, log, data) = ActionTestData.Make(el, provider: new FakeProvider(new HashSet<string>()));
        new ActionDefaultValues(data).Go();
        Assert.Single(log.Messages);
        Assert.Contains("not supported", log.Messages[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProviderException_LogsError_ContinuesOtherCategories()
    {
        var throwingProvider = new ThrowingProvider();
        var el = ActionTestData.ActionEl("""<Action Type="DefaultValues" ValueTypes="OS,Asset" />""");
        var (_, log, data) = ActionTestData.Make(el, provider: throwingProvider);
        new ActionDefaultValues(data).Go();
        // Error logged for OS, Asset still attempted
        Assert.Equal(2, throwingProvider.AttemptedCategories.Count);
        Assert.Contains(log.Messages, m => m.Contains("error", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Returns_Next()
    {
        var el = ActionTestData.ActionEl("""<Action Type="DefaultValues" />""");
        var (_, _, data) = ActionTestData.Make(el, provider: new FakeProvider());
        Assert.Equal(ActionResult.Next, new ActionDefaultValues(data).Go());
    }
}

file sealed class ThrowingProvider : IDefaultValueProvider
{
    public List<string> AttemptedCategories { get; } = [];
    public IReadOnlySet<string> SupportedCategories { get; } =
        XmlConstants.DefaultValueCategories.Ordered.ToHashSet();

    public void Collect(string category, ITSEnv env, ICMLog log)
    {
        AttemptedCategories.Add(category);
        throw new InvalidOperationException("simulated failure");
    }
}
