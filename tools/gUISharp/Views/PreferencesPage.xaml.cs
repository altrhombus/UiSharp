using GUISharp.Services;
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

        (s.Theme switch
        {
            AppTheme.Light => ThemeLight,
            AppTheme.Dark  => ThemeDark,
            _              => ThemeSystem,
        }).IsChecked = true;

        RecentFilesLimitBox.Value = s.RecentFilesLimit;
    }

    private void Theme_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        var rb    = sender as RadioButton;
        var theme = rb == ThemeLight ? AppTheme.Light
                  : rb == ThemeDark  ? AppTheme.Dark
                                     : AppTheme.System;
        App.UserSettings.Settings.Theme = theme;
        App.UserSettings.Save();
        App.ApplyTheme();
    }

    private void RecentFilesLimit_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!IsLoaded || double.IsNaN(sender.Value)) return;
        App.UserSettings.Settings.RecentFilesLimit = (int)sender.Value;
        App.UserSettings.Save();
    }
}
