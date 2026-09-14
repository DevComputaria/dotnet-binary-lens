using ClrLens.Core.Domain;
using ClrLens.SampleFixtures;

namespace ClrLens.Tests.Regression;

internal static class TestSupport
{
    public static string FixtureAssemblyPath => typeof(FixtureMarker).Assembly.Location;

    public static AnalysisReport CreateReport() => new(
        "0.1",
        new AssemblyIdentity("regression", new string('C', 64), "net8.0", "Amd64", "1.0.0.0"),
        new Provenance("1.0.0", "runtime", DateTimeOffset.Parse("2026-01-01T00:00:00Z"), new string('D', 64)),
        new AnalysisScenarioConfiguration(AnalysisScenario.Baseline, AnalysisCostMode.Typical),
        new AnalysisSummary(10, 50, 2, 1, 0.5, 0.3, 1),
        [
            new Finding("F-1", "cpu", Severity.Medium, 0.9,
                new AnalysisLocation("Type", "Method", 123, 0, 10, ["B0"], ["Type.Method"]),
                EvidenceKind.Inferred,
                RemediationType.Review,
                "Title",
                "Description",
                "Why",
                new CostModel("O(n)", "linear", "ops", new Dictionary<string, string> { ["n"] = "size" }),
                [],
                ["Do X"])
        ],
        [new BoundContract("S", 10, "bytes", "spec", "scope", ContractTrust.External, DateOnly.Parse("2099-01-01"), "owner", "ticket", EvidenceKind.ExternalContract)],
        [new Suppression("SUP-1", "F-1", "reason", "owner", "scope", DateOnly.Parse("2099-01-01"), "ticket")]);
}
