using ClrLens.PE;

namespace ClrLens.Tests.IL;

public sealed class AssemblyReaderTests
{
    [Fact]
    public async Task ReadAsync_ValidAssemblyProducesModelWithMetadata()
    {
        var result = await new AssemblyReader().ReadAsync(TestSupport.FixtureAssemblyPath);

        Assert.True(result.IsSuccess, string.Join(";", result.Diagnostics.Select(d => d.Message)));
        var model = Assert.IsType<AssemblyModel>(result.Model);
        Assert.Equal("ClrLens.SampleFixtures", model.Identity.Name);
        Assert.Equal(64, model.Identity.Sha256.Length);
        Assert.True(model.TypeCount > 0);
        Assert.True(model.MethodCount > 0);
        Assert.True(model.HasMetadata);
        Assert.Equal(model.AssemblyReferences.OrderBy(x => x, StringComparer.Ordinal), model.AssemblyReferences);
    }
}
