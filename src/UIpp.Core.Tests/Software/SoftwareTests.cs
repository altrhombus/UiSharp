using UIpp.Core.Software;

namespace UIpp.Core.Tests.Software;

public class SoftwareTests
{
    [Fact]
    public void Application_Type_IsApplication()
    {
        var app = new Application("id1", "Label", "Info", "AppName", "", "", 0);
        Assert.Equal("Application", app.Type);
    }

    [Fact]
    public void Application_GetVariableValue_ReturnsAppName()
    {
        var app = new Application("id1", "Label", "Info", "My App", "", "", 0);
        Assert.Equal("My App", app.GetVariableValue());
    }

    [Fact]
    public void Application_Properties_RoundTrip()
    {
        var app = new Application("A01", "Label A", "Info A", "App A", "X01", "X02", 3);
        Assert.Equal("A01",     app.Id);
        Assert.Equal("Label A", app.Label);
        Assert.Equal("Info A",  app.Info);
        Assert.Equal("App A",   app.AppName);
        Assert.Equal("X01",     app.IncludeIds);
        Assert.Equal("X02",     app.ExcludeIds);
        Assert.Equal(3,         app.OrderIndex);
    }

    [Fact]
    public void Package_Type_IsPackage()
    {
        var pkg = new Package("id2", "Label", "Info", "PKG00001", "Install", "", "", 0);
        Assert.Equal("Package", pkg.Type);
    }

    [Fact]
    public void Package_GetVariableValue_ReturnsPkgId()
    {
        var pkg = new Package("id2", "Label", "Info", "PKG00001", "Install", "", "", 0);
        Assert.Equal("PKG00001", pkg.GetVariableValue());
    }

    [Fact]
    public void Package_Properties_RoundTrip()
    {
        var pkg = new Package("P01", "Label P", "Info P", "ABC00001", "Install", "Y01", "Y02", 5);
        Assert.Equal("P01",      pkg.Id);
        Assert.Equal("Label P",  pkg.Label);
        Assert.Equal("Info P",   pkg.Info);
        Assert.Equal("ABC00001", pkg.PkgId);
        Assert.Equal("Install",  pkg.ProgramName);
        Assert.Equal("Y01",      pkg.IncludeIds);
        Assert.Equal("Y02",      pkg.ExcludeIds);
        Assert.Equal(5,          pkg.OrderIndex);
    }
}
