using ClrLens.Core.Serialization;

namespace ClrLens.Tests.Unit;

public sealed class SerializationTests
{
    [Fact]
    public void Serialize_IsDeterministic()
    {
        var report = TestSupport.CreateValidReport();

        var first = AnalysisReportSerializer.Serialize(report);
        var second = AnalysisReportSerializer.Serialize(report);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Deserialize_RoundTripsValues()
    {
        var report = TestSupport.CreateValidReport();
        var json = AnalysisReportSerializer.Serialize(report);

        var result = AnalysisReportSerializer.Deserialize(json);

        Assert.Equal(report.SchemaVersion, result.SchemaVersion);
        Assert.Equal(report.Assembly, result.Assembly);
        Assert.Equal(report.Provenance, result.Provenance);
        Assert.Equal(report.Contracts.Single(), result.Contracts.Single());
        Assert.Equal(report.Suppressions.Single(), result.Suppressions.Single());
    }
}
