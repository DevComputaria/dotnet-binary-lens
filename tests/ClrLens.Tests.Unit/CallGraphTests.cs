using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using ClrLens.Analysis;
using ClrLens.IL;

namespace ClrLens.Tests.Unit;

public sealed class CallGraphTests
{
    [Fact]
    public void Build_PreservesCallSitesResolutionsAndGraphNodes()
    {
        using var stream = File.OpenRead(TestSupport.FixtureAssemblyPath);
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();
        var decoder = new CilDecoder();
        var bodies = new Dictionary<MethodDefinitionHandle, DecodedMethodBody>();

        foreach (var handle in metadata.MethodDefinitions)
        {
            var method = metadata.GetMethodDefinition(handle);
            if (method.RelativeVirtualAddress == 0)
            {
                continue;
            }

            bodies[handle] = decoder.Decode(peReader, method);
        }

        var graph = CallGraphBuilder.Build(metadata, bodies);

        Assert.NotEmpty(graph.CallSites);
        Assert.All(graph.CallSites, site =>
        {
            Assert.True(site.IlOffset >= 0);
            Assert.NotEqual(CallResolutionKind.Unknown, site.Resolution);
        });
        Assert.NotEmpty(graph.InternalCalls);
        Assert.NotEmpty(graph.ExternalCalls);
        Assert.All(graph.ExternalCalls, site => Assert.NotEqual(UnknownEffect.None, site.Effects));
        Assert.Equal(bodies.Count, graph.Edges.Count);

        var methodSpecTokens = bodies.Values
            .SelectMany(body => body.Instructions)
            .Where(i => i.Operand is int)
            .Select(i => MetadataTokens.EntityHandle((int)i.Operand!))
            .Count(handle => handle.Kind == HandleKind.MethodSpecification);

        if (methodSpecTokens > 0)
        {
            Assert.True(graph.CallSites.Count(site => site.Resolution == CallResolutionKind.ExternalMethodSpecification) >= methodSpecTokens);
        }
    }
}
