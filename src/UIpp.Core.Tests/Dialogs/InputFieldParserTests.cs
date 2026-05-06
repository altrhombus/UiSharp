using System.Xml.Linq;
using UIpp.Core.Dialogs;
using UIpp.Core.Scripting;
using UIpp.Core.Variables;

namespace UIpp.Core.Tests.Dialogs;

public class InputFieldParserTests
{
    private static readonly IConditionEvaluator Conditions = new NativeConditionEvaluator();

    private static LocalTSEnv Env(params (string k, string v)[] vars)
    {
        var e = new LocalTSEnv();
        foreach (var (k, v) in vars) e.Set(k, v);
        return e;
    }

    private static XElement ActionEl(string inner) =>
        XElement.Parse($"""<Action Type="Input">{inner}</Action>""");

    // -------------------------------------------------------------------------
    // InputText
    // -------------------------------------------------------------------------

    [Fact]
    public void InputText_BasicAttributes_Parsed()
    {
        var el = ActionEl("""
            <InputText Question="Site code?" Variable="Site"
                       Hint="3 chars" Required="True" RegEx="^[A-Z]{3}$" />
            """);
        var specs = InputFieldParser.Parse(el, new LocalTSEnv(), Conditions);
        var text  = Assert.IsType<InputTextSpec>(Assert.Single(specs));
        Assert.Equal("Site code?", text.Question);
        Assert.Equal("Site",       text.Variable);
        Assert.Equal("3 chars",    text.Hint);
        Assert.True(text.Required);
        Assert.Equal(@"^[A-Z]{3}$", text.Regex);
    }

    [Fact]
    public void InputText_DefaultValue_FromEnv_PreferredOverXml()
    {
        var env = Env(("Site", "CHI"));
        var el  = ActionEl("""<InputText Question="?" Variable="Site" Default="DEFAULT" />""");
        var specs = InputFieldParser.Parse(el, env, Conditions);
        var text  = Assert.IsType<InputTextSpec>(Assert.Single(specs));
        Assert.Equal("CHI", text.DefaultValue);  // env wins over Default attr
    }

    [Fact]
    public void InputText_DefaultValue_FromAttr_WhenEnvEmpty()
    {
        var el    = ActionEl("""<InputText Question="?" Variable="Site" Default="FALLBACK" />""");
        var specs = InputFieldParser.Parse(el, new LocalTSEnv(), Conditions);
        var text  = Assert.IsType<InputTextSpec>(Assert.Single(specs));
        Assert.Equal("FALLBACK", text.DefaultValue);
    }

    [Fact]
    public void InputText_Validate_Required_Empty_Fails()
    {
        var spec = new InputTextSpec { Question = "Q", Variable = "V", Required = true };
        Assert.False(spec.Validate("").IsValid);
    }

    [Fact]
    public void InputText_Validate_Required_NonEmpty_Passes()
    {
        var spec = new InputTextSpec { Question = "Q", Variable = "V", Required = true };
        Assert.True(spec.Validate("hello").IsValid);
    }

    [Fact]
    public void InputText_Validate_Regex_Match_Passes()
    {
        var spec = new InputTextSpec { Question = "Q", Variable = "V", Regex = @"^\d{3}$", Required = false };
        Assert.True(spec.Validate("123").IsValid);
    }

    [Fact]
    public void InputText_Validate_Regex_NoMatch_Fails()
    {
        var spec = new InputTextSpec { Question = "Q", Variable = "V", Regex = @"^\d{3}$", Required = false };
        Assert.False(spec.Validate("abc").IsValid);
    }

    [Fact]
    public void InputText_Validate_Empty_NotRequired_Passes()
    {
        var spec = new InputTextSpec { Question = "Q", Variable = "V", Required = false };
        Assert.True(spec.Validate("").IsValid);
    }

    [Fact]
    public void InputText_ApplyForceCase_Upper()
    {
        var spec = new InputTextSpec { Question = "Q", Variable = "V", ForceCase = "Upper" };
        Assert.Equal("HELLO", spec.ApplyForceCase("hello"));
    }

    [Fact]
    public void InputText_ApplyForceCase_Lower()
    {
        var spec = new InputTextSpec { Question = "Q", Variable = "V", ForceCase = "Lower" };
        Assert.Equal("hello", spec.ApplyForceCase("HELLO"));
    }

    [Fact]
    public void InputText_ApplyForceCase_None_Unchanged()
    {
        var spec = new InputTextSpec { Question = "Q", Variable = "V" };
        Assert.Equal("Mixed", spec.ApplyForceCase("Mixed"));
    }

    [Fact]
    public void InputText_Condition_False_Excluded()
    {
        var el    = ActionEl("""<InputText Question="Q" Variable="V" Condition="'A' = 'B'" />""");
        var specs = InputFieldParser.Parse(el, new LocalTSEnv(), Conditions);
        Assert.Empty(specs);
    }

    [Fact]
    public void InputText_LegacyElementName_Accepted()
    {
        var el    = ActionEl("""<TextInput Question="Q" Variable="V" />""");
        var specs = InputFieldParser.Parse(el, new LocalTSEnv(), Conditions);
        Assert.IsType<InputTextSpec>(Assert.Single(specs));
    }

    // -------------------------------------------------------------------------
    // InputChoice
    // -------------------------------------------------------------------------

    [Fact]
    public void InputChoice_IndividualChoices_Parsed()
    {
        var el = ActionEl("""
            <InputChoice Question="Role?" Variable="Role">
              <Choice Option="Workstation" Value="WKS" />
              <Choice Option="Server"      Value="SRV" />
            </InputChoice>
            """);
        var specs  = InputFieldParser.Parse(el, new LocalTSEnv(), Conditions);
        var choice = Assert.IsType<InputChoiceSpec>(Assert.Single(specs));
        Assert.Equal(2, choice.Choices.Count);
        Assert.Equal("WKS", choice.Choices[0].Value);
        Assert.Equal("SRV", choice.Choices[1].Value);
    }

    [Fact]
    public void InputChoice_ChoiceList_Parsed()
    {
        var el = ActionEl("""
            <InputChoice Question="Site?" Variable="SiteCode">
              <ChoiceList OptionList="Chicago,Denver" ValueList="CHI,DEN" />
            </InputChoice>
            """);
        var specs  = InputFieldParser.Parse(el, new LocalTSEnv(), Conditions);
        var choice = Assert.IsType<InputChoiceSpec>(Assert.Single(specs));
        Assert.Equal(2, choice.Choices.Count);
        Assert.Equal("CHI", choice.Choices[0].Value);
        Assert.Equal("DEN", choice.Choices[1].Value);
    }

    [Fact]
    public void InputChoice_ChoiceList_SemicolonDelimited()
    {
        var el = ActionEl("""
            <InputChoice Question="?" Variable="X">
              <ChoiceList OptionList="A;B;C" />
            </InputChoice>
            """);
        var specs  = InputFieldParser.Parse(el, new LocalTSEnv(), Conditions);
        var choice = Assert.IsType<InputChoiceSpec>(Assert.Single(specs));
        Assert.Equal(3, choice.Choices.Count);
    }

    [Fact]
    public void InputChoice_ChoiceCondition_False_Excluded()
    {
        var el = ActionEl("""
            <InputChoice Question="?" Variable="X">
              <Choice Option="A" Value="A" Condition="'x'='y'" />
              <Choice Option="B" Value="B" />
            </InputChoice>
            """);
        var specs  = InputFieldParser.Parse(el, new LocalTSEnv(), Conditions);
        var choice = Assert.IsType<InputChoiceSpec>(Assert.Single(specs));
        Assert.Single(choice.Choices);
        Assert.Equal("B", choice.Choices[0].Value);
    }

    [Fact]
    public void InputChoice_NoChoices_Excluded()
    {
        var el = ActionEl("""
            <InputChoice Question="?" Variable="X">
              <Choice Option="A" Condition="'x'='y'" />
            </InputChoice>
            """);
        var specs = InputFieldParser.Parse(el, new LocalTSEnv(), Conditions);
        Assert.Empty(specs);
    }

    [Fact]
    public void InputChoice_Validate_RequiredEmpty_Fails()
    {
        var spec = new InputChoiceSpec
        {
            Question = "Q", Variable = "V", Required = true,
            Choices  = [new("A", "A", "")],
        };
        Assert.False(spec.Validate("").IsValid);
    }

    [Fact]
    public void InputChoice_OptionValue_DefaultsToOption()
    {
        var el = ActionEl("""
            <InputChoice Question="?" Variable="X">
              <Choice Option="Alpha" />
            </InputChoice>
            """);
        var specs  = InputFieldParser.Parse(el, new LocalTSEnv(), Conditions);
        var choice = Assert.IsType<InputChoiceSpec>(Assert.Single(specs));
        Assert.Equal("Alpha", choice.Choices[0].Value);
    }

    // -------------------------------------------------------------------------
    // InputCheckbox
    // -------------------------------------------------------------------------

    [Fact]
    public void InputCheckbox_Parsed()
    {
        var el = ActionEl("""
            <InputCheckbox Question="Enable?" Variable="Enabled"
                           CheckedValue="Yes" UncheckedValue="No" />
            """);
        var specs = InputFieldParser.Parse(el, new LocalTSEnv(), Conditions);
        var cb    = Assert.IsType<InputCheckboxSpec>(Assert.Single(specs));
        Assert.Equal("Yes", cb.CheckedValue);
        Assert.Equal("No",  cb.UncheckedValue);
    }

    [Fact]
    public void InputCheckbox_DefaultCheckedValues()
    {
        var el    = ActionEl("""<InputCheckbox Question="?" Variable="X" />""");
        var specs = InputFieldParser.Parse(el, new LocalTSEnv(), Conditions);
        var cb    = Assert.IsType<InputCheckboxSpec>(Assert.Single(specs));
        Assert.Equal("True",  cb.CheckedValue);
        Assert.Equal("False", cb.UncheckedValue);
    }

    // -------------------------------------------------------------------------
    // InputInfo
    // -------------------------------------------------------------------------

    [Fact]
    public void InputInfo_TextFromContent()
    {
        var el    = ActionEl("""<InputInfo NumberofLines="3">Hello, %Name%!</InputInfo>""");
        var env   = Env(("Name", "World"));
        var specs = InputFieldParser.Parse(el, env, Conditions);
        var info  = Assert.IsType<InputInfoSpec>(Assert.Single(specs));
        Assert.Equal("Hello, World!", info.Question);
        Assert.Equal(3, info.NumberOfLines);
    }

    [Fact]
    public void InputInfo_EscapeSequences_Expanded()
    {
        var el    = ActionEl("""<InputInfo>Line1\nLine2</InputInfo>""");
        var specs = InputFieldParser.Parse(el, new LocalTSEnv(), Conditions);
        var info  = Assert.IsType<InputInfoSpec>(Assert.Single(specs));
        Assert.Contains('\n', info.Question);
    }

    // -------------------------------------------------------------------------
    // Mixed / ordering
    // -------------------------------------------------------------------------

    [Fact]
    public void Mixed_FieldTypes_PreserveOrder()
    {
        var el = ActionEl("""
            <InputText     Question="Text"     Variable="T" />
            <InputCheckbox Question="Check"    Variable="C" />
            <InputInfo>Info text</InputInfo>
            <InputChoice   Question="Choice"   Variable="Ch">
              <Choice Option="X" />
            </InputChoice>
            """);
        var specs = InputFieldParser.Parse(el, new LocalTSEnv(), Conditions);
        Assert.Equal(4, specs.Count);
        Assert.IsType<InputTextSpec>(specs[0]);
        Assert.IsType<InputCheckboxSpec>(specs[1]);
        Assert.IsType<InputInfoSpec>(specs[2]);
        Assert.IsType<InputChoiceSpec>(specs[3]);
    }

    [Fact]
    public void UnknownElements_Ignored()
    {
        var el    = ActionEl("""<RandomThing Foo="bar" /><InputText Question="Q" Variable="V" />""");
        var specs = InputFieldParser.Parse(el, new LocalTSEnv(), Conditions);
        Assert.Single(specs);
    }
}
