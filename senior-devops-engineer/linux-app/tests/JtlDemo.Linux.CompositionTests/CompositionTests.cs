using JtlDemo.Rest.Server;
using Xunit;

namespace JtlDemo.Linux.CompositionTests;

public class CompositionTests
{
    [Fact]
    public void Catalog_composes_only_the_portable_modules()
    {
        var names = ApiModuleCatalog.BuildApiModules().Select(module => module.Name).ToArray();

        Assert.Equal(["Items", "Customers", "Stats"], names);
    }

    [Fact]
    public void Stats_module_survives_the_linux_slice()
    {
        Assert.Equal(42, StatsModule.Value);
    }
}
