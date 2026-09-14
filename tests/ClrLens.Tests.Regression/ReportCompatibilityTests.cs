using System.Text.Json;
using ClrLens.Core.Serialization;
using ClrLens.Core.Validation;

namespace ClrLens.Tests.Regression;

public sealed class ReportCompatibilityTests
{
    [Fact]
    public void Serializer_OutputContainsStableContractFields()
    {
        var report = TestSupport.CreateReport();

        var json = AnalysisReportSerializer.Serialize(report);
        using var document = JsonDocument.Parse(json);

        Assert.Equal("0.1", document.RootElement.GetProperty("schemaVersion").GetString());
        Assert.Equal("regression", document.RootElement.GetProperty("assembly").GetProperty("name").GetString());
        Assert.Equal("externalContract", document.RootElement.GetProperty("contracts")[0].GetProperty("evidence").GetString());
    }

    [Fact]
    public void Serializer_RoundTripRemainsValidForCurrentContractValidator()
    {
        var report = TestSupport.CreateReport();

        var roundTrip = AnalysisReportSerializer.Deserialize(AnalysisReportSerializer.Serialize(report));
        var errors = DomainContractValidator.Validate(roundTrip, DateOnly.Parse("2026-01-01"));

        Assert.Empty(errors);
    }
}
