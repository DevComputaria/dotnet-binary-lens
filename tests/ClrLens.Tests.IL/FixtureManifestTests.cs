using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using ClrLens.Analysis;
using ClrLens.IR;
using ClrLens.PE;
using ClrLens.SampleFixtures;

namespace ClrLens.Tests.IL;

public sealed class FixtureManifestTests
{
    [Fact]
    public void Manifest_ExistsAndMatchesFixtureShape()
    {
        Assert.True(File.Exists(TestSupport.ManifestPath));

        using var document = JsonDocument.Parse(File.ReadAllText(TestSupport.ManifestPath));
        var assemblyNode = document.RootElement.GetProperty("assemblies").EnumerateArray().First(e => e.GetProperty("project").GetString() == "ClrLens.SampleFixtures");

        Assert.Equal("0.1", document.RootElement.GetProperty("schemaVersion").GetString());
        Assert.Equal(64, assemblyNode.GetProperty("expectedHash").GetString()!.Length);

        using var stream = File.OpenRead(TestSupport.FixtureAssemblyPath);
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();
        var typeNames = metadata.TypeDefinitions
            .Select(h => metadata.GetTypeDefinition(h))
            .Select(t => $"{metadata.GetString(t.Namespace)}.{metadata.GetString(t.Name)}")
            .ToHashSet(StringComparer.Ordinal);
        var methodNames = metadata.MethodDefinitions
            .Select(h => metadata.GetMethodDefinition(h))
            .Select(m => metadata.GetString(m.Name))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var requiredType in assemblyNode.GetProperty("mustContainTypes").EnumerateArray().Select(e => e.GetString()!))
        {
            Assert.Contains(requiredType, typeNames);
        }

        foreach (var requiredMethod in assemblyNode.GetProperty("mustContainMethods").EnumerateArray().Select(e => e.GetString()!))
        {
            Assert.Contains(requiredMethod, methodNames);
        }

        Assert.True(File.Exists(TestSupport.InvalidMetadataFixturePath));
    }

    [Fact]
    public async Task FixtureHash_IsDeterministic_AndStaticInspectionDoesNotExecuteFixtureMethods()
    {
        Assert.Equal(0, RetentionPatterns.CacheCount);

        var bytes = await File.ReadAllBytesAsync(TestSupport.FixtureAssemblyPath);
        var hashA = Convert.ToHexString(SHA256.HashData(bytes));
        var hashB = Convert.ToHexString(SHA256.HashData(bytes));

        Assert.Equal(hashA, hashB);

        var ingestion = await new AssemblyReader().ReadAsync(TestSupport.FixtureAssemblyPath);
        Assert.True(ingestion.IsSuccess);

        using var stream = File.OpenRead(TestSupport.FixtureAssemblyPath);
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();
        var bodies = TestSupport.DecodeAllMethods(peReader, metadata);

        var irBuilder = new CilToIrBuilder();
        var cfgBuilder = new ControlFlowGraphBuilder();
        foreach (var body in bodies.Values)
        {
            _ = irBuilder.Build(body);
            _ = cfgBuilder.Build(body);
        }

        _ = CallGraphBuilder.Build(metadata, bodies);

        Assert.Equal(0, RetentionPatterns.CacheCount);
    }
}
