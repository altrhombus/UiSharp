using Microsoft.UI.Xaml.Controls;

namespace UiSharp.Editor.Views;

public sealed partial class CommitDialog : ContentDialog
{
    public string CommitMessage => MessageBox.Text.Trim();

    public CommitDialog(string filePath)
    {
        this.InitializeComponent();
        FileNameText.Text = $"File: {System.IO.Path.GetFileName(filePath)}";
        IsPrimaryButtonEnabled = false;
    }

    private void MessageBox_TextChanged(object sender, TextChangedEventArgs e)
        => IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(MessageBox.Text);
}
