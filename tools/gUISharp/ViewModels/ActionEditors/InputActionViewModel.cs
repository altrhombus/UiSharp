using System.Collections.ObjectModel;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUISharp.Services;
using UIpp.Core.Configuration;
using C = UIpp.Core.Configuration.XmlConstants;

namespace GUISharp.ViewModels.ActionEditors;

// ── Base class ────────────────────────────────────────────────────────────────

public abstract partial class InputFieldViewModel : ObservableObject
{
    [ObservableProperty] public partial string Condition  { get; set; }
    [ObservableProperty] public partial bool   IsExpanded { get; set; }

    protected InputFieldViewModel()
    {
        Condition  = string.Empty;
        IsExpanded = false;
    }

    public abstract string ElementName { get; }

    public abstract XElement ToElement();

    protected static void SetAttr(XElement el, string attr, string val)
    {
        if (!string.IsNullOrEmpty(val)) el.SetAttributeValue(attr, val);
    }
}

// ── InputText ─────────────────────────────────────────────────────────────────

public sealed partial class InputTextViewModel : InputFieldViewModel
{
    public override string ElementName => C.InputTypes.Text;

    [ObservableProperty] public partial string Variable  { get; set; }
    [ObservableProperty] public partial string Question  { get; set; }
    [ObservableProperty] public partial string Default   { get; set; }
    [ObservableProperty] public partial string Hint      { get; set; }
    [ObservableProperty] public partial string Prompt    { get; set; }
    [ObservableProperty] public partial string RegEx     { get; set; }
    [ObservableProperty] public partial bool   Required  { get; set; }
    [ObservableProperty] public partial bool   Password  { get; set; }
    [ObservableProperty] public partial bool   ReadOnly  { get; set; }
    [ObservableProperty] public partial string ForceCase { get; set; }

    public IReadOnlyList<string> ForceCaseOptions { get; } = ["", C.Values.Upper, C.Values.Lower];

    public InputTextViewModel()
    {
        Variable  = string.Empty;
        Question  = string.Empty;
        Default   = string.Empty;
        Hint      = string.Empty;
        Prompt    = string.Empty;
        RegEx     = string.Empty;
        Required  = true;
        Password  = false;
        ReadOnly  = false;
        ForceCase = string.Empty;
        IsExpanded = true;
    }

    public override XElement ToElement()
    {
        var el = new XElement(C.InputTypes.Text);
        SetAttr(el, C.Attributes.Variable, Variable);
        SetAttr(el, C.Attributes.Question, Question);
        SetAttr(el, C.Attributes.Default,  Default);
        SetAttr(el, C.Attributes.Hint,     Hint);
        SetAttr(el, C.Attributes.Prompt,   Prompt);
        SetAttr(el, C.Attributes.RegEx,    RegEx);
        if (!Required) el.SetAttributeValue(C.Attributes.Required, C.Values.False);
        if (Password)  el.SetAttributeValue(C.Attributes.Password,  C.Values.True);
        if (ReadOnly)  el.SetAttributeValue(C.Attributes.ReadOnly,  C.Values.True);
        SetAttr(el, C.Attributes.ForceCase, ForceCase);
        SetAttr(el, C.Attributes.Condition, Condition);
        return el;
    }
}

// ── Choice item ───────────────────────────────────────────────────────────────

public sealed partial class ChoiceItemViewModel : ObservableObject
{
    [ObservableProperty] public partial string Option         { get; set; }
    [ObservableProperty] public partial string Value          { get; set; }
    [ObservableProperty] public partial string AlternateValue { get; set; }
    [ObservableProperty] public partial string Condition      { get; set; }

    public ChoiceItemViewModel()
    {
        Option         = string.Empty;
        Value          = string.Empty;
        AlternateValue = string.Empty;
        Condition      = string.Empty;
    }
}

// ── InputChoice ───────────────────────────────────────────────────────────────

public sealed partial class InputChoiceViewModel : InputFieldViewModel
{
    public override string ElementName => C.InputTypes.Choice;

    [ObservableProperty] public partial string Variable          { get; set; }
    [ObservableProperty] public partial string Question          { get; set; }
    [ObservableProperty] public partial string Default           { get; set; }
    [ObservableProperty] public partial string AlternateVariable { get; set; }
    [ObservableProperty] public partial bool   Required          { get; set; }
    [ObservableProperty] public partial bool   Sort              { get; set; }
    [ObservableProperty] public partial bool   AutoComplete      { get; set; }

    public ObservableCollection<ChoiceItemViewModel> Choices { get; } = [];

    public InputChoiceViewModel()
    {
        Variable          = string.Empty;
        Question          = string.Empty;
        Default           = string.Empty;
        AlternateVariable = string.Empty;
        Required          = false;
        Sort              = true;
        AutoComplete      = false;
        IsExpanded        = true;
    }

    [RelayCommand]
    private void AddChoice() => Choices.Add(new ChoiceItemViewModel());

    [RelayCommand]
    private void RemoveChoice(ChoiceItemViewModel item) => Choices.Remove(item);

    public override XElement ToElement()
    {
        var el = new XElement(C.InputTypes.Choice);
        SetAttr(el, C.Attributes.Variable,          Variable);
        SetAttr(el, C.Attributes.Question,          Question);
        SetAttr(el, C.Attributes.Default,           Default);
        SetAttr(el, C.Attributes.AlternateVariable, AlternateVariable);
        if (Required)     el.SetAttributeValue(C.Attributes.Required,     C.Values.True);
        if (!Sort)        el.SetAttributeValue(C.Attributes.Sort,         C.Values.False);
        if (AutoComplete) el.SetAttributeValue(C.Attributes.AutoComplete, C.Values.True);
        SetAttr(el, C.Attributes.Condition, Condition);
        foreach (var c in Choices)
        {
            var ce = new XElement(C.Elements.Choice);
            SetAttr(ce, C.Attributes.Option,         c.Option);
            SetAttr(ce, C.Attributes.Value,          c.Value);
            SetAttr(ce, C.Attributes.AlternateValue, c.AlternateValue);
            SetAttr(ce, C.Attributes.Condition,      c.Condition);
            el.Add(ce);
        }
        return el;
    }
}

// ── InputCheckbox ─────────────────────────────────────────────────────────────

public sealed partial class InputCheckboxViewModel : InputFieldViewModel
{
    public override string ElementName => C.InputTypes.Checkbox;

    [ObservableProperty] public partial string Variable       { get; set; }
    [ObservableProperty] public partial string Question       { get; set; }
    [ObservableProperty] public partial string Default        { get; set; }
    [ObservableProperty] public partial string CheckedValue   { get; set; }
    [ObservableProperty] public partial string UncheckedValue { get; set; }

    public InputCheckboxViewModel()
    {
        Variable       = string.Empty;
        Question       = string.Empty;
        Default        = string.Empty;
        CheckedValue   = "True";
        UncheckedValue = "False";
        IsExpanded     = true;
    }

    public override XElement ToElement()
    {
        var el = new XElement(C.InputTypes.Checkbox);
        SetAttr(el, C.Attributes.Variable, Variable);
        SetAttr(el, C.Attributes.Question, Question);
        SetAttr(el, C.Attributes.Default,  Default);
        if (CheckedValue   != "True")  el.SetAttributeValue(C.Attributes.CheckedValue,   CheckedValue);
        if (UncheckedValue != "False") el.SetAttributeValue(C.Attributes.UncheckedValue, UncheckedValue);
        SetAttr(el, C.Attributes.Condition, Condition);
        return el;
    }
}

// ── InputInfo ─────────────────────────────────────────────────────────────────

public sealed partial class InputInfoViewModel : InputFieldViewModel
{
    public override string ElementName => C.InputTypes.Info;

    [ObservableProperty] public partial string Text  { get; set; }
    [ObservableProperty] public partial string Color { get; set; }

    public InputInfoViewModel()
    {
        Text       = string.Empty;
        Color      = string.Empty;
        IsExpanded = true;
    }

    public override XElement ToElement()
    {
        var el = new XElement(C.InputTypes.Info);
        SetAttr(el, C.Attributes.Color,     Color);
        SetAttr(el, C.Attributes.Condition, Condition);
        el.Value = Text;
        return el;
    }
}

// ── InputActionViewModel ──────────────────────────────────────────────────────

public sealed partial class InputActionViewModel : ObservableObject, IActionEditor
{
    private readonly ActionNodeModel _model;

    [ObservableProperty] public partial string Title      { get; set; }
    [ObservableProperty] public partial string Size       { get; set; }
    [ObservableProperty] public partial bool   ShowCancel { get; set; }
    [ObservableProperty] public partial string Condition  { get; set; }

    public ObservableCollection<InputFieldViewModel> Fields { get; } = [];
    public bool HasFields => Fields.Count > 0;

    public static IReadOnlyList<string> SizeOptions { get; } =
        [C.Values.SizeRegular, C.Values.SizeTall, C.Values.SizeExtraTall];

    public InputActionViewModel(ActionNodeModel model)
    {
        _model = model;
        Fields.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFields));

        Title      = Attr(model.Node, C.Attributes.Title);
        Size       = Attr(model.Node, C.Attributes.Size, C.Values.SizeRegular);
        ShowCancel = BoolAttr(model.Node, C.Attributes.ShowCancel);
        Condition  = Attr(model.Node, C.Attributes.Condition);

        foreach (var el in model.Node.Elements())
        {
            var localName = el.Name.LocalName;
            if (!IsInputElement(localName)) continue;
            var vm = CreateFieldViewModel(el, NormalizeInputName(localName));
            if (vm is not null)
            {
                vm.IsExpanded = false;
                Fields.Add(vm);
            }
        }
    }

    [RelayCommand]
    private void AddTextField()    => Fields.Add(new InputTextViewModel());

    [RelayCommand]
    private void AddChoiceField()  => Fields.Add(new InputChoiceViewModel());

    [RelayCommand]
    private void AddCheckboxField() => Fields.Add(new InputCheckboxViewModel());

    [RelayCommand]
    private void AddInfoField()    => Fields.Add(new InputInfoViewModel());

    [RelayCommand]
    private void RemoveField(InputFieldViewModel item) => Fields.Remove(item);

    public void FlushToNode()
    {
        Set(C.Attributes.Title,      Title);
        Set(C.Attributes.Size,       Size);
        SetBool(C.Attributes.ShowCancel, ShowCancel);
        Set(C.Attributes.Condition,  Condition);

        _model.Node.Elements().Where(e => IsInputElement(e.Name.LocalName)).Remove();
        foreach (var field in Fields)
            _model.Node.Add(field.ToElement());
    }

    // ── Field factory ─────────────────────────────────────────────────────────

    private static InputFieldViewModel? CreateFieldViewModel(XElement el, string normalizedName)
        => normalizedName switch
        {
            C.InputTypes.Text => new InputTextViewModel
            {
                Variable  = Attr(el, C.Attributes.Variable),
                Question  = Attr(el, C.Attributes.Question),
                Default   = Attr(el, C.Attributes.Default),
                Hint      = Attr(el, C.Attributes.Hint),
                Prompt    = Attr(el, C.Attributes.Prompt),
                RegEx     = Attr(el, C.Attributes.RegEx),
                Required  = BoolAttr(el, C.Attributes.Required, true),
                Password  = BoolAttr(el, C.Attributes.Password),
                ReadOnly  = BoolAttr(el, C.Attributes.ReadOnly),
                ForceCase = Attr(el, C.Attributes.ForceCase),
                Condition = Attr(el, C.Attributes.Condition),
            },

            C.InputTypes.Choice   => BuildChoiceViewModel(el),

            C.InputTypes.Checkbox => new InputCheckboxViewModel
            {
                Variable       = Attr(el, C.Attributes.Variable),
                Question       = Attr(el, C.Attributes.Question),
                Default        = Attr(el, C.Attributes.Default),
                CheckedValue   = Attr(el, C.Attributes.CheckedValue,   "True"),
                UncheckedValue = Attr(el, C.Attributes.UncheckedValue, "False"),
                Condition      = Attr(el, C.Attributes.Condition),
            },

            C.InputTypes.Info => new InputInfoViewModel
            {
                Text      = el.Value.Trim(),
                Color     = Attr(el, C.Attributes.Color),
                Condition = Attr(el, C.Attributes.Condition),
            },

            _ => null,
        };

    private static InputChoiceViewModel BuildChoiceViewModel(XElement el)
    {
        var vm = new InputChoiceViewModel
        {
            Variable          = Attr(el, C.Attributes.Variable),
            Question          = Attr(el, C.Attributes.Question),
            Default           = Attr(el, C.Attributes.Default),
            AlternateVariable = Attr(el, C.Attributes.AlternateVariable),
            Required          = BoolAttr(el, C.Attributes.Required),
            Sort              = BoolAttr(el, C.Attributes.Sort, true),
            AutoComplete      = BoolAttr(el, C.Attributes.AutoComplete),
            Condition         = Attr(el, C.Attributes.Condition),
        };

        foreach (var choiceEl in el.Elements(C.Elements.Choice))
        {
            vm.Choices.Add(new ChoiceItemViewModel
            {
                Option         = Attr(choiceEl, C.Attributes.Option),
                Value          = Attr(choiceEl, C.Attributes.Value),
                AlternateValue = Attr(choiceEl, C.Attributes.AlternateValue),
                Condition      = Attr(choiceEl, C.Attributes.Condition),
            });
        }

        // Convert ChoiceList delimited entries into individual Choice items.
        foreach (var listEl in el.Elements(C.Elements.ChoiceList))
        {
            var opts = SplitList(Attr(listEl, C.Attributes.OptionList));
            var vals = SplitList(Attr(listEl, C.Attributes.ValueList, Attr(listEl, C.Attributes.OptionList)));
            for (int i = 0; i < opts.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(opts[i])) continue;
                vm.Choices.Add(new ChoiceItemViewModel
                {
                    Option = opts[i],
                    Value  = i < vals.Count ? vals[i] : string.Empty,
                });
            }
        }

        return vm;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<string> SplitList(string input) =>
        [.. input.Split([',', ';'], StringSplitOptions.TrimEntries)
                 .Where(s => !string.IsNullOrEmpty(s))];

    private static bool IsInputElement(string name) =>
        name.Equals(C.InputTypes.Text,        StringComparison.OrdinalIgnoreCase) ||
        name.Equals(C.InputTypes.Choice,      StringComparison.OrdinalIgnoreCase) ||
        name.Equals(C.InputTypes.Checkbox,    StringComparison.OrdinalIgnoreCase) ||
        name.Equals(C.InputTypes.Info,        StringComparison.OrdinalIgnoreCase) ||
        name.Equals(C.InputTypes.TextOld,     StringComparison.OrdinalIgnoreCase) ||
        name.Equals(C.InputTypes.ChoiceOld,   StringComparison.OrdinalIgnoreCase) ||
        name.Equals(C.InputTypes.CheckboxOld, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeInputName(string name) => name switch
    {
        var n when n.Equals(C.InputTypes.TextOld,     StringComparison.OrdinalIgnoreCase) => C.InputTypes.Text,
        var n when n.Equals(C.InputTypes.ChoiceOld,   StringComparison.OrdinalIgnoreCase) => C.InputTypes.Choice,
        var n when n.Equals(C.InputTypes.CheckboxOld, StringComparison.OrdinalIgnoreCase) => C.InputTypes.Checkbox,
        _ => name,
    };

    private static string Attr(XElement el, string name, string def = "") =>
        (string?)el.Attribute(name) ?? def;

    private static bool BoolAttr(XElement el, string name, bool def = false)
    {
        var v = Attr(el, name, def ? "True" : "False");
        return v.Equals("True", StringComparison.OrdinalIgnoreCase) ||
               v.Equals("1",    StringComparison.Ordinal);
    }

    private void Set(string name, string val)
    {
        if (string.IsNullOrEmpty(val)) _model.Node.Attribute(name)?.Remove();
        else _model.Node.SetAttributeValue(name, val);
    }

    private void SetBool(string name, bool val) =>
        _model.Node.SetAttributeValue(name, val ? C.Values.True : C.Values.False);
}
