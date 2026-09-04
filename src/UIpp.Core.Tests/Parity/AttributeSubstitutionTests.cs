using System.Xml.Linq;
using UIpp.Core.Configuration;
using UIpp.Core.Dialogs;
using UIpp.Core.Scripting;
using UIpp.Core.Variables;

namespace UIpp.Core.Tests.Parity;

/// <summary>
/// Pins the attribute-reading contract to C++ UI++, which UiSharp must match to
/// be a drop-in replacement for existing configuration files.
///
/// The original reads every attribute through GetXMLAttribute
/// (UI++/Actions/IAction.cpp:21):
///
///     attributeValue = node.attribute(attrName).value();
///     if (attributeValue.GetLength() > 0 &amp;&amp; raw == false)
///         return CTSEnv::Instance().VariableSubstitute(attributeValue);
///     else if (attributeValue.GetLength() > 0) return attributeValue;
///     else if (defaultValue != NULL &amp;&amp; ...) return defaultValue;
///     else return _T("");
///
/// so: values are substituted, defaults are not, and emptiness is judged on the
/// raw value. Only CheckCondition and WarnCondition are ever read raw
/// (InteractiveActions.cpp:222,230).
/// </summary>
public class AttributeSubstitutionTests
{
    private static LocalTSEnv Env(params (string k, string v)[] vars)
    {
        var env = new LocalTSEnv(_ => null);
        foreach (var (k, v) in vars) env.Set(k, v);
        return env;
    }

    private static IReadOnlyList<InputFieldSpec> ParseInput(string inputXml, LocalTSEnv env) =>
        InputFieldParser.Parse(
            XElement.Parse($"<Action Type=\"Input\">{inputXml}</Action>"),
            env, new NativeConditionEvaluator());

    // -------------------------------------------------------------------------
    // Text input
    // -------------------------------------------------------------------------

    [Fact]
    public void Regex_IsSubstituted()
    {
        // The case that proved the bug: UI++2.xml sets SystemNameSuffix then uses
        // RegEx=".{3,5}%SystemNameSuffix%". Before the fix the token survived into
        // the pattern, so the field could never be satisfied.
        var spec = Assert.IsType<InputTextSpec>(Assert.Single(ParseInput(
            """<TextInput Variable="SystemName" Question="Name" RegEx=".{3,5}%SystemNameSuffix%" />""",
            Env(("SystemNameSuffix", "CTG")))));

        Assert.Equal(".{3,5}CTG", spec.Regex);
        Assert.True(spec.Validate("WKSCTG").IsValid);
        Assert.False(spec.Validate("WKSXYZ").IsValid);
    }

    [Theory]
    [InlineData("Hint")]
    [InlineData("Prompt")]
    public void TextAttributes_AreSubstituted(string attribute)
    {
        var spec = Assert.IsType<InputTextSpec>(Assert.Single(ParseInput(
            $"""<TextInput Variable="X" Question="Q" {attribute}="value-%Suffix%" />""",
            Env(("Suffix", "CTG")))));

        var actual = attribute switch
        {
            "Hint"   => spec.Hint,
            "Prompt" => spec.Prompt,
            _        => throw new ArgumentOutOfRangeException(nameof(attribute)),
        };

        Assert.Equal("value-CTG", actual);
    }

    [Fact]
    public void Question_IsSubstituted()
    {
        var spec = Assert.IsType<InputTextSpec>(Assert.Single(ParseInput(
            """<TextInput Variable="X" Question="Name for %Org%" />""",
            Env(("Org", "Coretech")))));

        Assert.Equal("Name for Coretech", spec.Question);
    }

    [Fact]
    public void ForceCaseAndAdValidate_AreSubstituted()
    {
        var spec = Assert.IsType<InputTextSpec>(Assert.Single(ParseInput(
            """<TextInput Variable="X" Question="Q" ForceCase="%Case%" ADValidate="%Mode%" />""",
            Env(("Case", "Upper"), ("Mode", "Computer")))));

        Assert.Equal("Upper", spec.ForceCase);
        Assert.Equal("Computer", spec.AdValidate);

        // ForceCase only works once substituted, since it is compared by value.
        Assert.Equal("ABC", spec.ApplyForceCase("abc"));
    }

    [Fact]
    public void Variable_IsSubstituted()
    {
        var spec = Assert.IsType<InputTextSpec>(Assert.Single(ParseInput(
            """<TextInput Variable="Prefix%Index%" Question="Q" />""",
            Env(("Index", "01")))));

        Assert.Equal("Prefix01", spec.Variable);
    }

    // -------------------------------------------------------------------------
    // Boolean attributes — C++ substitutes before FTW::IsTrue
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("True",  true)]
    [InlineData("False", false)]
    [InlineData("yes",   true)]
    [InlineData("1",     true)]
    public void BoolAttribute_FromVariable_IsHonoured(string value, bool expected)
    {
        var spec = Assert.IsType<InputTextSpec>(Assert.Single(ParseInput(
            """<TextInput Variable="X" Question="Q" Required="%Flag%" />""",
            Env(("Flag", value)))));

        Assert.Equal(expected, spec.Required);
    }

    [Fact]
    public void BoolAttribute_Absent_UsesDeclaredDefault()
    {
        var spec = Assert.IsType<InputTextSpec>(Assert.Single(ParseInput(
            """<TextInput Variable="X" Question="Q" />""", Env())));

        Assert.True(spec.Required);      // Required defaults to true
        Assert.False(spec.Password);     // Password defaults to false
    }

    // -------------------------------------------------------------------------
    // Choices
    // -------------------------------------------------------------------------

    [Fact]
    public void ChoiceOptionValueAndAlternate_AreSubstituted()
    {
        var spec = Assert.IsType<InputChoiceSpec>(Assert.Single(ParseInput(
            """
            <ChoiceInput Variable="Dept" Question="Department">
              <Choice Option="%DeptLabel%" Value="%DeptCode%" AlternateValue="%DeptAlt%" />
            </ChoiceInput>
            """,
            Env(("DeptLabel", "Fire"), ("DeptCode", "FIRE"), ("DeptAlt", "F")))));

        var choice = Assert.Single(spec.Choices);
        Assert.Equal("Fire", choice.Option);
        Assert.Equal("FIRE", choice.Value);
        Assert.Equal("F",    choice.AltValue);
    }

    [Fact]
    public void ChoiceValue_DefaultsToOption_AfterSubstitution()
    {
        // C++ passes the already-substituted option as GetXMLAttribute's default
        // for Value. Defaults are not themselves substituted, so this only works
        // because option was substituted first.
        var spec = Assert.IsType<InputChoiceSpec>(Assert.Single(ParseInput(
            """
            <ChoiceInput Variable="Dept" Question="Department">
              <Choice Option="%DeptLabel%" />
            </ChoiceInput>
            """,
            Env(("DeptLabel", "Fire")))));

        var choice = Assert.Single(spec.Choices);
        Assert.Equal("Fire", choice.Option);
        Assert.Equal("Fire", choice.Value);
    }

    [Fact]
    public void AlternateVariableAndDropDownSize_AreSubstituted()
    {
        var spec = Assert.IsType<InputChoiceSpec>(Assert.Single(ParseInput(
            """
            <ChoiceInput Variable="D" Question="Q" AlternateVariable="Alt%N%" DropDownSize="%Size%">
              <Choice Option="A" />
            </ChoiceInput>
            """,
            Env(("N", "2"), ("Size", "9")))));

        Assert.Equal("Alt2", spec.AltVariable);
        Assert.Equal(9, spec.DropDownSize);
    }

    [Fact]
    public void ChoiceList_IsSubstitutedThenSplit()
    {
        var spec = Assert.IsType<InputChoiceSpec>(Assert.Single(ParseInput(
            """
            <ChoiceInput Variable="Vol" Question="Volume">
              <ChoiceList OptionList="%Options%" ValueList="%Values%" />
            </ChoiceInput>
            """,
            Env(("Options", "Disk 0,Disk 1"), ("Values", "C:,D:")))));

        Assert.Equal(2, spec.Choices.Count);
        Assert.Equal("Disk 0", spec.Choices[0].Option);
        Assert.Equal("C:",     spec.Choices[0].Value);
        Assert.Equal("Disk 1", spec.Choices[1].Option);
        Assert.Equal("D:",     spec.Choices[1].Value);
    }

    // -------------------------------------------------------------------------
    // Checkbox and info
    // -------------------------------------------------------------------------

    [Fact]
    public void CheckboxValues_AreSubstituted()
    {
        var spec = Assert.IsType<InputCheckboxSpec>(Assert.Single(ParseInput(
            """
            <CheckboxInput Variable="Exec" Question="Executive?"
                           CheckedValue="%Yes%" UncheckedValue="%No%" />
            """,
            Env(("Yes", "1"), ("No", "0")))));

        Assert.Equal("1", spec.CheckedValue);
        Assert.Equal("0", spec.UncheckedValue);
    }

    [Fact]
    public void CheckboxValues_Absent_KeepTheirDefaults()
    {
        var spec = Assert.IsType<InputCheckboxSpec>(Assert.Single(ParseInput(
            """<CheckboxInput Variable="Exec" Question="Executive?" />""", Env())));

        Assert.Equal("True",  spec.CheckedValue);
        Assert.Equal("False", spec.UncheckedValue);
    }

    [Fact]
    public void InfoTextColorAndLines_AreSubstituted()
    {
        var spec = Assert.IsType<InputInfoSpec>(Assert.Single(ParseInput(
            """<InputInfo Color="%C%" NumberofLines="%L%">text</InputInfo>""",
            Env(("C", "#FF0000"), ("L", "3")))));

        Assert.Equal("#FF0000", spec.TextColor);
        Assert.Equal(3, spec.NumberOfLines);
    }

    // -------------------------------------------------------------------------
    // GetXMLAttribute's default-value semantics
    // -------------------------------------------------------------------------

    [Fact]
    public void DefaultValue_IsNotSubstituted()
    {
        // In the original a default is always a compile-time constant, never a
        // variable reference, and the substitution branch is skipped when the
        // attribute is absent. A literal %Token% in a default must survive.
        var spec = Assert.IsType<InputTextSpec>(Assert.Single(ParseInput(
            """<TextInput Variable="X" />""", Env(("Question", "should-not-appear")))));

        Assert.Equal(XmlConstants.Defaults.Question, spec.Question);
    }

    [Fact]
    public void PresentButEmptyAttribute_FallsBackToDefault()
    {
        // C++ judges emptiness on the raw value before substituting, so an empty
        // attribute takes the default rather than yielding "".
        var spec = Assert.IsType<InputCheckboxSpec>(Assert.Single(ParseInput(
            """<CheckboxInput Variable="Exec" Question="Q" CheckedValue="" />""", Env())));

        Assert.Equal("True", spec.CheckedValue);
    }

    [Fact]
    public void UnsetVariable_LeavesTokenInPlace()
    {
        // Matching the original: an unresolved reference stays literal rather
        // than collapsing to an empty string.
        var spec = Assert.IsType<InputTextSpec>(Assert.Single(ParseInput(
            """<TextInput Variable="X" Question="Q" Hint="%NeverSet%" />""", Env())));

        Assert.Equal("%NeverSet%", spec.Hint);
    }

    // -------------------------------------------------------------------------
    // The two attributes that must stay raw
    // -------------------------------------------------------------------------

    [Fact]
    public void PreflightConditions_StayRaw()
    {
        // InteractiveActions.cpp:222,230 pass raw=true so these are substituted
        // at evaluation time instead. This must not regress.
        var env = Env(("XHWMemory", "2048"));
        var el = XElement.Parse(
            """
            <Action Type="Preflight">
              <Check Text="Memory" CheckCondition="%XHWMemory% &gt;= 1024"
                     WarnCondition="%XHWMemory% &gt;= 4096" />
            </Action>
            """);

        var check = Assert.Single(
            PreflightEvaluator.ParseChecks(el, env, new NativeConditionEvaluator()));

        Assert.Equal("%XHWMemory% >= 1024", check.CheckCondition);
        Assert.Equal("%XHWMemory% >= 4096", check.WarnCondition);
    }

    [Fact]
    public void PreflightConditions_AreSubstitutedWhenEvaluated()
    {
        var env = Env(("XHWMemory", "2048"));
        var el = XElement.Parse(
            """
            <Action Type="Preflight">
              <Check Text="Memory" CheckCondition="%XHWMemory% &gt;= 1024"
                     WarnCondition="%XHWMemory% &gt;= 4096" />
            </Action>
            """);

        var cond    = new NativeConditionEvaluator();
        var checks  = PreflightEvaluator.ParseChecks(el, env, cond);
        var results = PreflightEvaluator.Evaluate(checks, cond, env);

        // 2048 passes the 1024 check but not the 4096 warn threshold.
        Assert.Equal(PreflightStatus.Warn, Assert.Single(results).Status);
    }

    [Fact]
    public void FieldCondition_IsSubstitutedAtEvaluationTime()
    {
        var specs = ParseInput(
            """
            <TextInput Variable="A" Question="Shown"  Condition="&quot;%Tier%&quot; = &quot;Gold&quot;" />
            <TextInput Variable="B" Question="Hidden" Condition="&quot;%Tier%&quot; = &quot;Bronze&quot;" />
            """,
            Env(("Tier", "Gold")));

        Assert.Equal("Shown", Assert.Single(specs).Question);
    }

    // -------------------------------------------------------------------------
    // Global traits — UI++.cpp:237 reads these through GetXMLAttribute too
    // -------------------------------------------------------------------------

    [Fact]
    public void RootAttributes_AreSubstituted_WhenEnvSupplied()
    {
        var env = Env(("Org", "Coretech"), ("Accent", "#2233DD"));
        var config = ConfigLoader.LoadFromXml(
            """<UIpp Title="%Org% Deployment" Color="%Accent%"><Actions /></UIpp>""", env);

        Assert.Equal("Coretech Deployment", config.GlobalTraits.Title);
        Assert.Equal(0x22, config.GlobalTraits.AccentColor.R);
        Assert.Equal(0x33, config.GlobalTraits.AccentColor.G);
        Assert.Equal(0xDD, config.GlobalTraits.AccentColor.B);
    }

    [Fact]
    public void SoftwareAttributes_AreSubstituted_WhenEnvSupplied()
    {
        var env = Env(("Ver", "2019"));
        var config = ConfigLoader.LoadFromXml(
            """
            <UIpp>
              <Software>
                <Application Id="Reader" Label="Acrobat %Ver%" Name="Adobe Reader %Ver%" />
              </Software>
              <Actions />
            </UIpp>
            """, env);

        var sw = config.Software["Reader"];
        Assert.Equal("Acrobat 2019", sw.Label);
        Assert.Equal("Adobe Reader 2019", sw.GetVariableValue());
    }

    [Fact]
    public void RootAttributes_StayLiteral_WithoutEnv()
    {
        // gUI# loads configs for editing with no task-sequence environment, and
        // must see the author's text rather than a resolved value.
        var config = ConfigLoader.LoadFromXml(
            """<UIpp Title="%Org% Deployment"><Actions /></UIpp>""");

        Assert.Equal("%Org% Deployment", config.GlobalTraits.Title);
    }
}
