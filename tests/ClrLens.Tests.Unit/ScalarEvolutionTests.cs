using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ClrLens.Analysis;
using ClrLens.IL;
using ClrLens.SampleFixtures;
using Xunit;

namespace ClrLens.Tests.Unit;

public sealed class ScalarEvolutionTests
{
    [Fact]
    public void LinearLoopProducesSymbolicRecurrenceAndTerminationAssumption()
    {
        using var context = FixtureContext.Create();
        var body = context.Decode("LinearLoop");
        var graph = new ControlFlowGraphBuilder().Build(body);
        var result = Assert.Single(ScalarEvolutionAnalyzer.Analyze(body, graph));

        Assert.NotEmpty(result.Recurrences);
        Assert.Contains("{", result.Recurrences[0].Expression, StringComparison.Ordinal);
        Assert.Equal(TerminationState.ProvenUnderAssumptions, result.Termination);
    }

    [Fact]
    public void UnknownProgressRemainsUnknownInsteadOfNonTerminationProof()
    {
        using var context = FixtureContext.Create();
        var body = context.Decode("CreateDelegate");
        var graph = new ControlFlowGraphBuilder().Build(body);
        var results = ScalarEvolutionAnalyzer.Analyze(body, graph);

        Assert.All(results, result => Assert.NotEqual(TerminationState.NonTerminationProven, result.Termination));
    }

    private sealed class FixtureContext : IDisposable
    {
        private readonly FileStream stream;
        private readonly PEReader reader;
        private readonly MetadataReader metadata;
        private readonly Dictionary<string, MethodDefinition> methods;

        private FixtureContext(string path)
        {
            stream = File.OpenRead(path);
            reader = new PEReader(stream);
            metadata = reader.GetMetadataReader();
            methods = metadata.MethodDefinitions
                .Select(handle => metadata.GetMethodDefinition(handle))
                .GroupBy(method => metadata.GetString(method.Name), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        }

        public static FixtureContext Create() => new(typeof(FixtureMarker).Assembly.Location);

        public DecodedMethodBody Decode(string name) => new CilDecoder().Decode(reader, methods[name]);

        public void Dispose()
        {
            reader.Dispose();
            stream.Dispose();
        }
    }
}
