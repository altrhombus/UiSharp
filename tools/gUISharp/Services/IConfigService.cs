using UIpp.Core.Configuration;

namespace GUISharp.Services;

public interface IConfigService
{
    Task<EditorConfig> LoadAsync(string path);
    Task SaveAsync(EditorConfig config, string path);
    EditorConfig NewConfig();
    EditorConfig LoadFromXml(string xml);
}
