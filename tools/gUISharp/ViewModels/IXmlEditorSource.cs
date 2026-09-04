using System.ComponentModel;

namespace UiSharp.Editor.ViewModels;

public interface IXmlEditorSource : INotifyPropertyChanged
{
    string CurrentXmlText { get; }
    string? XmlValidationError { get; }
    (int Start, int End) SelectedLineRange { get; }
    event EventHandler? SelectionDecorationChanged;
    void OnXmlEdited(string xml);
    void SelectAtLine(int line);
}
