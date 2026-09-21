using ClrLens.Core.Domain;
using ClrLens.Core.Serialization;
using ClrLens.Core.Validation;
using ClrLens.Reporting;
using Xunit;

namespace ClrLens.Tests.Unit;

public sealed class DomainContractTests
{
    [Fact]
    public void ExternalContract_IsSerializedDeterministically()
    {
        var report = CreateReport(new BoundContract(
            "payloadBytes", 2_097_152, "bytes", "api-gateway", "Endpoint.Process",
            ContractTrust.External, new DateOnly(2027, 1, 1), "platform", "REL-1"));

        var first = AnalysisReportSerializer.Serialize(report);
        var second = AnalysisReportSerializer.Serialize(report);

        Assert.Equal(first, second);
        Assert.Contains("externalContract", first, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidHashAndExternalEvidence_AreRejected()
    {
        var contract = new BoundContract("payload", -1, "bytes", "", "Endpoint", ContractTrust.External, new DateOnly(2025, 1, 1), Evidence: EvidenceKind.Proven);
        var report = CreateReport(contract) with
        {
            Assembly = new AssemblyIdentity("Sample", "invalid", "net8.0", "AnyCPU")
        };

        var errors = DomainContractValidator.Validate(report, new DateOnly(2026, 9, 13));

        Assert.Contains(errors, error => error.Contains("sha256", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("negative", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("expired", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("ExternalContract", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_PreservesAssemblyHashAndToolVersion()
    {
        var report = CreateReport();
        var restored = AnalysisReportSerializer.Deserialize(AnalysisReportSerializer.Serialize(report));

        Assert.Equal(report.Assembly.Sha256, restored.Assembly.Sha256);
        Assert.Equal(report.Provenance.ToolVersion, restored.Provenance.ToolVersion);
        Assert.Equal(report.Configuration.CostMode, restored.Configuration.CostMode);
    }

    [Fact]
    public void ContractClassification_UsesExternalContract_WhenBoundaryIsNotProvenLocally()
    {
        var contract = new BoundContract("payloadBytes", 1_048_576, "bytes", "api-gateway", "Endpoint.Process",
            ContractTrust.External, new DateOnly(2027, 1, 1), "platform", "REL-1");

        Assert.Equal(EvidenceKind.ExternalContract, DomainContractValidator.ClassifyContract(contract, provenLocally: false));
        Assert.Equal(EvidenceKind.ProvenUnderAssumptions, DomainContractValidator.ClassifyContract(contract, provenLocally: true));
    }

    [Fact]
    public void Suppression_DoesNotPromoteUnknownToProven()
    {
        var finding = new Finding(
            "MEM012",
            "memory",
            Severity.High,
            0.85,
            new AnalysisLocation("Sample", "Process", 100, 10, 20, [], []),
            EvidenceKind.Unknown,
            RemediationType.Review,
            "Payload bound is unknown",
            "A payload is read from an untrusted source.",
            "The bound is not proven by the assembly.",
            null,
            [],
            ["Review the gateway contract"],
            false,
            null);

        var suppression = new Suppression(
            "SUP-1",
            "Sample.Process",
            "Gateway contract caps the body at 2MiB.",
            "platform-team",
            "analysis",
            new DateOnly(2027, 1, 1),
            "REL-1842");

        var suppressed = DomainContractValidator.ApplySuppression(finding, suppression);

        Assert.True(suppressed.Suppressed);
        Assert.Equal("SUP-1", suppressed.SuppressionId);
        Assert.Equal(EvidenceKind.Unknown, suppressed.Evidence);
    }

    [Fact]
    public void SarifIncludesSuppressionMetadata()
    {
        var finding = new Finding(
            "MEM012",
            "memory",
            Severity.High,
            0.85,
            new AnalysisLocation("Sample", "Process", 100, 10, 20, [], []),
            EvidenceKind.ExternalContract,
            RemediationType.Review,
            "Payload bound is externally constrained",
            "A payload is bounded by the platform contract.",
            "The assembly did not prove the bound locally.",
            null,
            [],
            ["Review the gateway contract"],
            true,
            "SUP-1");

        var sarif = SarifReportSerializer.Serialize([finding]);

        Assert.Contains("suppressed", sarif, StringComparison.Ordinal);
        Assert.Contains("SUP-1", sarif, StringComparison.Ordinal);
        Assert.Contains("SUPPRESSED_UNDER_EXTERNAL_CONTRACT", sarif, StringComparison.Ordinal);
    }

    private static AnalysisReport CreateReport(BoundContract? contract = null) => new(
        "0.1",
        new AssemblyIdentity("Sample", new string('a', 64), "net8.0", "AnyCPU"),
        new Provenance("1.0.0", "net8", DateTimeOffset.Parse("2026-09-13T00:00:00Z"), new string('a', 64)),
        new AnalysisScenarioConfiguration(AnalysisScenario.Baseline, AnalysisCostMode.Pessimistic),
        new AnalysisSummary(1, 2, 0, 0, 0, 0, 0),
        [], contract is null ? [] : [contract], []);
}
