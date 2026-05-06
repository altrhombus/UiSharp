using GUISharp.ViewModels;
using GUISharp.ViewModels.ActionEditors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.Views;

public sealed partial class ActionListPage : Page
{
    public MainWindowViewModel ViewModel => App.MainVm;

    public ActionListPage()
    {
        this.InitializeComponent();
    }
}

public sealed class ActionEditorTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TSVarTemplate         { get; set; }
    public DataTemplate? ExternalCallTemplate  { get; set; }
    public DataTemplate? DefaultValuesTemplate { get; set; }
    public DataTemplate? RandomStringTemplate  { get; set; }
    public DataTemplate? FileReadTemplate      { get; set; }
    public DataTemplate? VarsTemplate          { get; set; }
    public DataTemplate? FromJsonTemplate      { get; set; }
    public DataTemplate? RestTemplate          { get; set; }
    public DataTemplate? SaveItemsTemplate     { get; set; }
    public DataTemplate? ToJsonTemplate        { get; set; }
    public DataTemplate? TSVarListTemplate     { get; set; }
    public DataTemplate? PreflightTemplate     { get; set; }
    public DataTemplate? InputTemplate         { get; set; }
    public DataTemplate? ActionGroupTemplate   { get; set; }
    public DataTemplate? FallbackTemplate      { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is not ActionNodeViewModel vm) return FallbackTemplate!;

        if (vm.IsGroup) return ActionGroupTemplate ?? FallbackTemplate!;

        return vm.TypeName switch
        {
            C.ActionTypes.TSVar         => TSVarTemplate         ?? FallbackTemplate!,
            C.ActionTypes.ExternalCall  => ExternalCallTemplate  ?? FallbackTemplate!,
            C.ActionTypes.DefaultValues => DefaultValuesTemplate ?? FallbackTemplate!,
            C.ActionTypes.RandomString  => RandomStringTemplate  ?? FallbackTemplate!,
            C.ActionTypes.FileRead      => FileReadTemplate      ?? FallbackTemplate!,
            C.ActionTypes.Vars          => VarsTemplate          ?? FallbackTemplate!,
            C.ActionTypes.FromJson      => FromJsonTemplate      ?? FallbackTemplate!,
            C.ActionTypes.Rest          => RestTemplate          ?? FallbackTemplate!,
            C.ActionTypes.SaveItems     => SaveItemsTemplate     ?? FallbackTemplate!,
            C.ActionTypes.ToJson        => ToJsonTemplate        ?? FallbackTemplate!,
            C.ActionTypes.TSVarList     => TSVarListTemplate     ?? FallbackTemplate!,
            C.ActionTypes.Preflight     => PreflightTemplate     ?? FallbackTemplate!,
            C.ActionTypes.UserInput     => InputTemplate         ?? FallbackTemplate!,
            _                           => FallbackTemplate!,
        };
    }
}
