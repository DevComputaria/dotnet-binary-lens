using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ClrLens.Analysis;
using ClrLens.IL;
using ClrLens.Rules;
using ClrLens.SampleFixtures;
using Xunit;

namespace ClrLens.Tests.Unit;

public sealed class StructuralFindingsTests
{
    [Fact]
    public void AnalyzerProducesActionableFindingsForFixtureMethods()
    {
        using var context = FixtureContext.Create();
        var analyzer = new StructuralFindingsAnalyzer();
        var findings = new List<ClrLens.Core.Domain.Finding>();

        foreach (var entry in context.Bodies)
        {
            var method = context.Metadata.GetMethodDefinition(entry.Key);
            var name = context.Metadata.GetString(method.Name);
            var graph = new ControlFlowGraphBuilder().Build(entry.Value);
            var calls = context.CallGraph.CallSites.Where(site => site.Caller == entry.Key).ToArray();
            findings.AddRange(analyzer.Analyze(new StructuralMethodContext(entry.Key, name, entry.Value, graph, calls)));
        }

        Assert.NotEmpty(findings);
        Assert.All(findings, finding =>
        {
            Assert.False(string.IsNullOrWhiteSpace(finding.Id));
            Assert.InRange(finding.Confidence, 0, 1);
            Assert.NotNull(finding.Location);
            Assert.NotEqual(ClrLens.Core.Domain.RemediationType.AutoFix, finding.Remediation);
        });
        Assert.Contains(findings, finding => finding.Id == "CPU001");
    }

    [Fact]
    public void SarifSerializerProducesSarifDocument()
    {
        var finding = new ClrLens.Core.Domain.Finding(
            "CPU005", "CPU", ClrLens.Core.Domain.Severity.Medium, 0.9,
            new ClrLens.Core.Domain.AnalysisLocation("Fixture", "Method", 1, 4, 8, [], []),
            ClrLens.Core.Domain.EvidenceKind.Inferred,
            ClrLens.Core.Domain.RemediationType.Review,
            "Repeated call", "Call in loop", "Cost is multiplied", null, [], []);

        var json = ClrLens.Reporting.SarifReportSerializer.Serialize([finding]);

        Assert.Contains("sarif-2.1.0", json, StringComparison.Ordinal);
        Assert.Contains("CPU005", json, StringComparison.Ordinal);
        Assert.Contains("Inferred", json, StringComparison.Ordinal);
    }

    private sealed class FixtureContext : IDisposable
    {
        private readonly FileStream stream;
        private readonly PEReader peReader;
        public MetadataReader Metadata { get; }
        public Dictionary<MethodDefinitionHandle, DecodedMethodBody> Bodies { get; }
        public CallGraph CallGraph { get; }

        private FixtureContext(string path)
        {
            stream = File.OpenRead(path);
            peReader = new PEReader(stream);
            Metadata = peReader.GetMetadataReader();
            var decoder = new CilDecoder();
            Bodies = Metadata.MethodDefinitions
                .Select(handle => (handle, method: Metadata.GetMethodDefinition(handle)))
                .Where(item => item.method.RelativeVirtualAddress != 0)
                .ToDictionary(item => item.handle, item => decoder.Decode(peReader, item.method));
            CallGraph = CallGraphBuilder.Build(Metadata, Bodies);
        }

        public static FixtureContext Create() => new(typeof(FixtureMarker).Assembly.Location);

        public void Dispose()
        {
            peReader.Dispose();
            stream.Dispose();
        }
    }
}
