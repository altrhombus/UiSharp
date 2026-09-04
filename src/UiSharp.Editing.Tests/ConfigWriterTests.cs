using System.Drawing;
using System.Xml.Linq;
using UiSharp.Core.Configuration;
using UiSharp.Editing;
using UiSharp.Core.Dialogs;
using UiSharp.Core.Software;

namespace UiSharp.Editing.Tests;

public class ConfigWriterTests
{
    private static EditorConfig RoundTrip(EditorConfig config)
    {
        var path = Path.GetTempFileName() + ".xml";
        try
        {
            ConfigWriter.Save(config, path);
            return EditorConfig.FromLoaded(ConfigLoader.Load(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Write_MinimalConfig_RootElementIsUiSharp()
    {
        var doc = ConfigWriter.Write(new EditorConfig());
        Assert.Equal(XmlConstants.Elements.Root, doc.Root?.Name.LocalName);
    }

    [Fact]
    public void RoundTrip_DefaultTraits_PreservesTitle()
    {
        var config = new EditorConfig
        {
            GlobalTraits = new DialogTraits { Title = "Test Setup" }
        };
        var result = RoundTrip(config);
        Assert.Equal("Test Setup", result.GlobalTraits.Title);
    }

    [Fact]
    public void RoundTrip_CustomColor_PreservesColor()
    {
        var config = new EditorConfig
        {
            GlobalTraits = new DialogTraits
            {
                AccentColor = Color.FromArgb(0xFF, 0x11, 0x22, 0x33)
            }
        };
        var result = RoundTrip(config);
        Assert.Equal(0x11, result.GlobalTraits.AccentColor.R);
        Assert.Equal(0x22, result.GlobalTraits.AccentColor.G);
        Assert.Equal(0x33, result.GlobalTraits.AccentColor.B);
    }

    [Fact]
    public void RoundTrip_FlatTrue_Preserved()
    {
        var config = new EditorConfig
        {
            GlobalTraits = new DialogTraits { Flags = DialogTraitFlags.Flat | DialogTraitFlags.Default }
        };
        var result = RoundTrip(config);
        Assert.True(result.GlobalTraits.Flat);
    }

    [Fact]
    public void RoundTrip_ConditionEngineVbscript_Preserved()
    {
        var config = new EditorConfig
        {
            ConditionEngine = XmlConstants.Values.ConditionEngineVbscript
        };
        var result = RoundTrip(config);
        Assert.Equal(XmlConstants.Values.ConditionEngineVbscript, result.ConditionEngine);
    }

    [Fact]
    public void RoundTrip_NoSchemaVersion_RemainsNull()
    {
        var config = new EditorConfig { SchemaVersion = null };
        var result = RoundTrip(config);
        Assert.Null(result.SchemaVersion);
    }

    [Fact]
    public void RoundTrip_SchemaVersionSet_Preserved()
    {
        var config = new EditorConfig { SchemaVersion = 1 };
        var result = RoundTrip(config);
        Assert.Equal(1, result.SchemaVersion);
    }

    [Fact]
    public void RoundTrip_Application_Preserved()
    {
        var app = new Application("app-001", "Adobe Reader", "", "Adobe Reader XI", "", "", 0);
        var config = new EditorConfig { SoftwareList = [app] };

        var result = RoundTrip(config);

        var sw = result.SoftwareList.Single(s => s.Id == "app-001");
        var resultApp = Assert.IsType<Application>(sw);
        Assert.Equal("Adobe Reader", resultApp.Label);
        Assert.Equal("Adobe Reader XI", resultApp.AppName);
    }

    [Fact]
    public void RoundTrip_Package_Preserved()
    {
        var pkg = new Package("pkg-001", ".NET 4.5", "", "ONE000100", "Install .Net 4.5", "", "", 0);
        var config = new EditorConfig { SoftwareList = [pkg] };

        var result = RoundTrip(config);

        var sw = result.SoftwareList.Single(s => s.Id == "pkg-001");
        var resultPkg = Assert.IsType<Package>(sw);
        Assert.Equal(".NET 4.5", resultPkg.Label);
        Assert.Equal("ONE000100", resultPkg.PkgId);
        Assert.Equal("Install .Net 4.5", resultPkg.ProgramName);
    }

    [Fact]
    public void RoundTrip_TSVarAction_PreservesNode()
    {
        var node = new XElement(XmlConstants.Elements.Action,
            new XAttribute(XmlConstants.Attributes.Type, XmlConstants.ActionTypes.TSVar),
            new XAttribute(XmlConstants.Attributes.Variable, "OSDComputerName"),
            "DEFAULT-PC");

        var config = new EditorConfig
        {
            Actions = [new ActionNodeModel { Node = node }]
        };

        var path = Path.GetTempFileName() + ".xml";
        try
        {
            ConfigWriter.Save(config, path);
            var doc = XDocument.Load(path);
            var actionEl = doc.Root?
                .Element(XmlConstants.Elements.Actions)?
                .Element(XmlConstants.Elements.Action);

            Assert.NotNull(actionEl);
            Assert.Equal(XmlConstants.ActionTypes.TSVar,
                (string?)actionEl.Attribute(XmlConstants.Attributes.Type));
            Assert.Equal("OSDComputerName",
                (string?)actionEl.Attribute(XmlConstants.Attributes.Variable));
            Assert.Equal("DEFAULT-PC", actionEl.Value);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void RoundTrip_ActionGroup_ChildrenPreserved()
    {
        var child = new XElement(XmlConstants.Elements.Action,
            new XAttribute(XmlConstants.Attributes.Type, XmlConstants.ActionTypes.TSVar),
            new XAttribute(XmlConstants.Attributes.Variable, "Site"),
            "CHI");

        var groupNode = new XElement(XmlConstants.Elements.ActionGroup,
            new XAttribute(XmlConstants.Attributes.Name, "MyGroup"));

        var group = new ActionNodeModel
        {
            Node = groupNode,
            Children = { new ActionNodeModel { Node = child } }
        };

        var config = new EditorConfig { Actions = [group] };

        var path = Path.GetTempFileName() + ".xml";
        try
        {
            ConfigWriter.Save(config, path);
            var doc = XDocument.Load(path);
            var groupEl = doc.Root?
                .Element(XmlConstants.Elements.Actions)?
                .Element(XmlConstants.Elements.ActionGroup);

            Assert.NotNull(groupEl);
            Assert.Equal("MyGroup", (string?)groupEl.Attribute(XmlConstants.Attributes.Name));
            Assert.Single(groupEl.Elements());
        }
        finally { File.Delete(path); }
    }
}
