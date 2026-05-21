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
        var s = App.UserSettings.Settings;

        RecentFilesLimitBox.Value = s.RecentFilesLimit;
        ConfigMgrServerBox.Text   = s.ConfigMgrServer;
        ConfigMgrSiteCodeBox.Text = s.ConfigMgrSiteCode;

        switch (s.DefaultPanelLayout)
        {
            case "GuidedOnly": LayoutGuidedOnly.IsChecked = true; break;
            case "XmlOnly":    LayoutXmlOnly.IsChecked    = true; break;
            default:           LayoutBoth.IsChecked       = true; break;
        }

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

    private void ConfigMgrServer_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        App.UserSettings.Settings.ConfigMgrServer = ConfigMgrServerBox.Text.Trim();
        App.UserSettings.Save();
    }

    private void ConfigMgrSiteCode_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        App.UserSettings.Settings.ConfigMgrSiteCode = ConfigMgrSiteCodeBox.Text.Trim();
        App.UserSettings.Save();
    }

    private void Layout_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        App.UserSettings.Settings.DefaultPanelLayout =
            LayoutGuidedOnly.IsChecked == true ? "GuidedOnly" :
            LayoutXmlOnly.IsChecked    == true ? "XmlOnly"    : "Both";
        App.UserSettings.Save();
    }
}
