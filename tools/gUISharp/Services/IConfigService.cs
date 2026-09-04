using UiSharp.Core.Configuration;

namespace UiSharp.Editor.Services;

public interface IConfigService
{
    Task<EditorConfig> LoadAsync(string path);
    Task SaveAsync(EditorConfig config, string path);
    EditorConfig NewConfig();
    EditorConfig LoadFromXml(string xml);
}
