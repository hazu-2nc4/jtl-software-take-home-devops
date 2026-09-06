using JtlDemo.Modules.Windows;
using Xunit;

namespace JtlDemo.Windows.CompositionTests;

public class CompositionTests
{
    [Fact]
    public void Windows_catalog_composes_only_the_excluded_modules()
    {
        var names = WindowsApiModuleCatalog.BuildApiModules().Select(module => module.Name).ToArray();

        Assert.Equal(["Documents", "Printers"], names);
    }
}
