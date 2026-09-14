using ClrLens.Core.Domain;
using ClrLens.Core.Serialization;

namespace ClrLens.Tests.Unit;

public sealed class DomainContractTests
{
    [Fact]
    public void AssemblyIdentity_PreservesValues()
    {
        var identity = new AssemblyIdentity("Name", new string('A', 64), "net8.0", "Amd64", "1.2.3", true, true, true);

        Assert.Equal("Name", identity.Name);
        Assert.Equal(new string('A', 64), identity.Sha256);
        Assert.Equal("1.2.3", identity.Version);
        Assert.Equal("net8.0", identity.TargetFramework);
        Assert.True(identity.HasPdb);
        Assert.True(identity.IsStrongNamed);
        Assert.True(identity.IsReadyToRun);
    }

    [Fact]
    public void EvidenceKindExternalContract_IsSerializedAsCamelCase()
    {
        var report = TestSupport.CreateValidReport();

        var json = AnalysisReportSerializer.Serialize(report);

        Assert.Contains("\"externalContract\"", json, StringComparison.Ordinal);
    }
}
