using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GUISharp.Views;

public sealed partial class NewConfigWizardDialog : ContentDialog
{
    private enum Scenario { StandardOsd, SoftwareOnly, UserInfo, Blank }

    private int _step = 0;

    public NewConfigWizardDialog()
    {
        this.InitializeComponent();
        this.Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"];
        IsSecondaryButtonEnabled = false;
    }

    public string GetTemplateXml()
    {
        var scenario = SelectedScenario();
        var title    = TitleBox.Text.Trim();
        if (string.IsNullOrEmpty(title)) title = DefaultTitle(scenario);
        var appBase  = AppVarBaseBox.Text.Trim();
        if (string.IsNullOrEmpty(appBase)) appBase = "XApplications";
        var pkgBase  = PkgVarBaseBox.Text.Trim();
        if (string.IsNullOrEmpty(pkgBase)) pkgBase = "XPackages";
        var t = Xml(title); var a = Xml(appBase); var p = Xml(pkgBase);

        return scenario switch
        {
            Scenario.StandardOsd => $"""
                <UIpp Title="{t}">
                  <Actions>
                    <Action Type="TSVar" Variable="OSDComputerName" />
                    <Action Type="AppTree" Title="{t}" ApplicationVariableBase="{a}" PackageVariableBase="{p}" />
                  </Actions>
                </UIpp>
                """,

            Scenario.SoftwareOnly => $"""
                <UIpp Title="{t}">
                  <Actions>
                    <Action Type="AppTree" Title="{t}" ApplicationVariableBase="{a}" PackageVariableBase="{p}" />
                  </Actions>
                </UIpp>
                """,

            Scenario.UserInfo => $"""
                <UIpp Title="{t}">
                  <Actions>
                    <Action Type="Input" Title="{t}">
                      <InputText Variable="XUserInput" Prompt="Enter a value" Required="True" />
                    </Action>
                  </Actions>
                </UIpp>
                """,

            _ => $"""<UIpp Title="{t}" />""",
        };
    }

    private void Dialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_step == 0)
        {
            args.Cancel = true;
            NavigateToStep2();
        }
    }

    private void NavigateToStep2()
    {
        _step = 1;
        var scenario = SelectedScenario();

        Step1Panel.Visibility     = Visibility.Collapsed;
        Step2Panel.Visibility     = Visibility.Visible;
        AppTreeSettings.Visibility = scenario is Scenario.StandardOsd or Scenario.SoftwareOnly
            ? Visibility.Visible : Visibility.Collapsed;

        Step2Subtitle.Text   = $"Configure your {ScenarioLabel(scenario)} configuration.";
        TitleBox.Text        = DefaultTitle(scenario);
        TitleBox.Focus(FocusState.Programmatic);
        PrimaryButtonText    = "Create";
        IsSecondaryButtonEnabled = true;
    }

    private void Dialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_step == 1)
        {
            args.Cancel = true;
            _step = 0;
            Step1Panel.Visibility    = Visibility.Visible;
            Step2Panel.Visibility    = Visibility.Collapsed;
            PrimaryButtonText        = "Next";
            IsSecondaryButtonEnabled = false;
        }
    }

    private Scenario SelectedScenario()
    {
        if (RadioSoftwareOnly.IsChecked == true) return Scenario.SoftwareOnly;
        if (RadioUserInfo.IsChecked     == true) return Scenario.UserInfo;
        if (RadioBlank.IsChecked        == true) return Scenario.Blank;
        return Scenario.StandardOsd;
    }

    private static string DefaultTitle(Scenario s) => s switch
    {
        Scenario.StandardOsd  => "Operating System Deployment",
        Scenario.SoftwareOnly => "Software Selection",
        Scenario.UserInfo     => "User Information",
        _                     => "New Configuration",
    };

    private static string ScenarioLabel(Scenario s) => s switch
    {
        Scenario.StandardOsd  => "Standard OSD",
        Scenario.SoftwareOnly => "Software Selection",
        Scenario.UserInfo     => "User Info Capture",
        _                     => "Blank",
    };

    private static string Xml(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
