using ClrLens.PE;
using ClrLens.SampleFixtures;
using Xunit;

namespace ClrLens.Tests.Unit;

public sealed class AssemblyReaderUnitTests
{
    [Fact]
    public async Task ReadsManagedAssemblyMetadataWithoutExecutingIt()
    {
        var result = await new AssemblyReader().ReadAsync(typeof(FixtureMarker).Assembly.Location);

        Assert.True(result.IsSuccess, string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Assert.NotNull(result.Model);
        var model = result.Model!;
        Assert.Equal("ClrLens.SampleFixtures", model.Identity.Name);
        Assert.Equal(64, model.Identity.Sha256.Length);
        Assert.True(model.HasMetadata);
        Assert.True(model.TypeCount >= 6);
        Assert.True(model.MethodCount >= 18);
        Assert.NotEmpty(model.AssemblyReferences);
    }

    [Fact]
    public async Task MissingAssemblyProducesStructuredDiagnostic()
    {
        var result = await new AssemblyReader().ReadAsync(Path.Combine(Path.GetTempPath(), "missing-clrlens.dll"));

        Assert.False(result.IsSuccess);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(AssemblyDiagnosticCode.FileNotFound, diagnostic.Code);
        Assert.True(diagnostic.IsError);
    }

    [Fact]
    public async Task FileSizeLimitIsEnforced()
    {
        var path = typeof(FixtureMarker).Assembly.Location;
        var result = await new AssemblyReader().ReadAsync(path, new AssemblyIngestionOptions(MaxFileSizeBytes: 1));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == AssemblyDiagnosticCode.FileTooLarge);
    }
}
