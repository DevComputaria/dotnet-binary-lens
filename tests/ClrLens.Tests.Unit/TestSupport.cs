using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ClrLens.Core.Domain;
using ClrLens.SampleFixtures;

namespace ClrLens.Tests.Unit;

internal static class TestSupport
{
    public static string FixtureAssemblyPath => typeof(FixtureMarker).Assembly.Location;

    public static string RepoRoot
    {
        get
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "ClrLens.sln")))
                    return current.FullName;
                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repository root.");
        }
    }


    public static AnalysisReport CreateValidReport() => new(
        "0.1",
        new AssemblyIdentity("sample", new string('A', 64), "net8.0", "Amd64", "1.0.0.0"),
        new Provenance("1.0.0", "runtime", DateTimeOffset.Parse("2026-01-01T00:00:00Z"), new string('B', 64)),
        new AnalysisScenarioConfiguration(AnalysisScenario.Baseline, AnalysisCostMode.Typical),
        new AnalysisSummary(1, 2, 0, 0, 0.1, 0.1, 0),
        [],
        [new BoundContract("S", 1, "bytes", "spec", "global", ContractTrust.External, DateOnly.Parse("2099-12-31"), "team", "T-1", EvidenceKind.ExternalContract)],
        [new Suppression("sup-1", "F001", "known", "team", "repo", DateOnly.Parse("2099-12-31"), "T-2")]);

    public static (PEReader peReader, MetadataReader metadata) OpenFixtureMetadata()
    {
        var stream = File.OpenRead(FixtureAssemblyPath);
        var peReader = new PEReader(stream);
        return (peReader, peReader.GetMetadataReader());
    }


    public static ClrLens.IL.DecodedMethodBody DecodeFixtureMethod(string methodName)
    {
        using var stream = File.OpenRead(FixtureAssemblyPath);
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();
        var decoder = new ClrLens.IL.CilDecoder();

        foreach (var handle in metadata.MethodDefinitions)
        {
            var definition = metadata.GetMethodDefinition(handle);
            if (definition.RelativeVirtualAddress == 0)
            {
                continue;
            }

            if (metadata.GetString(definition.Name) == methodName)
            {
                return decoder.Decode(peReader, definition);
            }
        }

        throw new InvalidOperationException($"Method '{methodName}' not found in fixture assembly.");
    }

}
