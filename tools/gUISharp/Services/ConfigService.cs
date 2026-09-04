using UiSharp.Core.Configuration;
using UiSharp.Editing;

namespace UiSharp.Editor.Services;

public sealed class ConfigService : IConfigService
{
    public Task<EditorConfig> LoadAsync(string path) =>
        Task.Run(() => EditorConfig.FromLoaded(ConfigLoader.Load(path)));

    public Task SaveAsync(EditorConfig config, string path)
    {
        ConfigWriter.Save(config, path);
        return Task.CompletedTask;
    }

    public EditorConfig NewConfig() => new();

    public EditorConfig LoadFromXml(string xml) =>
        EditorConfig.FromLoaded(ConfigLoader.LoadFromXml(xml));
}
