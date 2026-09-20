using ClrLens.IL;
using ClrLens.PE;
using ClrLens.SampleFixtures;
using System.Reflection.Metadata;
using Xunit;

namespace ClrLens.Tests.Regression;

public sealed class FixtureRegressionTests
{
    [Fact]
    public async Task SampleFixture_IsIngestedWithoutExecutingMethods()
    {
        var result = await new AssemblyReader().ReadAsync(typeof(FixtureMarker).Assembly.Location);

        Assert.True(result.IsSuccess, string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Assert.NotNull(result.Model);
        Assert.Equal("ClrLens.SampleFixtures", result.Model!.Identity.Name);
        Assert.True(result.Model.TypeCount >= 6);
        Assert.True(result.Model.MethodCount >= 18);
    }

    [Fact]
    public async Task InvalidFixture_ReturnsStructuredDiagnostic()
    {
        var path = FindRepositoryFile("tests/fixtures/unsupported/invalid-metadata.bin");
        var result = await new AssemblyReader().ReadAsync(path);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code is AssemblyDiagnosticCode.InvalidPe or AssemblyDiagnosticCode.MissingMetadata);
    }

    [Fact]
    public void FixtureMethodsDecodeWithValidBranches()
    {
        using var stream = File.OpenRead(typeof(FixtureMarker).Assembly.Location);
        using var peReader = new System.Reflection.PortableExecutable.PEReader(stream);
        var metadata = peReader.GetMetadataReader();
        var decoder = new CilDecoder();
        var methods = metadata.MethodDefinitions
            .Select(metadata.GetMethodDefinition)
            .Where(method => method.RelativeVirtualAddress != 0)
            .Select(method => decoder.Decode(peReader, method))
            .ToArray();

        Assert.NotEmpty(methods);
        Assert.All(methods, method => Assert.DoesNotContain(method.Diagnostics, diagnostic => diagnostic.Code == CilDiagnosticCode.InvalidBranchTarget));
    }

    private static string FindRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository fixture '{relativePath}'.");
    }
}
