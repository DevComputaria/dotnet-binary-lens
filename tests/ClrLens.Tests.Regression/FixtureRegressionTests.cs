using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ClrLens.Analysis;
using ClrLens.IL;
using ClrLens.IR;
using ClrLens.PE;
using ClrLens.SampleFixtures;

namespace ClrLens.Tests.Regression;

public sealed class FixtureRegressionTests
{
    [Fact]
    public async Task HarnessFlow_AnalyzesFixtureWithoutExecutingFixtureMethods()
    {
        Assert.Equal(0, RetentionPatterns.CacheCount);

        var ingestion = await new AssemblyReader().ReadAsync(TestSupport.FixtureAssemblyPath);
        Assert.True(ingestion.IsSuccess, string.Join(";", ingestion.Diagnostics.Select(d => d.Message)));

        using var stream = File.OpenRead(TestSupport.FixtureAssemblyPath);
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();
        var decoder = new CilDecoder();
        var irBuilder = new CilToIrBuilder();
        var cfgBuilder = new ControlFlowGraphBuilder();
        var bodies = new Dictionary<System.Reflection.Metadata.MethodDefinitionHandle, DecodedMethodBody>();

        foreach (var handle in metadata.MethodDefinitions)
        {
            var method = metadata.GetMethodDefinition(handle);
            if (method.RelativeVirtualAddress == 0)
            {
                continue;
            }

            var decoded = decoder.Decode(peReader, method);
            bodies[handle] = decoded;
            _ = irBuilder.Build(decoded);
            _ = cfgBuilder.Build(decoded);
        }

        var graph = CallGraphBuilder.Build(metadata, bodies);
        Assert.NotEmpty(graph.CallSites);
        Assert.Equal(0, RetentionPatterns.CacheCount);
    }
}
