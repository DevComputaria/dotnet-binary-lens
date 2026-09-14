using ClrLens.Core.Domain;
using ClrLens.Core.Validation;

namespace ClrLens.Tests.Unit;

public sealed class DomainValidationTests
{
    [Fact]
    public void Validator_RejectsInvalidSha256()
    {
        var report = TestSupport.CreateValidReport() with { Assembly = new AssemblyIdentity("sample", "123", "net8.0", "Amd64") };

        var errors = DomainContractValidator.Validate(report, DateOnly.Parse("2026-01-01"));

        Assert.Contains(errors, e => e.Contains("assembly.sha256", StringComparison.Ordinal));
    }

    [Fact]
    public void Validator_RejectsNegativeContractAndWrongEvidenceAndExpiredContract()
    {
        var report = TestSupport.CreateValidReport() with
        {
            Contracts =
            [
                new BoundContract("X", -1, "bytes", "spec", "scope", ContractTrust.External, DateOnly.Parse("2025-12-31"), "owner", "ticket", EvidenceKind.Inferred)
            ]
        };

        var errors = DomainContractValidator.Validate(report, DateOnly.Parse("2026-01-01"));

        Assert.Contains(errors, e => e.Contains("max cannot be negative", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("must use ExternalContract", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("is expired", StringComparison.Ordinal));
    }

    [Fact]
    public void Validator_RejectsIncompleteSuppression()
    {
        var report = TestSupport.CreateValidReport() with
        {
            Suppressions =
            [
                new Suppression("sup", "target", "", "", "", DateOnly.Parse("2025-12-31"))
            ]
        };

        var errors = DomainContractValidator.Validate(report, DateOnly.Parse("2026-01-01"));

        Assert.Contains(errors, e => e.Contains("reason is required", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("owner is required", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("scope is required", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("is expired", StringComparison.Ordinal));
    }
}
