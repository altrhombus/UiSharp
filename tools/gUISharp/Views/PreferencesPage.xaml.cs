using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GUISharp.Views;

public sealed partial class PreferencesPage : Page
{
    public PreferencesPage()
    {
        this.InitializeComponent();
    }

    private void PreferencesPage_Loaded(object sender, RoutedEventArgs e)
    {
        RecentFilesLimitBox.Value = App.UserSettings.Settings.RecentFilesLimit;

        var infoVer = typeof(App).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? string.Empty;
        var plus    = infoVer.IndexOf('+');
        var version = plus >= 0 ? infoVer[..plus] : infoVer;
        var commit  = plus >= 0 ? infoVer[(plus + 1)..] : null;
        VersionText.Text = commit is { Length: > 0 }
            ? $"Version {version}  ·  {commit}"
            : $"Version {version}";
    }

    private void RecentFilesLimit_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!IsLoaded || double.IsNaN(sender.Value)) return;
        App.UserSettings.Settings.RecentFilesLimit = (int)sender.Value;
        App.UserSettings.Save();
    }
}
