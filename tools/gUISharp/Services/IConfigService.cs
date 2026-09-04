using UiSharp.Core.Configuration;
using UiSharp.Editing;

namespace UiSharp.Editor.Services;

public interface IConfigService
{
    Task<EditorConfig> LoadAsync(string path);
    Task SaveAsync(EditorConfig config, string path);
    EditorConfig NewConfig();
    EditorConfig LoadFromXml(string xml);
}
