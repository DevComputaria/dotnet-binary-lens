using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ClrLens.Analysis;
using ClrLens.IL;
using ClrLens.IR;
using ClrLens.SampleFixtures;
using Xunit;

namespace ClrLens.Tests.Unit;

public sealed class AnalysisPipelineTests
{
    [Fact]
    public void IrBuilder_PreservesOffsetsAndCreatesValues()
    {
        using var context = FixtureContext.Create();
        var body = context.Decode("LinearLoop");
        var ir = new CilToIrBuilder().Build(body);

        Assert.NotEmpty(ir.Nodes);
        Assert.All(ir.Nodes, node => Assert.InRange(node.Offset, 0, int.MaxValue));
        Assert.Contains(ir.Nodes, node => node.Operation == IrOperationKind.Constant && node.Result is not null);
        Assert.Contains(ir.Nodes, node => node.Operation == IrOperationKind.Binary && node.Inputs.Count == 2);
        Assert.True(ir.Diagnostics.All(diagnostic => !diagnostic.IsError));
    }

    [Fact]
    public void ControlFlowGraph_DetectsLoopsAndValidEdges()
    {
        using var context = FixtureContext.Create();
        var body = context.Decode("NestedLoop");
        var graph = new ControlFlowGraphBuilder().Build(body);

        Assert.NotEmpty(graph.Blocks);
        Assert.NotEmpty(graph.NaturalLoops);
        Assert.All(graph.Edges, edge =>
        {
            Assert.Contains(graph.Blocks, block => block.Id == edge.From);
            Assert.Contains(graph.Blocks, block => block.Id == edge.To);
        });
        Assert.All(graph.BackEdges, edge => Assert.True(graph.Dominators[edge.From].Contains(edge.To)));
    }

    [Fact]
    public void ControlFlowGraph_PreservesExceptionalEdges()
    {
        using var context = FixtureContext.Create();
        var body = context.Decode("ExceptionFlow");
        var graph = new ControlFlowGraphBuilder().Build(body);

        Assert.NotEmpty(body.ExceptionRegions);
        Assert.NotEmpty(graph.ExceptionalEdges);
    }

    [Fact]
    public void CallGraph_PreservesExternalEffectsAndCallSiteOffsets()
    {
        using var context = FixtureContext.Create();
        var bodies = context.ReadBodies();
        var graph = CallGraphBuilder.Build(context.Metadata, bodies);

        Assert.NotEmpty(graph.CallSites);
        Assert.NotEmpty(graph.ExternalCalls);
        Assert.All(graph.CallSites, site => Assert.InRange(site.IlOffset, 0, int.MaxValue));
        Assert.All(graph.ExternalCalls, site => Assert.NotEqual(UnknownEffect.None, site.Effects));
        Assert.Equal(bodies.Count, graph.Edges.Count);
    }

    private sealed class FixtureContext : IDisposable
    {
        private readonly FileStream stream;
        private readonly PEReader peReader;
        public MetadataReader Metadata { get; }
        private readonly Dictionary<MethodDefinitionHandle, MethodDefinition> methods;

        private FixtureContext(string path)
        {
            stream = File.OpenRead(path);
            peReader = new PEReader(stream);
            Metadata = peReader.GetMetadataReader();
            methods = Metadata.MethodDefinitions.ToDictionary(handle => handle, Metadata.GetMethodDefinition);
        }

        public static FixtureContext Create() => new(typeof(FixtureMarker).Assembly.Location);

        public DecodedMethodBody Decode(string name)
        {
            var handle = methods.First(pair => Metadata.GetString(pair.Value.Name) == name).Key;
            return new CilDecoder().Decode(peReader, methods[handle]);
        }

        public Dictionary<MethodDefinitionHandle, DecodedMethodBody> ReadBodies()
        {
            var decoder = new CilDecoder();
            return methods
                .Where(pair => pair.Value.RelativeVirtualAddress != 0)
                .ToDictionary(pair => pair.Key, pair => decoder.Decode(peReader, pair.Value));
        }

        public void Dispose()
        {
            peReader.Dispose();
            stream.Dispose();
        }
    }
}
