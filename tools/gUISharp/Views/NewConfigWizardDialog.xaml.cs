using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UiSharp.Editor.Views;

public sealed partial class NewConfigWizardDialog : ContentDialog
{
    private enum Scenario { StandardOsd, SoftwareOnly, UserInfo, Blank }

    private int _step = 0;

    public NewConfigWizardDialog()
    {
        this.InitializeComponent();
        this.Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"];
        IsSecondaryButtonEnabled = false;
        UpdatePreview();
    }

    private void Radio_Checked(object sender, RoutedEventArgs e)
        => UpdatePreview();

    private void UpdatePreview()
    {
        if (PreviewList is null) return;
        PreviewList.ItemsSource = GetPreviewItems(SelectedScenario());
    }

    private static IReadOnlyList<string> GetPreviewItems(Scenario s) => s switch
    {
        Scenario.StandardOsd  => ["TSVar → OSDComputerName", "AppTree: Software Selection"],
        Scenario.SoftwareOnly => ["AppTree: Software Selection"],
        Scenario.UserInfo     => ["Input Dialog", "  InputText: Enter a value"],
        _                     => ["(empty — build from scratch)"],
    };

    public string GetTemplateXml()
    {
        var scenario = SelectedScenario();
        var title    = TitleBox.Text.Trim();
        if (string.IsNullOrEmpty(title)) title = DefaultTitle(scenario);
        var subtitle = SubtitleBox.Text.Trim();
        var color    = AccentColorPicker.ColorHex.Trim();
        if (string.IsNullOrEmpty(color)) color = "#002147";
        var icons    = ShowIconsToggle.IsOn   ? "true" : "false";
        var sidebar  = ShowSidebarToggle.IsOn ? "true" : "false";
        var appBase  = AppVarBaseBox.Text.Trim();
        if (string.IsNullOrEmpty(appBase)) appBase = "XApplications";
        var pkgBase  = PkgVarBaseBox.Text.Trim();
        if (string.IsNullOrEmpty(pkgBase)) pkgBase = "XPackages";

        var t   = Xml(title);
        var sub = string.IsNullOrEmpty(subtitle) ? "" : $" Subtitle=\"{Xml(subtitle)}\"";
        var app = $" Color=\"{Xml(color)}\" DialogIcons=\"{icons}\" DialogSidebar=\"{sidebar}\"";
        var a   = Xml(appBase);
        var p   = Xml(pkgBase);

        return scenario switch
        {
            Scenario.StandardOsd => $"""
                <UIpp Title="{t}"{sub}{app}>
                  <Actions>
                    <Action Type="TSVar" Variable="OSDComputerName" />
                    <Action Type="AppTree" Title="{t}" ApplicationVariableBase="{a}" PackageVariableBase="{p}" />
                  </Actions>
                </UIpp>
                """,

            Scenario.SoftwareOnly => $"""
                <UIpp Title="{t}"{sub}{app}>
                  <Actions>
                    <Action Type="AppTree" Title="{t}" ApplicationVariableBase="{a}" PackageVariableBase="{p}" />
                  </Actions>
                </UIpp>
                """,

            Scenario.UserInfo => $"""
                <UIpp Title="{t}"{sub}{app}>
                  <Actions>
                    <Action Type="Input" Title="{t}">
                      <InputText Variable="XUserInput" Prompt="Enter a value" Required="True" />
                    </Action>
                  </Actions>
                </UIpp>
                """,

            _ => $"""<UIpp Title="{t}"{sub}{app} />""",
        };
    }

    private void Dialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_step == 0)
        {
            args.Cancel = true;
            NavigateToStep2();
        }
        else if (_step == 1)
        {
            args.Cancel = true;
            NavigateToStep3();
        }
        // _step == 2: let the dialog close and return ContentDialogResult.Primary
    }

    private void NavigateToStep2()
    {
        _step = 1;
        var scenario = SelectedScenario();

        Step1Panel.Visibility      = Visibility.Collapsed;
        Step2Panel.Visibility      = Visibility.Visible;
        AppTreeSettings.Visibility = scenario is Scenario.StandardOsd or Scenario.SoftwareOnly
            ? Visibility.Visible : Visibility.Collapsed;

        Step2Subtitle.Text       = $"Configure your {ScenarioLabel(scenario)} configuration.";
        TitleBox.Text            = DefaultTitle(scenario);
        TitleBox.Focus(FocusState.Programmatic);
        StepLabel.Text           = "Step 2 of 3";
        PrimaryButtonText        = "Next";
        IsSecondaryButtonEnabled = true;
    }

    private void NavigateToStep3()
    {
        _step = 2;
        Step2Panel.Visibility = Visibility.Collapsed;
        Step3Panel.Visibility = Visibility.Visible;
        StepLabel.Text        = "Step 3 of 3";
        PrimaryButtonText     = "Create";
        AccentColorPicker.Focus(FocusState.Programmatic);
    }

    private void Dialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
        if (_step == 2)
        {
            _step = 1;
            Step3Panel.Visibility = Visibility.Collapsed;
            Step2Panel.Visibility = Visibility.Visible;
            StepLabel.Text        = "Step 2 of 3";
            PrimaryButtonText     = "Next";
        }
        else if (_step == 1)
        {
            _step = 0;
            Step2Panel.Visibility    = Visibility.Collapsed;
            Step1Panel.Visibility    = Visibility.Visible;
            StepLabel.Text           = "Step 1 of 3";
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
