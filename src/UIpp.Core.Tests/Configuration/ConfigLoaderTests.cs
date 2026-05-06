using System.Drawing;
using UIpp.Core.Configuration;
using UIpp.Core.Dialogs;

namespace UIpp.Core.Tests.Configuration;

public class ConfigLoaderTests
{
    // Writes an XML string to a temp file, invokes the test, then cleans up.
    private static LoadedConfig LoadXml(string xml)
    {
        var path = Path.GetTempFileName() + ".xml";
        File.WriteAllText(path, xml);
        try   { return ConfigLoader.Load(path); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_MinimalXml_ReturnsDefaults()
    {
        var cfg = LoadXml("<UIpp />");
        Assert.Equal("UI++",   cfg.GlobalTraits.Title);
        Assert.Equal("Tahoma", cfg.GlobalTraits.FontFace);
        Assert.True(cfg.GlobalTraits.ShowIcons);
        Assert.True(cfg.GlobalTraits.ShowSidebar);
        Assert.True(cfg.GlobalTraits.AlwaysOnTop);
        Assert.False(cfg.GlobalTraits.Flat);
        Assert.Equal(XmlConstants.Values.ConditionEngineNative, cfg.ConditionEngine);
        Assert.Empty(cfg.Software);
    }

    [Fact]
    public void Load_CustomTitle_SetInTraits()
    {
        var cfg = LoadXml("""<UIpp Title="Contoso Setup" />""");
        Assert.Equal("Contoso Setup", cfg.GlobalTraits.Title);
    }

    [Fact]
    public void Load_FlatTrue_SetInFlags()
    {
        var cfg = LoadXml("""<UIpp Flat="True" />""");
        Assert.True(cfg.GlobalTraits.Flat);
    }

    [Fact]
    public void Load_ShowIconsFalse_ClearsFlag()
    {
        var cfg = LoadXml("""<UIpp DialogIcons="False" />""");
        Assert.False(cfg.GlobalTraits.ShowIcons);
    }

    [Fact]
    public void Load_AccentColor_ParsedCorrectly()
    {
        var cfg = LoadXml("""<UIpp Color="#FF0000" />""");
        Assert.Equal(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00), cfg.GlobalTraits.AccentColor);
    }

    [Fact]
    public void Load_ConditionEngineVbscript_SetInConfig()
    {
        var cfg = LoadXml("""<UIpp ConditionEngine="vbscript" />""");
        Assert.Equal("vbscript", cfg.ConditionEngine);
    }

    [Fact]
    public void Load_Software_ParsesApplicationAndPackage()
    {
        const string xml = """
            <UIpp>
              <Software>
                <Application Id="App1" Label="My App" Name="CM Application" />
                <Package     Id="Pkg1" Label="My Pkg" PkgID="ABC00001" ProgramName="Install" />
              </Software>
            </UIpp>
            """;
        var cfg = LoadXml(xml);

        Assert.Equal(2, cfg.Software.Count);

        var app = cfg.Software["App1"];
        Assert.Equal("Application",    app.Type);
        Assert.Equal("My App",         app.Label);
        Assert.Equal("CM Application", app.GetVariableValue());

        var pkg = cfg.Software["Pkg1"];
        Assert.Equal("Package",   pkg.Type);
        Assert.Equal("My Pkg",    pkg.Label);
        Assert.Equal("ABC00001",  pkg.GetVariableValue());
    }

    [Fact]
    public void Load_Software_SkipsNoIdEntries()
    {
        const string xml = """
            <UIpp>
              <Software>
                <Application Label="No ID here" Name="X" />
                <Application Id="Good" Label="Has ID" Name="Y" />
              </Software>
            </UIpp>
            """;
        var cfg = LoadXml(xml);
        Assert.Single(cfg.Software);
        Assert.True(cfg.Software.ContainsKey("Good"));
    }

    [Fact]
    public void Load_Software_OrderIndexPreserved()
    {
        const string xml = """
            <UIpp>
              <Software>
                <Application Id="A" Label="A" Name="A" />
                <Package     Id="B" Label="B" PkgID="B" ProgramName="X" />
              </Software>
            </UIpp>
            """;
        var cfg = LoadXml(xml);
        Assert.Equal(0, cfg.Software["A"].OrderIndex);
        Assert.Equal(1, cfg.Software["B"].OrderIndex);
    }

    [Fact]
    public void Load_WrongRootElement_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => LoadXml("<Config />"));
    }

    [Fact]
    public void Load_NoSoftwareElement_ReturnsEmptyDict()
    {
        var cfg = LoadXml("<UIpp><Actions /></UIpp>");
        Assert.Empty(cfg.Software);
    }
}
